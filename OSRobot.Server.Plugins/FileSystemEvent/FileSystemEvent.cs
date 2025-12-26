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
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.DateTimeEvent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace OSRobot.Server.Plugins.FileSystemEvent;

public class FileSystemEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new FileSystemEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public event EventTriggeredDelegate? EventTriggered;

    protected virtual void OnEventTriggered(EventTriggeredEventArgs e)
    {
        EventTriggeredDelegate? handler = EventTriggered;
        if (handler != null)
        {
            foreach (EventTriggeredDelegate singleCast in handler.GetInvocationList().Cast<EventTriggeredDelegate>())
            {
                if ((singleCast.Target is ISynchronizeInvoke syncInvoke) && (syncInvoke.InvokeRequired))
                    syncInvoke.Invoke(singleCast, [this, e]);
                else
                    singleCast(this, e);
            }
        }
    }

    private readonly List<FileSystemWatcher> _fileSystemWatchers = [];

    public void Init()
    {
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
                
            OnEventTriggered(new EventTriggeredEventArgs(dDataSet, Logger));
        }
        catch (Exception ex)
        {
            if (Config.Log)
                Logger.EventError(this, ex);
        }
    }
}
