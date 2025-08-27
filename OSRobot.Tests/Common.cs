using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Tests;

internal static class Common
{
    public static Folder CreateRootFolder()
    {
        return new()
        {
            Config = new FolderConfig()
            {
                Id = 0,
                Name = "RootFolder"
            }
        };
    }

    public static (DynamicDataChain, DynamicDataSet, IPluginInstanceLogger) GetTaskDefaultParameters(ITask task)
    {
        return (new DynamicDataChain(), new DynamicDataSet(), PluginInstanceLogger.GetLogger(task));
    }

    public static void ConfigureLogPath()
    {
        PluginInstanceLogger.LogPath = @"Logs\";
    }
}
