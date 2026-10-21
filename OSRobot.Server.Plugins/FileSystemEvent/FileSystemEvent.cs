// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.DateTimeEvent;
using System;
using System.Collections.Generic;
using System.IO;

namespace OSRobot.Server.Plugins.FileSystemEvent;

public class FileSystemEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new FileSystemEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    private IEventSink? _sink;

    private readonly List<FileSystemWatcher> _fileSystemWatchers = [];

    public void Init(IEventSink sink)
    {
        _sink = sink;

        FileSystemEventConfig config = (FileSystemEventConfig)Config;
        foreach (FolderToMonitor folder in config.FoldersToMonitor)
        {
            FileSystemWatcher watcher = new()
            {
                Path = folder.Path,
                IncludeSubdirectories = folder.MonitorSubFolders
            };

            if (folder.MonitorAction == MonitorActionType.NewFiles)
                watcher.Created += WatcherEvent;
            else if (folder.MonitorAction == MonitorActionType.ModifiedFiles)
                watcher.Changed += WatcherEvent;
            else
                watcher.Deleted += WatcherEvent;

            _fileSystemWatchers.Add(watcher);
        }

        foreach (FileSystemWatcher watcher in _fileSystemWatchers)
        {
            watcher.EnableRaisingEvents = true;
        }
    }

    public void Destroy()
    {
        foreach (FileSystemWatcher fWatcher in _fileSystemWatchers)
        {
            fWatcher.Dispose();
        }
    }

    private void WatcherEvent(object sender, FileSystemEventArgs e)
    {
        IPluginInstanceLogger Logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (Config.Log)
                Logger.EventTriggered(this);
            DateTime now = DateTime.Now;
            FileSystemEventConfig tConfig = (FileSystemEventConfig)Config;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
            
            FileInfo fi = new(e.FullPath);
            dDataSet.TryAdd(FileSystemEventCommon.DynDataKeyFullPathName, e.FullPath);
            dDataSet.TryAdd(FileSystemEventCommon.DynDataKeyFileName, e.Name ?? string.Empty);
            dDataSet.TryAdd(FileSystemEventCommon.DynDataKeyFileNameWithoutExtension, e.Name == null ? string.Empty : e.Name[..^fi.Extension.Length]);
            dDataSet.TryAdd(FileSystemEventCommon.DynDataKeyFileExtension, fi.Extension[1..]);
            dDataSet.TryAdd(FileSystemEventCommon.DynDataKeyChangeType, e.ChangeType.ToString());
            
            if (Config.Log)
            {
                Logger.Info(this, $"File detected {e.FullPath}");
                Logger.EventTriggering(this);
            }
                
            _sink?.Publish(this, dDataSet, Logger);
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever.
            Logger.EventError(this, ex);
        }
    }
}
