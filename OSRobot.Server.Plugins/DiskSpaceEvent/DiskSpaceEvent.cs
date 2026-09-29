// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using System.ComponentModel;

namespace OSRobot.Server.Plugins.DiskSpaceEvent;

public class DiskSpaceEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new DiskSpaceEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];
    
    public event EventTriggeredDelegate? EventTriggered;

    private readonly System.Timers.Timer _recurringTimer = new();

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

    public void Init()
    {
        _recurringTimer.Enabled = false;
        _recurringTimer.AutoReset = true;
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;

        DiskSpaceEventConfig config = (DiskSpaceEventConfig)Config;

        int CheckIntervalSeconds = config.CheckIntervalSeconds;
        if (CheckIntervalSeconds <= 0)
            CheckIntervalSeconds = 1;

        _recurringTimer.Interval = new TimeSpan(0, 0, CheckIntervalSeconds).TotalMilliseconds;

        _recurringTimer.Enabled = true;
    }

    public void Destroy()
    {
        _recurringTimer.Dispose();
    }

    private long ThresholdToBytes(long totalDriveSpace, int value, DiskThresholdUnitMeasure unit)
    {
        long result = 0;
        long tempValue = value;
        
        switch (unit)
        {
            case DiskThresholdUnitMeasure.Megabytes:
                result = tempValue * 1024 * 1024;
                break;

            case DiskThresholdUnitMeasure.Gigabytes:
                result = tempValue * 1024 * 1024 * 1024;
                break;

            case DiskThresholdUnitMeasure.Terabytes:
                result = tempValue * 1024 * 1024 * 1024 * 1024;
                break;

            case DiskThresholdUnitMeasure.Percentage:
                result = totalDriveSpace * tempValue / 100;
                break;
        }
                   
        return result;
    }

    private void RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            bool eventTriggered = false;

            if (Config.Log)
                logger.Info(this, "Checking disks space...");

            DiskSpaceEventConfig config = (DiskSpaceEventConfig)Config;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                DiskThreshold? diskTh = config.DiskThresholds.Where(t => t.Disk == drive.Name && drive.DriveType == DriveType.Fixed).FirstOrDefault();

                if (diskTh != null)
                {

                    if ((diskTh.CheckOperator == CheckOperator.LessThan && drive.AvailableFreeSpace < ThresholdToBytes(drive.TotalSize, diskTh.ThresholdValue, diskTh.UnitMeasure))
                        || (diskTh.CheckOperator == CheckOperator.GreaterThan && drive.AvailableFreeSpace > ThresholdToBytes(drive.TotalSize, diskTh.ThresholdValue, diskTh.UnitMeasure)))
                    {
                        eventTriggered = true;

                        DateTime now = DateTime.Now;
                        DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
                        dDataSet.TryAdd("DiskName", drive.Name);
                        dDataSet.TryAdd("DiskSpaceBytes", drive.AvailableFreeSpace);

                        if (Config.Log)
                        {
                            logger.Info(this, $"Disk: {drive.Name} Available free space (bytes) {drive.AvailableFreeSpace}");
                            logger.EventTriggering(this);
                        }
                            
                        OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));
                    }
                }
            }

            if (!eventTriggered && Config.Log)
                logger.Info(this, "No disk with space below the threshold");
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever.
            logger.EventError(this, ex);
        }
    }
}
