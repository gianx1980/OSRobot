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

    public static (DynamicDataChain, DynamicDataSet, IPluginInstanceLogger) GetEventDefaultParameters(IEvent osrEvent)
    {
        return (new DynamicDataChain(), new DynamicDataSet(), PluginInstanceLogger.GetLogger(osrEvent));
    }

    public static void ConfigureLogPath()
    {
        PluginInstanceLogger.LogPath = @"Logs\";
    }

    public static object? CallPrivateMethod(object o, string methodName, params object[] args)
    {
        var mi = o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mi != null)
        {
            return mi.Invoke(o, args);
        }
        return null;
    }

    public static void WriteTestFile(string filePathName, string content)
    {
        using FileStream fs = new(filePathName, FileMode.Create);
        using StreamWriter sw = new(fs);
        sw.WriteLine(content);
    }
}
