// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using OSRobot.Server.Configuration;
using OSRobot.Server.Infrastructure.Variables;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestDataProtectionSetup
{
    private const string _passwordVariable = "TEST_DP_CERT_PASSWORD";
    private const string _purpose = "TestDataProtectionSetup";

    private string _root = string.Empty;

    [TestInitialize]
    public void CreateFolder()
    {
        _root = Path.Combine(Path.GetTempPath(), "OSRobotDataProtectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void DeleteFolder()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string CreatePfx(string fileName, string password)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new($"CN=OSRobot test {fileName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));

        string path = Path.Combine(_root, fileName);
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
        return path;
    }

    private DataProtectionConfig Config(string certificatePath = "", string previousCertificatePaths = "")
    {
        return new DataProtectionConfig
        {
            KeysPath = Path.Combine(_root, "keys"),
            CertificatePath = certificatePath,
            CertificatePasswordEnvironmentVariable = _passwordVariable,
            PreviousCertificatePaths = previousCertificatePaths
        };
    }

    private static Func<string, string?> Environment(string? password, string? credentialsDirectory = null)
    {
        return name => name switch
        {
            _passwordVariable => password,
            "CREDENTIALS_DIRECTORY" => credentialsDirectory,
            _ => null
        };
    }

    // A provider built from scratch, as after a restart: nothing in memory, only the keys folder.
    private (ServiceProvider Services, DataProtectionSetupResult Result) Start(DataProtectionConfig config, string? password)
    {
        ServiceCollection services = new();
        DataProtectionSetupResult result = services.AddOSRobotDataProtection(config, _root, Environment(password));
        return (services.BuildServiceProvider(), result);
    }

    private static IDataProtector Protector(ServiceProvider services)
    {
        return services.GetRequiredService<IDataProtectionProvider>().CreateProtector(_purpose);
    }

    [TestMethod]
    public void With_a_certificate_the_keys_are_encrypted_and_readable_after_a_restart()
    {
        string pfx = CreatePfx("dp.pfx", "cert-pw");
        string ciphertext;

        (ServiceProvider first, DataProtectionSetupResult result) = Start(Config(pfx), "cert-pw");
        using (first)
        {
            ciphertext = Protector(first).Protect("s3cret");
            Assert.IsTrue(result.ProtectedAtRest);
            StringAssert.Contains(result.Protection, "certificate");
        }

        // The key file holds the key encrypted with the certificate, not in clear.
        string keyXml = File.ReadAllText(Directory.GetFiles(Path.Combine(_root, "keys"), "key-*.xml").Single());
        StringAssert.Contains(keyXml, "EncryptedXmlDecryptor");

        // The certificate is not in any certificate store: decrypting uses the one loaded from the .pfx.
        (ServiceProvider second, _) = Start(Config(pfx), "cert-pw");
        using (second)
        {
            Assert.AreEqual("s3cret", Protector(second).Unprotect(ciphertext));
        }
    }

    [TestMethod]
    public void Keys_from_a_previous_certificate_stay_readable_after_replacing_it()
    {
        string oldPfx = CreatePfx("old.pfx", "cert-pw");
        string newPfx = CreatePfx("new.pfx", "cert-pw");
        string ciphertext;

        (ServiceProvider first, _) = Start(Config(oldPfx), "cert-pw");
        using (first)
            ciphertext = Protector(first).Protect("s3cret");

        (ServiceProvider second, _) = Start(Config(newPfx, previousCertificatePaths: oldPfx), "cert-pw");
        using (second)
            Assert.AreEqual("s3cret", Protector(second).Unprotect(ciphertext));
    }

    [TestMethod]
    public void A_wrong_certificate_password_stops_the_start()
    {
        string pfx = CreatePfx("dp.pfx", "cert-pw");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Start(Config(pfx), "wrong"));
        StringAssert.Contains(ex.Message, "password");
    }

    [TestMethod]
    public void A_missing_certificate_file_stops_the_start()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Start(Config(Path.Combine(_root, "missing.pfx")), "x"));
        StringAssert.Contains(ex.Message, "does not exist");
    }

    [TestMethod]
    public void Without_a_certificate_the_keys_use_DPAPI_on_Windows_only()
    {
        (ServiceProvider services, DataProtectionSetupResult result) = Start(Config(), null);
        using (services)
        {
            Assert.AreEqual("s3cret", Protector(services).Unprotect(Protector(services).Protect("s3cret")));
            Assert.AreEqual(OperatingSystem.IsWindows(), result.ProtectedAtRest);
            Assert.AreEqual(Path.Combine(_root, "keys"), result.KeysPath);
        }
    }

    [TestMethod]
    public void The_systemd_credential_wins_over_the_environment_variable()
    {
        string credentials = Path.Combine(_root, "credentials");
        Directory.CreateDirectory(credentials);
        File.WriteAllText(Path.Combine(credentials, "dp-cert-password"), "from-systemd\n");

        Assert.AreEqual("from-systemd", DataProtectionSetup.ReadCertificatePassword(Config(), Environment("from-env", credentials)));
        Assert.AreEqual("from-env", DataProtectionSetup.ReadCertificatePassword(Config(), Environment("from-env", Path.Combine(_root, "none"))));
        Assert.IsNull(DataProtectionSetup.ReadCertificatePassword(Config(), Environment(null)));
    }
}
