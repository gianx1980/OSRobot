// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Persistence;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestDynamicDataValidator
{
    private static List<DynamicDataIssue> Validate(JobGraph graph)
    {
        using JsonDocument jsonDoc = JsonDocument.Parse(graph.ToJson());
        Folder rootFolder = (Folder)new JsonDeserialization(jsonDoc).Deserialize()!;
        return DynamicDataValidator.Validate(rootFolder);
    }

    private static DynamicDataIssue Single(List<DynamicDataIssue> issues)
    {
        Assert.AreEqual(1, issues.Count, string.Join(Environment.NewLine, issues));
        return issues[0];
    }

    [TestMethod]
    public void Valid_references_to_upstream_objects_produce_no_issues()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "hello")
            .Task(3, "B", value: "{object[2].Value} {object[1].ExecutionStartDateYear} {environment['PATH']}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3);

        Assert.AreEqual(0, Validate(graph).Count);
    }

    [TestMethod]
    public void A_reference_to_an_object_that_does_not_exist_is_reported()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "{object[99].Value}")
            .Connect(JobGraph.EventId, 2);

        DynamicDataIssue issue = Single(Validate(graph));
        Assert.AreEqual(2, issue.ObjectId);
        Assert.AreEqual("Value", issue.Location);
        Assert.AreEqual("{object[99].Value}", issue.Reference);
        StringAssert.Contains(issue.Message, "does not exist");
    }

    [TestMethod]
    public void A_reference_to_an_object_on_another_branch_is_reported()
    {
        // 1 -> 2 and 1 -> 3: object 2 never runs before object 3.
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B", value: "{object[2].Value}")
            .Connect(JobGraph.EventId, 2)
            .Connect(JobGraph.EventId, 3);

        StringAssert.Contains(Single(Validate(graph)).Message, "does not run before this object");
    }

    [TestMethod]
    public void A_reference_to_a_downstream_object_or_to_itself_is_reported()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "{object[3].Value} {object[2].Value}")
            .Task(3, "B")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3);

        Assert.AreEqual(2, Validate(graph).Count(i => i.Message.Contains("does not run before this object")));
    }

    [TestMethod]
    public void A_field_the_object_does_not_output_is_reported_with_a_case_hint()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B", value: "{object[2].value} {object[2].Nope}")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3);

        List<DynamicDataIssue> issues = Validate(graph);

        Assert.AreEqual(2, issues.Count);
        StringAssert.Contains(issues.Single(i => i.Reference == "{object[2].value}").Message, "did you mean 'Value'?");
        StringAssert.Contains(issues.Single(i => i.Reference == "{object[2].Nope}").Message, "has no field 'Nope'");
    }

    [TestMethod]
    public void References_nested_in_lists_of_sub_objects_are_checked()
    {
        JobGraph graph = new JobGraph()
            .Node(new
            {
                pluginId = "WriteTextFileTask",
                id = 2,
                name = "Log",
                columnsDefinition = new[]
                {
                    new { headerTitle = "Site", fieldValue = "{object[1].ObjectName}", fieldWidth = "" },
                    new { headerTitle = "Result", fieldValue = "{object[7].ExecutionResult}", fieldWidth = "" }
                }
            })
            .Connect(JobGraph.EventId, 2);

        DynamicDataIssue issue = Single(Validate(graph));
        Assert.AreEqual("ColumnsDefinition[1].FieldValue", issue.Location);
    }

    [TestMethod]
    public void Code_expressions_are_not_checked()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A", value: "[CODE]return \"{object[99].Value}\";")
            .Connect(JobGraph.EventId, 2);

        Assert.AreEqual(0, Validate(graph).Count);
    }

    [TestMethod]
    public void A_connection_condition_on_a_field_the_source_does_not_output_is_reported()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B")
            .Task(4, "C")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3, executeOperators: ["ValueEqualsTo"], dynamicDataCode: "Value")
            .Connect(2, 4, executeOperators: ["ValueEqualsTo"], dynamicDataCode: "Missing");

        DynamicDataIssue issue = Single(Validate(graph));
        Assert.AreEqual(2, issue.ObjectId);
        Assert.AreEqual("Connection to 4", issue.Location);
        Assert.AreEqual("Missing", issue.Reference);
    }
}
