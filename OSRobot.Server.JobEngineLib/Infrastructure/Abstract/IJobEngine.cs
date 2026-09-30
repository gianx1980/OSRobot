// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;

namespace OSRobot.Server.JobEngineLib.Infrastructure.Abstract;

public enum ReloadJobsReturnValues
{
    Ok = 0,
    CannotReloadWhileRunningTask,
    GenericError
}

public struct LogInfo
{
    public int FolderId { get; set; }
    public int EventId { get; set; }
    public DateTime ExecDateTime { get; set; }
    public string FileName { get; set; }    
}

public struct FolderInfo
{
    public int Id { get; set; }
    
    public string Name { get; set; }

    public string LogPath { get; set; }
}

public interface IJobEngine
{
    public void Start(CancellationToken cancellationToken = default);

    public void Stop(CancellationToken cancellationToken = default);

    public Task<bool> StartTaskAsync(int taskID, CancellationToken cancellationToken = default);

    public ReloadJobsReturnValues ReloadJobs();

    public List<IPlugin> GetPlugins();

    public IPlugin? GetPlugin(string pluginId);

    public List<LogInfo> GetFolderLogs(int folderId);

    public FolderInfo? GetFolderInfo(int folderId);

    public string? GetLogContent(int folderId, string logFileName);
}
