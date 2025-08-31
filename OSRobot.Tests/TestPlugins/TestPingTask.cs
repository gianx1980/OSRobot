/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.PingTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestPingTask
{
    [TestMethod]
    public void TestLocalAddress()
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
        (DynamicDataChain dataChain, 
         DynamicDataSet dynamicDataSet, 
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = task.Run(dataChain, dynamicDataSet, 0, logger);
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
    public void TestNonExistentAddress()
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
        (DynamicDataChain dataChain,
         DynamicDataSet dynamicDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = task.Run(dataChain, dynamicDataSet, 0, logger);
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
