// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.PingTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestPingTask
{
    [TestMethod]
    public async Task TestLocalAddress()
    {
        // ---------
        // Arrange
        // ---------
        Folder folder = Common.CreateRootFolder();

        PingTaskConfig config = new()
        {
            Id = 1,
            Name = "Ping task 1",
            Host = "127.0.0.1",
            Attempts = 4,
            Timeout = 1000
        };

        PingTask task = new()
        {
            ParentFolder = folder,
            Config = config
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain, 
         DynamicDataSet dynDataSet, 
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        task.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(result.ExecResults.Count > 0, "There are no executions.");

        // Pinging 127.0.0.1 we expect a 100% success rate
        ExecResult execResult = result.ExecResults[0];
        float thresholdSuccessRate = (float)execResult.Data["ThresholdSuccessRate"];
        Assert.IsTrue(thresholdSuccessRate == 100, "ThresholdSuccessRate is not equal to 100");
    }

    [TestMethod]
    public async Task TestNonExistentAddress()
    {
        // ---------
        // Arrange
        // ---------
        Folder folder = Common.CreateRootFolder();

        PingTaskConfig config = new()
        {
            Id = 1,
            Name = "Ping task 1",
            Host = "192.168.5.5",   // Hope it doesn't exist!
            Attempts = 4,
            Timeout = 1000
        };

        PingTask task = new()
        {
            ParentFolder = folder,
            Config = config
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        task.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(result.ExecResults.Count > 0, "There are no executions.");

        // We are trying to ping a non existent address, 0% success expected
        ExecResult execResult = result.ExecResults[0];
        float thresholdSuccessRate = (float)execResult.Data["ThresholdSuccessRate"];
        Assert.IsTrue(thresholdSuccessRate == 0, "ThresholdSuccessRate is not equal to 0");
    }
}
