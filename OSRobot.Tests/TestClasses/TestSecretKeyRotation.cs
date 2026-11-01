// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OSRobot.Server.Configuration;
using OSRobot.Server.Core.Variables;
using OSRobot.Server.Infrastructure.DataAccess;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Infrastructure.Variables;
using OSRobot.Server.Models.DTO.Variables;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestSecretKeyRotation
{
    private const string _password = "cert-pw";

    private string _root = string.Empty;
    private string _connectionString = string.Empty;

    [TestInitialize]
    public void CreateFolder()
    {
        _root = Path.Combine(Path.GetTempPath(), "OSRobotKeyRotationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _connectionString = $"Data Source={Path.Combine(_root, "config.db")};Pooling=False";
        ConfigDatabaseSchema.UpgradeVariablesSchema(_connectionString);
    }

    [TestCleanup]
    public void DeleteFolder()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string CreatePfx(string fileName)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new($"CN=OSRobot rotation {fileName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
        string path = Path.Combine(_root, fileName);
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, _password));
        return path;
    }

    // One server start: the services as Program.cs builds them, from the same keys folder and config.db.
    private sealed class Server(ServiceProvider services, DataProtectionSetupResult setup) : IDisposable
    {
        public ServiceProvider Services { get; } = services;
        public DataProtectionSetupResult Setup { get; } = setup;
        public VariableStore Store => Services.GetRequiredService<VariableStore>();

        public bool Rotate() => SecretKeyRotation.EnsureActiveKeyMatchesConfiguration(Services, Setup, NullLogger.Instance);

        public void Dispose() => Services.Dispose();
    }

    private Server Start(string certificatePath = "", string previousCertificatePaths = "")
    {
        DataProtectionConfig config = new()
        {
            KeysPath = Path.Combine(_root, "keys"),
            CertificatePath = certificatePath,
            CertificatePasswordEnvironmentVariable = "TEST_ROTATION_PASSWORD",
            PreviousCertificatePaths = previousCertificatePaths
        };

        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<RobotDBContext>(options => options.UseSqlite(_connectionString));
        DataProtectionSetupResult setup = services.AddOSRobotDataProtection(config, _root, name => name == "TEST_ROTATION_PASSWORD" ? _password : null);
        services.AddSingleton<VariableStore>();

        return new Server(services.BuildServiceProvider(), setup);
    }

    private string StoredValue(string name)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new("SELECT Value FROM Variables WHERE Name = $name", connection);
        command.Parameters.AddWithValue("$name", name);
        return (string)command.ExecuteScalar()!;
    }

    private List<string> KeyProtections()
    {
        return [.. Directory.GetFiles(Path.Combine(_root, "keys"), "key-*.xml")
                            .Select(f => XElement.Load(f))
                            .OrderBy(k => (DateTime)k.Element("creationDate")!)
                            .Select(DataProtectionSetup.DescribeKeyProtection)];
    }

    private static void SaveSecret(Server server, string name, string value)
    {
        server.Store.Save(new VariableSaveRequest { Name = name, Value = value, IsSecret = true }, null);
    }

    // Revoking every key but the newest proves the secrets no longer need the old ones.
    private static void RevokeAllButNewestKey(Server server)
    {
        IKeyManager keyManager = server.Services.GetRequiredService<IKeyManager>();
        IKey newest = keyManager.GetAllKeys().OrderByDescending(k => k.CreationDate).First();
        foreach (IKey key in keyManager.GetAllKeys().Where(k => k.KeyId != newest.KeyId))
            keyManager.RevokeKey(key.KeyId, "test: old protection");
    }

    [TestMethod]
    public void Adding_a_certificate_moves_the_secrets_to_a_certificate_protected_key()
    {
        string withoutCertificate = OperatingSystem.IsWindows() ? DataProtectionSetup.KeyProtectionDpapi : DataProtectionSetup.KeyProtectionNone;
        string ciphertextBefore;

        using (Server first = Start())
        {
            SaveSecret(first, "SmtpPassword", "p@ss-w0rd");
            SaveSecret(first, "Token", "abc-123");
            Assert.IsFalse(first.Rotate(), "Nothing changed: no rotation");
            ciphertextBefore = StoredValue("SmtpPassword");
        }

        CollectionAssert.AreEqual(new[] { withoutCertificate }, KeyProtections());

        string pfx = CreatePfx("dp.pfx");
        using (Server second = Start(pfx))
        {
            Assert.IsTrue(second.Rotate());

            List<string> protections = KeyProtections();
            Assert.HasCount(2, protections);
            Assert.AreEqual(second.Setup.KeyProtection, protections[1]);
            StringAssert.StartsWith(protections[1], DataProtectionSetup.KeyProtectionCertificatePrefix);
            Assert.AreNotEqual(ciphertextBefore, StoredValue("SmtpPassword"));

            RevokeAllButNewestKey(second);
            second.Store.Reload();
            Assert.AreEqual("p@ss-w0rd", second.Store.Resolve(VariableKind.Secret, "SmtpPassword"));
            Assert.AreEqual("abc-123", second.Store.Resolve(VariableKind.Secret, "Token"));

            Assert.IsFalse(second.Rotate(), "Already matching: no second rotation");
        }
    }

    [TestMethod]
    public void Replacing_the_certificate_lets_the_old_one_go_after_the_first_start()
    {
        string oldPfx = CreatePfx("old.pfx");
        string newPfx = CreatePfx("new.pfx");

        using (Server first = Start(oldPfx))
            SaveSecret(first, "Token", "abc-123");

        // First start with the new certificate: the old one is still listed, to read the old key.
        using (Server second = Start(newPfx, previousCertificatePaths: oldPfx))
            Assert.IsTrue(second.Rotate());

        // Later starts don't need the old certificate any more.
        using Server third = Start(newPfx);
        Assert.IsFalse(third.Rotate());
        Assert.AreEqual("abc-123", third.Store.Resolve(VariableKind.Secret, "Token"));
    }

    [TestMethod]
    public void Without_keys_there_is_nothing_to_rotate()
    {
        using Server server = Start(CreatePfx("dp.pfx"));

        Assert.IsFalse(server.Rotate());
    }

    [TestMethod]
    public void A_secret_that_cannot_be_decrypted_keeps_its_ciphertext()
    {
        using (Server first = Start())
            SaveSecret(first, "Token", "abc-123");

        // A row whose ciphertext no key can read, as after restoring config.db without its keys.
        using (SqliteConnection connection = new(_connectionString))
        {
            connection.Open();
            using SqliteCommand insert = new("INSERT INTO Variables(Name, Value, IsSecret, UpdatedAt) VALUES('Lost', 'not-a-ciphertext', 1, '2026-10-25')", connection);
            insert.ExecuteNonQuery();
        }

        using Server second = Start(CreatePfx("dp.pfx"));
        Assert.IsTrue(second.Rotate());

        Assert.AreEqual("not-a-ciphertext", StoredValue("Lost"));
        Assert.AreEqual("abc-123", second.Store.Resolve(VariableKind.Secret, "Token"));
    }
}
