// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.TaskFailureEvent;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestPlugins;

/// <summary>The event on its own, fed failures directly: scope, filtering, grouping. See TestTaskFailureReporting for the engine side.</summary>
[TestClass]
public sealed class TestTaskFailureEvent
{
    private sealed class CapturingSink : IEventSink
    {
        private readonly object _gate = new();
        private readonly List<DynamicDataSet> _published = [];

        public List<DynamicDataSet> Published { get { lock (_gate) return [.. _published]; } }

        public bool Publish(IEvent source, DynamicDataSet dynamicData, IPluginInstanceLogger logger)
        {
            lock (_gate)
                _published.Add(dynamicData);
            return true;
        }

        public bool WaitFor(int count, TimeSpan timeout) => EngineHarness.WaitFor(() => Published.Count >= count, timeout);
    }

    // Root (0) > Backups (1) > Nightly (2), and Root > Other (3).
    private readonly Folder _root = Common.CreateRootFolder();
    private Folder _backups = null!;
    private Folder _nightly = null!;
    private Folder _other = null!;

    [TestInitialize]
    public void CreateFolders()
    {
        Common.ConfigureLogPath();
        _backups = AddFolder(_root, 1, "Backups");
        _nightly = AddFolder(_backups, 2, "Nightly");
        _other = AddFolder(_root, 3, "Other");
    }

    private static Folder AddFolder(Folder parent, int id, string name)
    {
        Folder folder = new() { ParentFolder = parent, Config = new FolderConfig { Id = id, Name = name, Enabled = true, Log = true } };
        parent.Add(folder);
        return folder;
    }

    private static TestProbeTask Task(Folder folder, int id, string name)
    {
        return new TestProbeTask { ParentFolder = folder, Config = new TestProbeTaskConfig { Id = id, Name = name } };
    }

    private static TaskFailure Failure(ITask task, bool handled = false, string message = "boom", int? iteration = 0)
    {
        return new TaskFailure(task, iteration, message, DateTime.Now, handled);
    }

    private (TaskFailureEvent Event, CapturingSink Sink) Event(Folder folder, TaskFailureEventScope scope = TaskFailureEventScope.ThisFolderAndSubfolders,
                                                              bool onlyUnhandled = true, int groupForSeconds = 0)
    {
        TaskFailureEvent failureEvent = new()
        {
            ParentFolder = folder,
            Config = new TaskFailureEventConfig { Id = 50, Name = "Failures", Scope = scope, ReportOnlyUnhandled = onlyUnhandled, GroupForSeconds = groupForSeconds }
        };
        CapturingSink sink = new();
        failureEvent.Init(sink);
        return (failureEvent, sink);
    }

