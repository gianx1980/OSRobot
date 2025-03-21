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
