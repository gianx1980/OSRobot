// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Logging.Abstract;

public enum PlugingInstanceLoggerStatus
{
    Running,
    Completed,
    Error
}

public interface IPluginInstanceLogger
{
    void Init(string pathFileName);

    void TaskStarting(ITask task);

    void TaskStarted(ITask task);

    void TaskCompleted(ITask task);

    void TaskError(ITask task, Exception ex);

    void TaskEnded(ITask task);

    void TaskIterationError(ITask task, int iterationIndex, Exception ex);


    void EventError(IEvent tdrEvent, Exception ex);
    void EventTriggering(IEvent tdrEvent);
    void EventTriggered(IEvent tdrEvent);



    void Info(string text);
    void Info(string text, Exception ex);
    void Info(IPluginInstance pluginInstance, string text);
    void Info(IPluginInstance pluginInstance, string text, Exception ex);

    void Error(string text);
    void Error(string text, Exception ex);
    void Error(IPluginInstance pluginInstance, string text);
    void Error(IPluginInstance pluginInstance, string text, Exception ex);
}