    [TestMethod]
    public void A_failure_is_published_with_its_details()
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups);

        failureEvent.OnTaskFailed(Failure(Task(_nightly, 12, "Nightly backup"), message: "The network path was not found.", iteration: 3));

        Assert.IsTrue(sink.WaitFor(1, TimeSpan.FromSeconds(5)));
        DynamicDataSet data = sink.Published.Single();
        Assert.AreEqual(12, data[TaskFailureEvent.FailedObjectID]);
        Assert.AreEqual("Nightly backup", data[TaskFailureEvent.FailedObjectName]);
        Assert.AreEqual("Test probe task", data[TaskFailureEvent.FailedObjectType]);
        Assert.AreEqual("Backups/Nightly", data[TaskFailureEvent.FailedObjectFolder]);
        Assert.AreEqual("The network path was not found.", data[TaskFailureEvent.FailureMessage]);
        Assert.AreEqual(3, data[TaskFailureEvent.FailedIterationIndex]);
        Assert.AreEqual(1, data[TaskFailureEvent.FailureCount]);
        Assert.AreEqual(1, ((DataTable)data[TaskFailureEvent.Failures]).Rows.Count);
        Assert.AreEqual(50, data[CommonDynamicData.ObjectID]);
    }

    [TestMethod]
    public void Handled_failures_are_skipped_unless_asked_for()
    {
        (TaskFailureEvent onlyUnhandled, CapturingSink skipped) = Event(_backups);
        (TaskFailureEvent everything, CapturingSink reported) = Event(_backups, onlyUnhandled: false);
        TestProbeTask task = Task(_backups, 12, "A");

        onlyUnhandled.OnTaskFailed(Failure(task, handled: true));
        everything.OnTaskFailed(Failure(task, handled: true));

        Assert.IsTrue(reported.WaitFor(1, TimeSpan.FromSeconds(5)));
        Thread.Sleep(200);
        Assert.AreEqual(0, skipped.Published.Count);
    }

    [TestMethod]
    [DataRow(TaskFailureEventScope.ThisFolderAndSubfolders, "Backups,Nightly")]
    [DataRow(TaskFailureEventScope.ThisFolder, "Backups")]
    [DataRow(TaskFailureEventScope.AllFolders, "Root,Backups,Nightly,Other")]
    public void The_scope_is_relative_to_the_events_folder(TaskFailureEventScope scope, string expectedFolders)
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups, scope);
        Dictionary<string, Folder> folders = new() { ["Root"] = _root, ["Backups"] = _backups, ["Nightly"] = _nightly, ["Other"] = _other };

        foreach ((string name, Folder folder) in folders)
            failureEvent.OnTaskFailed(Failure(Task(folder, folder.Config.Id + 100, name)));

        string[] expected = expectedFolders.Split(',');
        Assert.IsTrue(sink.WaitFor(expected.Length, TimeSpan.FromSeconds(5)));
        Thread.Sleep(200);
        CollectionAssert.AreEquivalent(expected, sink.Published.Select(d => (string)d[TaskFailureEvent.FailedObjectName]).ToArray());
    }

    [TestMethod]
    public void Grouped_failures_fire_once_at_the_end_of_the_window()
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups, groupForSeconds: 1);

        failureEvent.OnTaskFailed(Failure(Task(_backups, 1, "First"), message: "first error"));
        failureEvent.OnTaskFailed(Failure(Task(_backups, 2, "Second")));
        failureEvent.OnTaskFailed(Failure(Task(_nightly, 3, "Third")));

        Thread.Sleep(500);
        Assert.AreEqual(0, sink.Published.Count, "Nothing before the window closes");

        Assert.IsTrue(sink.WaitFor(1, TimeSpan.FromSeconds(5)));
        Thread.Sleep(200);
        DynamicDataSet data = sink.Published.Single();
        Assert.AreEqual(3, data[TaskFailureEvent.FailureCount]);
        Assert.AreEqual(3, ((DataTable)data[TaskFailureEvent.Failures]).Rows.Count);
        Assert.AreEqual("First", data[TaskFailureEvent.FailedObjectName], "The single fields describe the first failure");
        Assert.AreEqual("first error", data[TaskFailureEvent.FailureMessage]);
    }

    [TestMethod]
    public void A_steady_stream_of_failures_is_still_reported_every_window()
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups, groupForSeconds: 1);
        TestProbeTask task = Task(_backups, 1, "Flapping");

        // A failure every 200 ms for 2.6 s: waiting for a quiet moment would never fire.
        for (int i = 0; i < 13; i++)
        {
            failureEvent.OnTaskFailed(Failure(task));
            Thread.Sleep(200);
        }

        Assert.IsTrue(sink.Published.Count >= 2, $"Published {sink.Published.Count} time(s)");
    }

    [TestMethod]
    public void The_recordset_is_capped_but_the_count_is_not()
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups, groupForSeconds: 1);
        TestProbeTask task = Task(_backups, 1, "Loop");

        for (int i = 0; i < TaskFailureEvent.MaxFailuresPerEvent + 100; i++)
            failureEvent.OnTaskFailed(Failure(task, iteration: i));

        Assert.IsTrue(sink.WaitFor(1, TimeSpan.FromSeconds(5)));
        DynamicDataSet data = sink.Published.Single();
        Assert.AreEqual(TaskFailureEvent.MaxFailuresPerEvent + 100, data[TaskFailureEvent.FailureCount]);
        Assert.AreEqual(TaskFailureEvent.MaxFailuresPerEvent, ((DataTable)data[TaskFailureEvent.Failures]).Rows.Count);
    }

    [TestMethod]
    public void Destroy_drops_an_open_window()
    {
        (TaskFailureEvent failureEvent, CapturingSink sink) = Event(_backups, groupForSeconds: 1);

        failureEvent.OnTaskFailed(Failure(Task(_backups, 1, "A")));
        failureEvent.Destroy();
        failureEvent.OnTaskFailed(Failure(Task(_backups, 2, "B")));

        Thread.Sleep(1500);
        Assert.AreEqual(0, sink.Published.Count);
    }
}
