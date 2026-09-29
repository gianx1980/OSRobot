// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core.Logging;

public class PluginInstanceLogger : IPluginInstanceLogger
{
    public static string LogPath { get; set; } = string.Empty;

    private string _pathFileName = string.Empty;
    private readonly object _lockWriting = new();

    private static int _instanceCounter = 0;
    private readonly static object _lockInstanceCounter = new();

    private static string GetLogFileName(int eventID)
    {
        // You can have the same event trigger multiple times at the same moment.
        // This counter is used to make the log file name unique for each event instance.
        lock (_lockInstanceCounter)
        {
            _instanceCounter++;
        }

        DateTime now = DateTime.Now;
        return $"{eventID}_{now.ToIsoDate().Replace(":", "_")}_{now.Ticks}_{_instanceCounter}.log";
    }

    #pragma warning disable CA1859
    private static IPluginInstanceLogger GetPluginLogger(IPluginInstance plugin)
    {
        //TODO: Check the log path!!!
        string logPath = Path.Combine(LogPath, plugin.ParentFolder?.GetPhysicalFullPath() ?? string.Empty);
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }

        PluginInstanceLogger pluginInstanceLogger = new();
        pluginInstanceLogger.Init(Path.Combine(logPath, GetLogFileName(plugin.Config.Id)));

        return pluginInstanceLogger;
    }
    #pragma warning restore CA1859

    public static IPluginInstanceLogger GetLogger(IEvent robotEvent)
    {
        return GetPluginLogger(robotEvent);
    }

    public static IPluginInstanceLogger GetLogger(ITask robotTask)
    {
        return GetPluginLogger(robotTask);
    }

    private void WriteLine(string text, int position = -1)
    {
        lock (_lockWriting)
        {
            using StreamWriter sw = new(_pathFileName, true);
            if (position != -1)
                sw.BaseStream.Position = position;

            sw.WriteLine(text);
        }
    }

    private string GetCurrentISODateTime()
    {
        return DateTime.Now.ToIsoDate();
    }

    public void TaskStarting(ITask task)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {task.Config.Id}:{task.Config.Name}:{task.GetType().Name} - Starting...");
    }

    public void TaskStarted(ITask task)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {task.Config.Id}:{task.Config.Name}:{task.GetType().Name} - Started");
    }

    public void TaskCompleted(ITask task)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {task.Config.Id}:{task.Config.Name}:{task.GetType().Name} - Completed");
    }

    public void TaskError(ITask task, Exception ex)
    {
        Error(task, "Task error", ex);
    }

    public void TaskIterarionError(ITask task, int iterationIndex, Exception ex)
    {
        Error(task, $"Task error (iterationIndex: {iterationIndex})", ex);
    }

    public void TaskEnded(ITask task)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {task.Config.Id}:{task.Config.Name}:{task.GetType().Name} - Ended");
    }

    public void EventError(IEvent tdrEvent, Exception ex)
    {
        Error(tdrEvent, "Event error", ex);
    }

    public void EventTriggering(IEvent tdrEvent)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {tdrEvent.Config.Id}:{tdrEvent.Config.Name}:{tdrEvent.GetType().Name} - Event triggering");
    }

    public void EventTriggered(IEvent tdrEvent)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {tdrEvent.Config.Id}:{tdrEvent.Config.Name}:{tdrEvent.GetType().Name} - Event triggered");
    }

    public void Error(string text)
    {
        WriteLine($"{GetCurrentISODateTime()} - ERROR - {text}");
    }

    public void Error(string text, Exception ex)
    {
        WriteLine($"{GetCurrentISODateTime()} - ERROR - {text}\n{ex}");
    }

    public void Error(IPluginInstance pluginInstance, string text)
    {
        WriteLine($"{GetCurrentISODateTime()} - ERROR - {pluginInstance.Config.Id}:{pluginInstance.Config.Name}:{pluginInstance.GetType().Name} - {text}");
    }

    public void Error(IPluginInstance pluginInstance, string text, Exception ex)
    {
        WriteLine($"{GetCurrentISODateTime()} - ERROR - {pluginInstance.Config.Id}:{pluginInstance.Config.Name}:{pluginInstance.GetType().Name} - {text}\n{ex}");
    }

    public void Info(string text)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {text}");
    }

    public void Info(string text, Exception ex)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {text}\n{ex}");
    }

    public void Info(IPluginInstance pluginInstance, string text)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {pluginInstance.Config.Id}:{pluginInstance.Config.Name}:{pluginInstance.GetType().Name} - {text}");
    }

    public void Info(IPluginInstance pluginInstance, string text, Exception ex)
    {
        WriteLine($"{GetCurrentISODateTime()} - INFO - {pluginInstance.Config.Id}:{pluginInstance.Config.Name}:{pluginInstance.GetType().Name} - {text}\n{ex}");
    }

    public void Init(string pathFileName)
    {
        _pathFileName = pathFileName;
    }
}
