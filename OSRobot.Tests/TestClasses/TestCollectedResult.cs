// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>How CommonDynamicData.BuildCollectedResult folds the results of all the iterations into one.</summary>
[TestClass]
public sealed class TestCollectedResult
{
    private const string Recordset = CommonDynamicData.DefaultRecordsetName;

    private static readonly TestProbeTask Source = new() { Config = new TestProbeTaskConfig { Id = 2, Name = "A" } };

    private static ExecResult Result(bool succeeded, params (string Key, object Value)[] fields)
    {
        DynamicDataSet data = [];
        foreach ((string key, object value) in fields)
            data[key] = value;
        return new ExecResult(succeeded, data);
    }

    private static DataTable Table(params (string Name, Type Type)[] columns)
    {
        DataTable table = new();
        foreach ((string name, Type type) in columns)
            table.Columns.Add(name, type);
        return table;
    }

    private static DataTable Collect(params ExecResult[] results) =>
        (DataTable)CommonDynamicData.BuildCollectedResult(Source, [.. results], EnumCollectedResultRule.AllSucceeded).Data[Recordset];

    [TestMethod]
    public void The_recordsets_of_all_the_iterations_are_unioned_in_order_and_other_fields_keep_the_last_value()
    {
        DataTable first = Table(("Name", typeof(string)));
        first.Rows.Add("a");
        first.Rows.Add("b");
        DataTable second = Table(("Name", typeof(string)));
        second.Rows.Add("c");

        ExecResult collected = CommonDynamicData.BuildCollectedResult(Source,
            [Result(true, (Recordset, first), ("Value", "v1")), Result(true, (Recordset, second), ("Value", "v2"))],
            EnumCollectedResultRule.AllSucceeded);

        DataTable union = (DataTable)collected.Data[Recordset];
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, union.Rows.Cast<DataRow>().Select(r => (string)r["Name"]).ToArray());
        Assert.AreEqual("v2", collected.Data["Value"]);

        // IterationResults keeps each iteration's own recordset, untouched.
        List<Dictionary<string, object>> results = (List<Dictionary<string, object>>)collected.Data[CommonDynamicData.IterationResults];
        Assert.AreSame(first, results[0][Recordset]);
        Assert.AreSame(second, results[1][Recordset]);
    }

    [TestMethod]
    public void A_column_missing_from_an_iteration_is_empty_in_its_rows()
    {
        DataTable first = Table(("Name", typeof(string)));
        first.Rows.Add("a");
        DataTable second = Table(("Name", typeof(string)), ("Size", typeof(long)));
        second.Rows.Add("b", 42L);

        DataTable union = Collect(Result(true, (Recordset, first)), Result(true, (Recordset, second)));

        Assert.HasCount(2, union.Columns);
        Assert.AreEqual(DBNull.Value, union.Rows[0]["Size"]);
        Assert.AreEqual(42L, union.Rows[1]["Size"]);
    }

    [TestMethod]
    public void A_column_with_different_types_in_different_iterations_fails_with_a_clear_error()
    {
        DataTable first = Table(("Size", typeof(int)));
        DataTable second = Table(("Size", typeof(string)));

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() =>
            Collect(Result(true, (Recordset, first)), Result(true, (Recordset, second))));

        StringAssert.Contains(ex.Message, "'Size'");
        StringAssert.Contains(ex.Message, $"'{Recordset}'");
    }

    [TestMethod]
    public void Rows_with_the_same_primary_key_are_all_kept()
    {
        // DataTable.Merge would combine these two rows into one.
        DataTable first = Table(("Id", typeof(int)), ("Name", typeof(string)));
        first.PrimaryKey = [first.Columns["Id"]!];
        first.Rows.Add(1, "first");
        DataTable second = Table(("Id", typeof(int)), ("Name", typeof(string)));
        second.PrimaryKey = [second.Columns["Id"]!];
        second.Rows.Add(1, "second");

        DataTable union = Collect(Result(true, (Recordset, first)), Result(true, (Recordset, second)));

        CollectionAssert.AreEqual(new[] { "first", "second" }, union.Rows.Cast<DataRow>().Select(r => (string)r["Name"]).ToArray());
    }

    [TestMethod]
    public void A_failed_iteration_without_a_recordset_adds_no_rows_but_still_counts()
    {
        DataTable first = Table(("Name", typeof(string)));
        first.Rows.Add("a");
        DataTable third = Table(("Name", typeof(string)));
        third.Rows.Add("c");

        ExecResult collected = CommonDynamicData.BuildCollectedResult(Source,
            [Result(true, (Recordset, first)), Result(false), Result(true, (Recordset, third))],
            EnumCollectedResultRule.AllSucceeded);

        Assert.HasCount(2, ((DataTable)collected.Data[Recordset]).Rows);
        Assert.HasCount(3, (List<Dictionary<string, object>>)collected.Data[CommonDynamicData.IterationResults]);
        Assert.IsFalse(collected.Result, "With a failed iteration, 'all succeeded' is not satisfied.");
    }

    [TestMethod]
    public void Lists_of_rows_are_concatenated()
    {
        List<Dictionary<string, object>> first = [new() { ["Name"] = "a" }];
        List<Dictionary<string, object>> second = [new() { ["Name"] = "b" }, new() { ["Other"] = 1 }];

        ExecResult collected = CommonDynamicData.BuildCollectedResult(Source,
            [Result(true, (Recordset, first)), Result(true, (Recordset, second))],
            EnumCollectedResultRule.AllSucceeded);

        List<Dictionary<string, object>> union = (List<Dictionary<string, object>>)collected.Data[Recordset];
        Assert.HasCount(3, union);
        Assert.AreEqual("b", union[1]["Name"]);
        Assert.AreEqual(1, union[2]["Other"]);
    }

    [TestMethod]
    public void Tables_and_lists_of_rows_in_the_same_field_fail_with_a_clear_error()
    {
        List<Dictionary<string, object>> list = [new() { ["Name"] = "a" }];

        ApplicationException ex = Assert.ThrowsExactly<ApplicationException>(() =>
            Collect(Result(true, (Recordset, Table(("Name", typeof(string))))), Result(true, (Recordset, list))));

        StringAssert.Contains(ex.Message, "some are tables and some are lists of rows");
    }
}
