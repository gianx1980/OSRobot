// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Variables;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestVariablePlaceholders
{
    private static VariableSet Variables(params (string Name, string Value)[] variables)
    {
        return new VariableSet(variables.Select(v => KeyValuePair.Create(v.Name, v.Value)), []);
    }

    private static VariableSet Secrets(params (string Name, string? Value)[] secrets)
    {
        return new VariableSet([], secrets.Select(s => KeyValuePair.Create(s.Name, s.Value)));
    }

    private static DynamicDataChain ChainWith(int objectId, string key, object value)
    {
        DynamicDataChain chain = [];
        chain[objectId] = new DynamicDataSet { [key] = value };
        return chain;
    }

    // ----- Resolution ---------------------------------------------------------------------------

    [TestMethod]
    public void Replaces_variables_and_secrets()
    {
        VariableSet set = new([KeyValuePair.Create("ShareRoot", @"\\files\exports")], [KeyValuePair.Create("SmtpPassword", (string?)"p@ss-w0rd")]);

        Assert.AreEqual(@"\\files\exports\today p@ss-w0rd",
                        DynamicDataParser.ReplaceDynamicData(@"{var['ShareRoot']}\today {secret['SmtpPassword']}", [], 0, null, set));
    }

    [TestMethod]
    public void Names_and_placeholder_keywords_are_case_insensitive()
    {
        VariableSet set = new([KeyValuePair.Create("ShareRoot", "x")], [KeyValuePair.Create("Pwd", (string?)"y")]);

        Assert.AreEqual("x y", DynamicDataParser.ReplaceDynamicData("{VAR['shareroot']} {Secret['PWD']}", [], 0, null, set));
    }

    [TestMethod]
    public void Mixes_with_object_references()
    {
        DynamicDataChain chain = ChainWith(2, "FileName", "report.csv");

        Assert.AreEqual("/in/report.csv", DynamicDataParser.ReplaceDynamicData("{var['Inbox']}/{object[2].FileName}", chain, 0, null, Variables(("Inbox", "/in"))));
    }

    [TestMethod]
    public void An_undefined_variable_or_secret_fails_with_its_name()
    {
        ApplicationException variableEx = Assert.Throws<ApplicationException>(
            () => DynamicDataParser.ReplaceDynamicData("{var['Missing']}", [], 0, null, VariableSet.Empty));
        StringAssert.Contains(variableEx.Message, "Variable 'Missing' is not defined");

        ApplicationException secretEx = Assert.Throws<ApplicationException>(
            () => DynamicDataParser.ReplaceDynamicData("{secret['Missing']}", [], 0, null, VariableSet.Empty));
        StringAssert.Contains(secretEx.Message, "Secret 'Missing' is not defined");
    }

    [TestMethod]
    public void A_secret_that_cannot_be_decrypted_fails_with_a_hint()
    {
        ApplicationException ex = Assert.Throws<ApplicationException>(
            () => DynamicDataParser.ReplaceDynamicData("{secret['Lost']}", [], 0, null, Secrets(("Lost", null))));

        StringAssert.Contains(ex.Message, "cannot be decrypted");
    }

    [TestMethod]
    public void A_variable_and_a_secret_with_the_same_name_are_distinct()
    {
        VariableSet set = new([KeyValuePair.Create("Name", "plain")], [KeyValuePair.Create("Name", (string?)"hidden")]);

        Assert.AreEqual("plain hidden", DynamicDataParser.ReplaceDynamicData("{var['Name']} {secret['Name']}", [], 0, null, set));
    }

    // ----- Single pass: substituted values are never parsed again ---------------------------------

    [TestMethod]
    public void A_placeholder_inside_an_upstream_value_stays_literal()
    {
        // e.g. a REST response or a file line that contains placeholder text
        DynamicDataChain chain = ChainWith(2, "RawContent", "{secret['SmtpPassword']} {var['ShareRoot']}");
        VariableSet set = new([KeyValuePair.Create("ShareRoot", "x")], [KeyValuePair.Create("SmtpPassword", (string?)"p@ss-w0rd")]);

        Assert.AreEqual("got: {secret['SmtpPassword']} {var['ShareRoot']}",
                        DynamicDataParser.ReplaceDynamicData("got: {object[2].RawContent}", chain, 0, null, set));
    }

    [TestMethod]
    public void An_environment_placeholder_inside_an_upstream_value_stays_literal()
    {
        string name = "OSROBOT_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(name, "leaked");
        try
        {
            DynamicDataChain chain = ChainWith(2, "RawContent", $"{{environment['{name}']}}");

            Assert.AreEqual($"{{environment['{name}']}}", DynamicDataParser.ReplaceDynamicData("{object[2].RawContent}", chain, 0, null, VariableSet.Empty));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [TestMethod]
    public void A_variable_value_that_looks_like_a_placeholder_stays_literal()
    {
        VariableSet set = new([KeyValuePair.Create("Tricky", "{secret['Pwd']}")], [KeyValuePair.Create("Pwd", (string?)"hidden")]);

        Assert.AreEqual("{secret['Pwd']}", DynamicDataParser.ReplaceDynamicData("{var['Tricky']}", [], 0, null, set));
    }

    // ----- ResolveVariables / GetVariableReferences ---------------------------------------------

    [TestMethod]
    public void ResolveVariables_leaves_object_references_untouched()
    {
        Assert.AreEqual("srv01;{object[3].Database}",
                        DynamicDataParser.ResolveVariables("{var['Server']};{object[3].Database}", Variables(("Server", "srv01"))));
    }

    [TestMethod]
    public void ResolveVariables_handles_empty_input_and_leaves_code_unchanged()
    {
        Assert.AreEqual(string.Empty, DynamicDataParser.ResolveVariables(null, VariableSet.Empty));
        Assert.AreEqual("[CODE]\"{var['X']}\"", DynamicDataParser.ResolveVariables("[CODE]\"{var['X']}\"", VariableSet.Empty));
    }

    [TestMethod]
    public void GetVariableReferences_finds_variables_and_secrets_but_not_environment()
    {
        List<VariableReference> references = DynamicDataParser.GetVariableReferences("{var['A']} {environment['PATH']} {secret['B']} {object[1].C}");

        CollectionAssert.AreEqual(new[] { new VariableReference(VariableKind.Variable, "A", "{var['A']}"),
                                          new VariableReference(VariableKind.Secret, "B", "{secret['B']}") },
                                  references);
    }

    // ----- Redaction -----------------------------------------------------------------------------

    [TestMethod]
    public void Redact_replaces_secret_values_but_not_variables()
    {
        VariableSet set = new([KeyValuePair.Create("Plain", "visible-value")], [KeyValuePair.Create("Pwd", (string?)"p@ss-w0rd")]);

        Assert.AreEqual("login failed for p@ss? no: *** / visible-value", set.Redact("login failed for p@ss? no: p@ss-w0rd / visible-value"));
    }

    [TestMethod]
    public void Redact_replaces_the_longest_secret_first()
    {
        VariableSet set = Secrets(("Short", "abcd"), ("Long", "abcd-efgh"));

        Assert.AreEqual("*** and ***", set.Redact("abcd-efgh and abcd"));
    }

    [TestMethod]
    public void Redact_ignores_very_short_and_undecryptable_secrets()
    {
        VariableSet set = Secrets(("Pin", "123"), ("Lost", null));

        Assert.AreEqual("pin 123", set.Redact("pin 123"));
    }
}
