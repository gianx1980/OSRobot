// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.RunProgramTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestRunProgramTask
{
    private static string ShellPath => OperatingSystem.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "/bin/sh";

    // Arguments for ShellPath that run the given shell command.
    private static string ShellArgs(string command) => OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"";

    private static RunProgramTask CreateTask(RunProgramTaskConfig config)
    {
        Folder folder = Common.CreateRootFolder();
        Common.ConfigureLogPath();

        return new RunProgramTask()
        {
            ParentFolder = folder,
            Config = config
        };
    }

    private static RunProgramTaskConfig CreateConfig(string command, bool failOnNonZeroExitCode = true) => new()
    {
        Id = 1,
        Name = "Run program task 1",
        ProgramPath = ShellPath,
        Parameters = ShellArgs(command),
        FailOnNonZeroExitCode = failOnNonZeroExitCode
    };

    private static async Task<InstanceExecResult> RunAsync(RunProgramTask task, CancellationToken cancellationToken)
    {
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        task.Init();
        try
        {
            return await task.RunAsync(dynDataChain, dynDataSet, 0, logger, cancellationToken);
        }
        finally
        {
            task.Destroy();
        }
    }

    [TestMethod]
    public async Task A_zero_exit_code_is_a_success()
    {
        RunProgramTask task = CreateTask(CreateConfig("exit 0"));

        InstanceExecResult result = await RunAsync(task, CancellationToken.None);

        Assert.HasCount(1, result.ExecResults);
        Assert.IsTrue(result.ExecResults[0].Result);
    }

    [TestMethod]
    public async Task A_non_zero_exit_code_is_a_failure()
    {
        RunProgramTask task = CreateTask(CreateConfig("exit 3"));

        InstanceExecResult result = await RunAsync(task, CancellationToken.None);

        Assert.HasCount(1, result.ExecResults);
        Assert.IsFalse(result.ExecResults[0].Result);
    }

    [TestMethod]
    public async Task A_non_zero_exit_code_is_a_success_when_the_check_is_disabled()
    {
        RunProgramTask task = CreateTask(CreateConfig("exit 3", failOnNonZeroExitCode: false));

        InstanceExecResult result = await RunAsync(task, CancellationToken.None);

        Assert.HasCount(1, result.ExecResults);
        Assert.IsTrue(result.ExecResults[0].Result);
    }

    [TestMethod]
    public void The_check_is_enabled_by_default()
    {
        Assert.IsTrue(new RunProgramTaskConfig().FailOnNonZeroExitCode);
    }

    [TestMethod]
    public async Task Cancelling_the_task_kills_the_program_and_its_children()
    {
        // The shell starts a long-running child: both must be gone once the task is cancelled.
        string childName = OperatingSystem.IsWindows() ? "PING" : "sleep";
        string childCommand = OperatingSystem.IsWindows() ? "ping -n 120 127.0.0.1 > nul" : "sleep 120";
        HashSet<int> childrenBefore = [.. Process.GetProcessesByName(childName).Select(p => p.Id)];

        RunProgramTask task = CreateTask(CreateConfig(childCommand));
        using CancellationTokenSource cts = new();
        Task<InstanceExecResult> runTask = RunAsync(task, cts.Token);

        // Wait for the child to show up.
        List<Process> newChildren = [];
        Stopwatch sw = Stopwatch.StartNew();
        while (newChildren.Count == 0 && sw.Elapsed < TimeSpan.FromSeconds(15))
        {
            await Task.Delay(100);
            newChildren = [.. Process.GetProcessesByName(childName).Where(p => !childrenBefore.Contains(p.Id))];
        }
        Assert.IsNotEmpty(newChildren, "The program's child process never started.");

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);

        foreach (Process child in newChildren)
        {
            Assert.IsTrue(child.WaitForExit(TimeSpan.FromSeconds(10)), $"Child process {child.Id} is still running after the task was cancelled.");
            child.Dispose();
        }
    }
}
