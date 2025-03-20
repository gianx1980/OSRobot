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

using NickStrupat;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using System.ComponentModel;
using System.Diagnostics;


namespace OSRobot.Server.Plugins.MemoryEvent;

public class MemoryUsageSample(float sampleValue)
{
    public DateTime SampleDateTime { get; set; } = DateTime.Now;

    public float SampleValue { get; set; } = sampleValue;
}

public class MemoryEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }
    public int ID { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new MemoryEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public event EventTriggeredDelegate? EventTriggered;

    private System.Timers.Timer? _recurringTimer;

    private readonly ComputerInfo _computerInfo = new();

    private List<MemoryUsageSample> _memoryUsageSamples = [];

    private DateTime _dateLastTrigger;

    private DateTime _dateFirstSample = DateTime.MinValue;

    private const int _defaultIntervalMinutes = 5;

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
        _memoryUsageSamples = [];
        _recurringTimer = new()
        {
            Enabled = false,
            AutoReset = true
        };
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;
        _recurringTimer.Enabled = true;

        MemoryEventConfig tConfig = (MemoryEventConfig)Config;

        int checkIntervalSeconds = tConfig.CheckIntervalSeconds;
        if (checkIntervalSeconds <= 0)
            checkIntervalSeconds = 1;

        _recurringTimer.Interval = new TimeSpan(0, 0, checkIntervalSeconds).TotalMilliseconds;
    }

    public void Destroy()
    {
        _recurringTimer?.Dispose();
    }

    private void RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger Logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            bool triggerEvent = false;

            if (Config.Log)
                Logger.Info(this, "Checking Memory usage...");

            MemoryEventConfig tConfig = (MemoryEventConfig)Config;

            if (_dateFirstSample == DateTime.MinValue)
                _dateFirstSample = DateTime.Now;

            float totalMemory = _computerInfo.TotalPhysicalMemory;
            float usedMemory = _computerInfo.TotalPhysicalMemory - _computerInfo.AvailablePhysicalMemory;
            float memoryUsage = usedMemory / totalMemory * 100; 

            Debug.WriteLine($"Samples Count before: {_memoryUsageSamples.Count}");
            if (tConfig.TriggerIfAvgUsageIsAboveThresholdLastXMin)
                _memoryUsageSamples.Add(new MemoryUsageSample(memoryUsage));
            Debug.WriteLine($"Samples Count after: {_memoryUsageSamples.Count}");

            if (tConfig.TriggerIfPassedXMinFromLastTrigger
                && DateTime.Now.Subtract(_dateLastTrigger).TotalMinutes < tConfig.MinutesFromLastTrigger)
            {
                if (Config.Log)
                    Logger.Info(this, "Minimum trigger time not elapsed");
                return;
            }

            Debug.WriteLine($"Object hash: {this.GetHashCode()}, MemoryUsage: {memoryUsage}, Threshold: {tConfig.Threshold}");

            if (tConfig.TriggerIfUsageIsAboveThreshold)
            {
                if (memoryUsage > tConfig.Threshold)
                    triggerEvent = true;
            }
            else if (tConfig.TriggerIfAvgUsageIsAboveThresholdLastXMin)
            {
                DateTime dtFrom = DateTime.Now.Subtract(new TimeSpan(0, tConfig.AvgIntervalMinutes ?? _defaultIntervalMinutes, 0));
                memoryUsage = _memoryUsageSamples.Where(t => t.SampleDateTime >= dtFrom).DefaultIfEmpty().Average(t => t == null ? 0 : t.SampleValue);

                Debug.WriteLine($"Memory Average Usage: {memoryUsage}, Threshold: {tConfig.Threshold}, Samples Count: {_memoryUsageSamples.Count}");

                // Before starting triggering the event, we want at least some samples for duration of TConfig.AvgIntervalMinutes minutes
                if (DateTime.Now.Subtract(_dateFirstSample).TotalMinutes > tConfig.AvgIntervalMinutes
                    && memoryUsage > tConfig.ThresholdLastXMin)
                    triggerEvent = true;

                _memoryUsageSamples.RemoveAll(t => t.SampleDateTime < dtFrom);
            }

            if (triggerEvent)
            {
                DateTime now = DateTime.Now;
                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
                dDataSet.Add("MemoryUsagePercentage", memoryUsage);

                if (Config.Log)
                {
                    Logger.Info(this, $"Memory usage %: {memoryUsage}");
                    Logger.EventTriggering(this);
                }
                    
                OnEventTriggered(new EventTriggeredEventArgs(dDataSet, Logger));
                _dateLastTrigger = now;
            }

            if (!triggerEvent && Config.Log)
                Logger.Info(this, "Memory usage threshold not exceeded");
        }
        catch (Exception ex)
        {
            if (Config.Log)
                Logger.EventError(this, ex);
        }
    }
}
