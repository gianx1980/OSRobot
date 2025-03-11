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

namespace OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

public enum ReloadJobsReturnValues
{
    Ok = 0,
    CannotReloadWhileRunningTask,
    GenericError
}

public struct LogInfoItem
{
    public int FolderId { get; set; }
    public int EventId { get; set; }
    public DateTime ExecDateTime { get; set; }
    public string FileName { get; set; }    
}

public interface IJobEngine
{
    public void Start();

    public void Stop();

    public bool StartTask(int taskID);

    public ReloadJobsReturnValues ReloadJobs();

    public List<IPlugin> GetPlugins();

    public IPlugin? GetPlugin(string pluginId);

    public List<LogInfoItem> GetFolderLogs(int folderId);

    public string? GetLogContent(int folderId, string logFileName);
}
