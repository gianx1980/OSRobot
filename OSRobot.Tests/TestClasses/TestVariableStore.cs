// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OSRobot.Server.Core.Variables;
using OSRobot.Server.Infrastructure.DataAccess;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Infrastructure.Variables;
using OSRobot.Server.Models.DTO.Variables;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestVariableStore
{
    private string _root = string.Empty;
    private string _connectionString = string.Empty;
    private ServiceProvider _services = null!;

    [TestInitialize]
    public void CreateDatabase()
    {
        _root = Path.Combine(Path.GetTempPath(), "OSRobotVariableTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        // Pooling off: the temporary file can then be deleted at the end of the test.
        _connectionString = $"Data Source={Path.Combine(_root, "config.db")};Pooling=False";
        ConfigDatabaseSchema.UpgradeVariablesSchema(_connectionString);

        _services = BuildServices(new EphemeralDataProtectionProvider());
    }

    [TestCleanup]
    public void DeleteDatabase()
    {
        _services.Dispose();
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private ServiceProvider BuildServices(IDataProtectionProvider dataProtectionProvider)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<RobotDBContext>(options => options.UseSqlite(_connectionString));
        services.AddSingleton(dataProtectionProvider);
        services.AddSingleton<VariableStore>();
        return services.BuildServiceProvider();
    }

    private VariableStore Store => _services.GetRequiredService<VariableStore>();

    private static VariableSaveRequest Request(string name, string? value, bool isSecret, string? description = null)
    {
        return new VariableSaveRequest { Name = name, Value = value, IsSecret = isSecret, Description = description };
    }

    private string StoredValue(string name)
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new("SELECT Value FROM Variables WHERE Name = $name", connection);
        command.Parameters.AddWithValue("$name", name);
        return (string)command.ExecuteScalar()!;
    }

    // ----- Variables ----------------------------------------------------------------------------

    [TestMethod]
    public void A_saved_variable_is_listed_with_its_value_and_resolves()
    {
        Assert.IsTrue(Store.Save(Request("ShareRoot", @"\\files\exports", isSecret: false, "Where exports go"), "admin"));

        VariableListItem item = Store.List().Single();
        Assert.AreEqual("ShareRoot", item.Name);
        Assert.AreEqual(@"\\files\exports", item.Value);
        Assert.AreEqual("Where exports go", item.Description);
        Assert.AreEqual("admin", item.UpdatedBy);
        Assert.IsFalse(item.IsSecret);
        Assert.AreEqual(@"\\files\exports", Store.Resolve(VariableKind.Variable, "ShareRoot"));
    }

    [TestMethod]
    public void Names_are_case_insensitive_and_saving_again_updates()
    {
        Store.Save(Request("ShareRoot", "a", isSecret: false), null);

        Assert.IsFalse(Store.Save(Request("SHAREROOT", "b", isSecret: false), null));

        VariableListItem item = Store.List().Single();
        Assert.AreEqual("SHAREROOT", item.Name);
        Assert.AreEqual("b", Store.Resolve(VariableKind.Variable, "shareroot"));
    }

    [TestMethod]
    [DataRow("", DisplayName = "Empty")]
    [DataRow("1st", DisplayName = "Starts with a digit")]
    [DataRow("has space", DisplayName = "Space")]
    [DataRow("quote'", DisplayName = "Quote")]
    [DataRow("brace}", DisplayName = "Brace")]
    public void An_invalid_name_is_refused(string name)
    {
        Assert.Throws<VariableValidationException>(() => Store.Save(Request(name, "x", isSecret: false), null));
    }

    [TestMethod]
    public void Delete_removes_the_variable()
    {
        Store.Save(Request("ShareRoot", "a", isSecret: false), null);

        Assert.IsTrue(Store.Delete("shareroot"));
        Assert.IsFalse(Store.Delete("ShareRoot"));
        Assert.AreEqual(0, Store.List().Count);
        Assert.Throws<ApplicationException>(() => Store.Resolve(VariableKind.Variable, "ShareRoot"));
    }

    // ----- Secrets ------------------------------------------------------------------------------

    [TestMethod]
    public void A_secret_is_stored_encrypted_never_listed_and_resolves()
    {
        Store.Save(Request("SmtpPassword", "p@ss-w0rd", isSecret: true), null);

        VariableListItem item = Store.List().Single();
        Assert.IsTrue(item.IsSecret);
        Assert.IsNull(item.Value);
        Assert.IsTrue(item.IsReadable);

        Assert.IsFalse(StoredValue("SmtpPassword").Contains("p@ss-w0rd"));
        Assert.AreEqual("p@ss-w0rd", Store.Resolve(VariableKind.Secret, "SmtpPassword"));
        Assert.IsFalse(Store.IsDefined(VariableKind.Variable, "SmtpPassword"));
    }

    [TestMethod]
    public void A_new_secret_needs_a_value()
    {
        Assert.Throws<VariableValidationException>(() => Store.Save(Request("SmtpPassword", "", isSecret: true), null));
        Assert.AreEqual(0, Store.List().Count);
    }

    [TestMethod]
    public void Saving_a_secret_without_a_value_keeps_the_current_one()
    {
        Store.Save(Request("SmtpPassword", "p@ss-w0rd", isSecret: true), null);

        Store.Save(Request("SmtpPassword", null, isSecret: true, "now with a description"), null);

        Assert.AreEqual("p@ss-w0rd", Store.Resolve(VariableKind.Secret, "SmtpPassword"));
        Assert.AreEqual("now with a description", Store.List().Single().Description);
    }

    [TestMethod]
    public void A_variable_turned_into_a_secret_keeps_its_value_encrypted()
    {
        Store.Save(Request("Token", "abc-123", isSecret: false), null);

        Store.Save(Request("Token", null, isSecret: true), null);

        Assert.IsFalse(StoredValue("Token").Contains("abc-123"));
        Assert.AreEqual("abc-123", Store.Resolve(VariableKind.Secret, "Token"));
    }

    [TestMethod]
    public void A_secret_turned_into_a_variable_needs_its_value_again()
    {
        Store.Save(Request("Token", "abc-123", isSecret: true), null);

        Assert.Throws<VariableValidationException>(() => Store.Save(Request("Token", null, isSecret: false), null));
        Assert.AreEqual("abc-123", Store.Resolve(VariableKind.Secret, "Token"));

        Store.Save(Request("Token", "visible", isSecret: false), null);
        Assert.AreEqual("visible", Store.Resolve(VariableKind.Variable, "Token"));
    }

    [TestMethod]
    public void Secrets_are_redacted()
    {
        Store.Save(Request("SmtpPassword", "p@ss-w0rd", isSecret: true), null);
        Store.Save(Request("Plain", "visible-value", isSecret: false), null);

        Assert.AreEqual("auth *** visible-value", Store.Redact("auth p@ss-w0rd visible-value"));
    }

    [TestMethod]
    public void A_secret_saved_with_other_keys_is_listed_as_unreadable_and_fails_with_a_hint()
    {
        Store.Save(Request("SmtpPassword", "p@ss-w0rd", isSecret: true), null);

        // Same database, different keys: as after losing the keys folder.
        using ServiceProvider otherKeys = BuildServices(new EphemeralDataProtectionProvider());
        VariableStore store = otherKeys.GetRequiredService<VariableStore>();

        Assert.IsFalse(store.List().Single().IsReadable);
        ApplicationException ex = Assert.Throws<ApplicationException>(() => store.Resolve(VariableKind.Secret, "SmtpPassword"));
        StringAssert.Contains(ex.Message, "cannot be decrypted");
    }

    [TestMethod]
    public void A_change_is_seen_by_the_next_resolution_without_a_reload()
    {
        Store.Save(Request("ShareRoot", "a", isSecret: false), null);
        Assert.AreEqual("a", Store.Resolve(VariableKind.Variable, "ShareRoot"));

        Store.Save(Request("ShareRoot", "b", isSecret: false), null);

        Assert.AreEqual("b", Store.Resolve(VariableKind.Variable, "ShareRoot"));
    }
}
