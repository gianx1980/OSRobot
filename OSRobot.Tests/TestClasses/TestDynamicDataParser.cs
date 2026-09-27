using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestDynamicDataParser
{
    private static DynamicDataChain ChainWith(int objectId, params (string Key, object Value)[] values)
    {
        DynamicDataSet set = [];
        foreach ((string key, object value) in values)
            set[key] = value;

        DynamicDataChain chain = [];
        chain[objectId] = set;
        return chain;
    }

    private static DataTable Recordset(params string[] names)
    {
        DataTable table = new();
        table.Columns.Add("Name", typeof(string));
        foreach (string name in names)
            table.Rows.Add(name);
        return table;
    }

    // ----- Placeholder replacement ----------------------------------------------------------------

    [TestMethod]
    public void Replaces_a_field_placeholder_with_its_value()
    {
        DynamicDataChain chain = ChainWith(1, ("Greeting", "hello"));

        Assert.AreEqual("say hello!", DynamicDataParser.ReplaceDynamicData("say {object[1].Greeting}!", chain, 0, null));
    }

    [TestMethod]
    public void Replaces_several_placeholders_from_different_objects()
    {
        DynamicDataChain chain = ChainWith(1, ("A", "one"));
        chain[2] = new DynamicDataSet { ["B"] = 2 };

        Assert.AreEqual("one-2-one", DynamicDataParser.ReplaceDynamicData("{object[1].A}-{object[2].B}-{object[1].A}", chain, 0, null));
    }

    [TestMethod]
    public void Placeholders_are_case_insensitive()
    {
        DynamicDataChain chain = ChainWith(1, ("Greeting", "hello"));

        Assert.AreEqual("hello", DynamicDataParser.ReplaceDynamicData("{OBJECT[1].Greeting}", chain, 0, null));
    }

    [TestMethod]
    public void Text_without_placeholders_is_returned_unchanged_and_null_becomes_empty()
    {
        DynamicDataChain chain = [];

        Assert.AreEqual("plain text {not a placeholder}", DynamicDataParser.ReplaceDynamicData("plain text {not a placeholder}", chain, 0, null));
        Assert.AreEqual(string.Empty, DynamicDataParser.ReplaceDynamicData(null!, chain, 0, null));
    }

    [TestMethod]
    public void A_placeholder_for_an_unknown_object_throws()
    {
        DynamicDataChain chain = ChainWith(1, ("A", "x"));

        Assert.Throws<KeyNotFoundException>(() => DynamicDataParser.ReplaceDynamicData("{object[99].A}", chain, 0, null));
    }

    [TestMethod]
    public void Replaces_an_environment_variable()
    {
        string name = "OSROBOT_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(name, "from-env");
        try
        {
            Assert.AreEqual("value=from-env", DynamicDataParser.ReplaceDynamicData($"value={{environment['{name}']}}", [], 0, null));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [TestMethod]
    public void An_undefined_environment_variable_becomes_empty()
    {
        Assert.AreEqual("[]", DynamicDataParser.ReplaceDynamicData("[{environment['OSROBOT_SURELY_NOT_SET_12345']}]", [], 0, null));
    }

    // ----- Recordsets ---------------------------------------------------------------------------

    [TestMethod]
    public void Reads_a_recordset_cell_by_explicit_row_index()
    {
        DynamicDataChain chain = ChainWith(1, ("DefaultRecordset", Recordset("first", "second", "third")));

        Assert.AreEqual("second", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset[1]['Name']}", chain, 0, null));
    }

    [TestMethod]
    public void Recordset_row_defaults_to_the_current_iteration()
    {
        DynamicDataChain chain = ChainWith(1, ("DefaultRecordset", Recordset("first", "second", "third")));

        Assert.AreEqual("first", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset['Name']}", chain, 0, null));
        Assert.AreEqual("third", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset['Name']}", chain, 2, null));
        Assert.AreEqual("second", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset[{iterationIndex}]['Name']}", chain, 1, null));
    }

    [TestMethod]
    public void Recordset_row_can_follow_the_sub_instance_index()
    {
        DynamicDataChain chain = ChainWith(1, ("DefaultRecordset", Recordset("first", "second", "third")));

        Assert.AreEqual("third", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset[{subInstanceIndex}]['Name']}", chain, 0, 2));
        Assert.AreEqual("first", DynamicDataParser.ReplaceDynamicData("{object[1].DefaultRecordset[{subInstanceIndex}]['Name']}", chain, 0, null),
                        "A missing sub-instance index counts as row 0.");
    }

    [TestMethod]
    public void Reads_a_cell_from_a_list_of_dictionaries_recordset()
    {
        List<Dictionary<string, object>> rows =
        [
            new() { ["Name"] = "a" },
            new() { ["Name"] = "b" }
        ];
        DynamicDataChain chain = ChainWith(1, ("Rows", rows));

        Assert.AreEqual("b", DynamicDataParser.ReplaceDynamicData("{object[1].Rows[1]['Name']}", chain, 0, null));
    }

    [TestMethod]
    [DataRow("", 4, null, 4)]
    [DataRow("{iterationIndex}", 4, null, 4)]
    [DataRow("{subInstanceIndex}", 4, 7, 7)]
    [DataRow("{subInstanceIndex}", 4, null, 0)]
    [DataRow("3", 4, 7, 3)]
    public void GetRowIndex_resolves_each_form(string rowIndex, int iteration, int? subInstance, int expected)
    {
        Assert.AreEqual(expected, DynamicDataParser.GetRowIndex(rowIndex, iteration, subInstance));
    }

    // ----- Discovery helpers --------------------------------------------------------------------

    [TestMethod]
    public void ContainsDynamicData_and_GetDynamicDataInfo_find_placeholders()
    {
        Assert.IsTrue(DynamicDataParser.ContainsDynamicData("x {object[3].Field} y"));
        Assert.IsFalse(DynamicDataParser.ContainsDynamicData("no placeholders {here}"));

        List<DynamicDataInfo> info = DynamicDataParser.GetDynamicDataInfo("{object[3].Rs[2]['Col']} and {object[4].Other}");
        Assert.HasCount(2, info);
        Assert.AreEqual(3, info[0].ObjectID);
        Assert.AreEqual("Rs", info[0].FieldName);
        Assert.AreEqual("2", info[0].RowIndex);
        Assert.AreEqual("Col", info[0].SubFieldName);
        Assert.AreEqual(4, info[1].ObjectID);
        Assert.AreEqual(string.Empty, info[1].SubFieldName);
    }

    // ----- Iteration count ----------------------------------------------------------------------

    [TestMethod]
    public void IterationCount_for_an_exact_number_uses_the_configured_count()
    {
        TestProbeTaskConfig config = new() { PluginIterationMode = IterationMode.IterateExactNumber, IterationsCount = 5 };

        Assert.AreEqual(5, DynamicDataParser.GetIterationCount(config, [], []));
    }

    [TestMethod]
    public void IterationCount_over_the_default_recordset_uses_its_row_count()
    {
        TestProbeTaskConfig config = new() { PluginIterationMode = IterationMode.IterateDefaultRecordset };

        DynamicDataSet previous = new() { [CommonDynamicData.DefaultRecordsetName] = Recordset("a", "b", "c") };
        Assert.AreEqual(3, DynamicDataParser.GetIterationCount(config, [], previous));

        DynamicDataSet previousList = new() { [CommonDynamicData.DefaultRecordsetName] = new List<Dictionary<string, object>> { new(), new() } };
        Assert.AreEqual(2, DynamicDataParser.GetIterationCount(config, [], previousList));

        Assert.AreEqual(1, DynamicDataParser.GetIterationCount(config, [], []), "No previous recordset means a single run.");
    }

    [TestMethod]
    public void IterationCount_over_an_object_recordset_uses_the_referenced_recordset()
    {
        TestProbeTaskConfig config = new()
        {
            PluginIterationMode = IterationMode.IterateObjectRecordset,
            IterationObject = "{object[7].Result}"
        };
        DynamicDataChain chain = ChainWith(7, ("Result", Recordset("a", "b", "c", "d")));

        Assert.AreEqual(4, DynamicDataParser.GetIterationCount(config, chain, []));
    }

    // ----- Parsing whole configs ----------------------------------------------------------------

    [TestMethod]
    public void Parse_only_touches_properties_marked_as_dynamic_data()
    {
        TestProbeTaskConfig config = new()
        {
            Value = "{object[1].Greeting}",          // marked [DynamicData]: replaced
            Label = "{object[1].Greeting}"           // not marked: left alone
        };

        DynamicDataParser.Parse(config, ChainWith(1, ("Greeting", "hello")), 0, null);

        Assert.AreEqual("hello", config.Value);
        Assert.AreEqual("{object[1].Greeting}", config.Label);
    }
}
