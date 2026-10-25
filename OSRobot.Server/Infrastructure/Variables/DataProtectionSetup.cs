// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using OSRobot.Server.Configuration;

namespace OSRobot.Server.Infrastructure.Variables;

/// <param name="KeysPath">Full path of the key ring folder.</param>
/// <param name="Protection">How the key ring is encrypted, for the startup log.</param>
/// <param name="ProtectedAtRest">False when only the folder's permissions protect the keys.</param>
/// <param name="KeyProtection">What new keys are encrypted with, as <see cref="DataProtectionSetup.DescribeKeyProtection"/>
/// reads it back from a key: "certificate:THUMBPRINT", "dpapi" or "none".</param>
public sealed record DataProtectionSetupResult(string KeysPath, string Protection, bool ProtectedAtRest, string KeyProtection);

/// <summary>
/// ASP.NET Core Data Protection, which encrypts the secrets stored in config.db. The keys live in their
/// own folder (never in config.db), and are themselves encrypted with, in order of preference:
/// a certificate, when one is configured (any platform); DPAPI, on Windows (tied to the service
/// account); nothing, elsewhere (the folder's permissions only, and the server warns at startup).
/// A configured certificate that can't be opened stops the server: silently falling back to
/// unprotected keys would be worse.
/// </summary>
public static class DataProtectionSetup
{
    // Fixed, so the keys keep working when the installation folder moves (the default isolates
    // applications by content root path).
    public const string ApplicationName = "OSRobot";

    /// <param name="dataPath">JobEngineConfig:DataPath, home of the default keys folder.</param>
    /// <param name="getEnvironmentVariable">Environment lookup (replaceable by tests).</param>
    public static DataProtectionSetupResult AddOSRobotDataProtection(this IServiceCollection services, DataProtectionConfig config, string dataPath,
                                                                     Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        string keysPath = Path.GetFullPath(string.IsNullOrWhiteSpace(config.KeysPath) ? Path.Combine(dataPath, "keys") : config.KeysPath);
        Directory.CreateDirectory(keysPath);

        IDataProtectionBuilder builder = services.AddDataProtection()
                                                 .SetApplicationName(ApplicationName)
                                                 .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        if (!string.IsNullOrWhiteSpace(config.CertificatePath))
        {
            string? password = ReadCertificatePassword(config, getEnvironmentVariable);
            X509Certificate2 certificate = LoadCertificate(config.CertificatePath, password);

            // The certificate given here is also used to decrypt: it doesn't need to be in a certificate store.
            builder.ProtectKeysWithCertificate(certificate);

            X509Certificate2[] previousCertificates =
            [
                .. config.PreviousCertificatePaths
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(path => LoadCertificate(path, password))
            ];
            if (previousCertificates.Length > 0)
                builder.UnprotectKeysWithAnyCertificate(previousCertificates);

            return new DataProtectionSetupResult(keysPath, $"certificate {certificate.Subject} (thumbprint {certificate.Thumbprint})", true,
                                                 KeyProtectionCertificatePrefix + certificate.Thumbprint);
        }

        if (OperatingSystem.IsWindows())
        {
            builder.ProtectKeysWithDpapi();
            return new DataProtectionSetupResult(keysPath, "Windows DPAPI (current account)", true, KeyProtectionDpapi);
        }

        return new DataProtectionSetupResult(keysPath, "none: only the folder's permissions", false, KeyProtectionNone);
    }

    public const string KeyProtectionCertificatePrefix = "certificate:";
    public const string KeyProtectionDpapi = "dpapi";
    public const string KeyProtectionNone = "none";

    /// <summary>
    /// How a key of the key ring (its &lt;key&gt; element) is encrypted: "certificate:THUMBPRINT", "dpapi",
    /// "none", or "unknown" for an encryption OS-Robot doesn't configure. Read from the key file itself, so
    /// it tells how the key really is stored, whatever the configuration says now.
    /// </summary>
    public static string DescribeKeyProtection(XElement keyElement)
    {
        XElement? encryptedSecret = keyElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "encryptedSecret");
        if (encryptedSecret == null)
            return KeyProtectionNone;

        string decryptorType = (string?)encryptedSecret.Attribute("decryptorType") ?? string.Empty;

        // DpapiNGXmlDecryptor (Windows DPAPI-NG) is not DPAPI as configured here: keep it apart.
        if (decryptorType.Contains(".DpapiXmlDecryptor,", StringComparison.Ordinal))
            return KeyProtectionDpapi;

        if (decryptorType.Contains(".EncryptedXmlDecryptor,", StringComparison.Ordinal))
        {
            // EncryptedXml embeds the certificate the key was encrypted with.
            string? certificateBase64 = encryptedSecret.Descendants().FirstOrDefault(e => e.Name.LocalName == "X509Certificate")?.Value;
            if (certificateBase64 != null)
            {
                using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateBase64.Trim()));
                return KeyProtectionCertificatePrefix + certificate.Thumbprint;
            }
        }

        return "unknown";
    }

    /// <summary>
    /// The certificate's password: the systemd credential first (decrypted by systemd into
    /// $CREDENTIALS_DIRECTORY, readable by the service only), then the environment variable. Null: no password.
    /// </summary>
    public static string? ReadCertificatePassword(DataProtectionConfig config, Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        string? credentialsDirectory = getEnvironmentVariable("CREDENTIALS_DIRECTORY");
        if (!string.IsNullOrEmpty(credentialsDirectory) && !string.IsNullOrWhiteSpace(config.CertificatePasswordCredential))
        {
            string credentialFile = Path.Combine(credentialsDirectory, config.CertificatePasswordCredential);
            if (File.Exists(credentialFile))
                return File.ReadAllText(credentialFile).TrimEnd('\r', '\n');
        }

        if (!string.IsNullOrWhiteSpace(config.CertificatePasswordEnvironmentVariable))
            return getEnvironmentVariable(config.CertificatePasswordEnvironmentVariable);

        return null;
    }

    private static X509Certificate2 LoadCertificate(string path, string? password)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"Data Protection: the certificate file '{fullPath}' does not exist (AppSettings:DataProtection).");

        X509Certificate2 certificate;
        try
        {
            // The private key stays in memory instead of being written to a key store (not supported on macOS).
            X509KeyStorageFlags flags = OperatingSystem.IsMacOS() ? X509KeyStorageFlags.DefaultKeySet : X509KeyStorageFlags.EphemeralKeySet;
            certificate = X509CertificateLoader.LoadPkcs12FromFile(fullPath, password, flags);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException($"Data Protection: cannot open the certificate '{fullPath}'. Is its password right? " +
                                                "See AppSettings:DataProtection and the deployment guide.", ex);
        }

        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException($"Data Protection: the certificate '{fullPath}' has no private key. Export it as a .pfx with its private key.");

        return certificate;
    }
}
