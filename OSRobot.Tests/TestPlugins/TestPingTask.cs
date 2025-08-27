using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.PingTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestPingTask
{
    [TestMethod]
    public void TestPlugin()
    {
        // ---------
        // Arrange
        // ---------
        Folder folder = Common.CreateRootFolder();

        PingTaskConfig config = new()
        {
            Id = 1,
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
}
