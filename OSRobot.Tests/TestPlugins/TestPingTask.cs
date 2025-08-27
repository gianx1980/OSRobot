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
        Folder folder = new()
        {
            Config = new FolderConfig()
            {
                Id = 1,
                Name = "RootFolder"
            }
        };

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

        task.Init();

        DynamicDataChain dataChain = [];
        DynamicDataSet dynamicDataSet = new();
        PluginInstanceLogger.LogPath = @"Logs\";
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(task);


        // ---------
        // Act
        // ---------
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
