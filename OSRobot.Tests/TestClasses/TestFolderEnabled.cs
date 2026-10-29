// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Persistence;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>Disabling a folder: read from its node in the parent folder, and its events (subfolders included) don't start.</summary>
// EngineHarness uses process-wide state (Core, ProbeLog, the trigger registry).
[TestClass]
[DoNotParallelize]
public sealed class TestFolderEnabled
{
    private static Folder Load(JobGraph graph)
    {
        using JsonDocument jsonDoc = JsonDocument.Parse(graph.ToJson());
        return (Folder)new JsonDeserialization(jsonDoc).Deserialize()!;
    }

    private static Folder Child(Folder parent, int id) => parent.Items.OfType<Folder>().Single(f => f.Config.Id == id);

    [TestMethod]
    public void The_enabled_flag_is_read_from_the_folders_node()
    {
        Folder root = Load(new JobGraph()
            .Folder(10, "On", JobGraph.FolderContent(), enabled: true)
            .Folder(20, "Off", JobGraph.FolderContent()
                .Folder(21, "Inside off", JobGraph.FolderContent()), enabled: false)
            .Folder(30, "Saved before the flag existed", JobGraph.FolderContent()));

        Assert.IsTrue(Child(root, 10).Config.Enabled);
        Assert.IsFalse(Child(root, 20).Config.Enabled);
        Assert.IsTrue(Child(Child(root, 20), 21).Config.Enabled, "Its own flag: the parent's state is applied by the engine");
        Assert.IsTrue(Child(root, 30).Config.Enabled, "A missing flag means enabled");
        Assert.AreEqual("Off", Child(root, 20).Config.Name);
    }

    [TestMethod]
    public void The_events_of_a_disabled_folder_and_its_subfolders_do_not_start()
    {
        JobGraph graph = new JobGraph()
            .Folder(10, "Running", JobGraph.FolderContent().Trigger(11), enabled: true)
            .Folder(20, "Paused", JobGraph.FolderContent().Trigger(21)
                .Folder(30, "Inside paused", JobGraph.FolderContent().Trigger(31), enabled: true), enabled: false);

        using EngineHarness harness = new(graph);

        Assert.IsTrue(harness.Fire(), "The root's event runs");
        Assert.IsTrue(TestTriggerEvent.Fire(11), "An event in an enabled folder runs");
        Assert.IsFalse(TestTriggerEvent.Fire(21), "An event in a disabled folder is not started");
        Assert.IsFalse(TestTriggerEvent.Fire(31), "Nor one in a subfolder of a disabled folder");
    }

    [TestMethod]
    public void Start_task_still_runs_a_task_in_a_disabled_folder()
    {
        // As for a disabled task: running it by hand is a deliberate choice, useful to test a paused job.
        JobGraph graph = new JobGraph()
            .Folder(20, "Paused", JobGraph.FolderContent().Task(22, "Manual"), enabled: false);

        using EngineHarness harness = new(graph);

        Assert.IsTrue(harness.Engine.StartTaskAsync(22).GetAwaiter().GetResult());
        Assert.IsTrue(EngineHarness.WaitFor(() => ProbeLog.Find("Manual") != null, TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    public void Re_enabling_a_folder_starts_its_events_after_a_reload()
    {
        string jobsPath;
        using EngineHarness harness = new(new JobGraph()
            .Folder(20, "Paused", JobGraph.FolderContent().Trigger(21), enabled: false));
        Assert.IsFalse(TestTriggerEvent.Fire(21));

        jobsPath = Path.Combine(harness.Config.DataPath, "jobs.json");
        File.WriteAllText(jobsPath, new JobGraph().Folder(20, "Paused", JobGraph.FolderContent().Trigger(21), enabled: true).ToJson());
        harness.Engine.ReloadJobs();

        Assert.IsTrue(TestTriggerEvent.Fire(21));
    }
}
