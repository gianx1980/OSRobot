// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Persistence;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestCloneTaskForRun
{
    // Event -> 2 -> 3 -> 4, all in the root folder: the middle task has a parent folder full of
    // other objects and a connection leading further down the graph.
    private static ITask LoadMiddleTask()
    {
        JobGraph graph = new JobGraph()
            .Task(2, "A")
            .Task(3, "B", value: "{object[2].Value}")
            .Task(4, "C")
            .Connect(JobGraph.EventId, 2)
            .Connect(2, 3)
            .Connect(3, 4);

        using JsonDocument jsonDoc = JsonDocument.Parse(graph.ToJson());
        Folder rootFolder = (Folder)new JsonDeserialization(jsonDoc).Deserialize()!;
        return rootFolder.Items.OfType<ITask>().Single(t => t.Config.Id == 3);
    }

    private static string Serialize(object o) => JsonSerializer.Serialize(o, o.GetType());

    [TestMethod]
    public void Clone_is_a_new_instance_of_the_same_type()
    {
        ITask definition = LoadMiddleTask();

        ITask clone = CoreHelpers.CloneTaskForRun(definition);

        Assert.AreNotSame(definition, clone);
        Assert.AreEqual(definition.GetType(), clone.GetType());
    }

    [TestMethod]
    public void Clone_has_its_own_copy_of_the_config()
    {
        ITask definition = LoadMiddleTask();

        ITask clone = CoreHelpers.CloneTaskForRun(definition);

        Assert.AreNotSame(definition.Config, clone.Config);
        Assert.AreEqual(Serialize(definition.Config), Serialize(clone.Config));
    }

    [TestMethod]
    public void Clone_shares_the_definitions_folder_and_connections_instead_of_copying_the_graph()
    {
        // Copying them made every run serialize the whole folder and everything downstream,
        // so the cost of running one task grew with the size of the workspace.
        ITask definition = LoadMiddleTask();

        ITask clone = CoreHelpers.CloneTaskForRun(definition);

        Assert.AreSame(definition.ParentFolder, clone.ParentFolder);
        Assert.AreSame(definition.Connections, clone.Connections);
    }
}
