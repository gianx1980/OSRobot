// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using NickStrupat;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
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

    private IEventSink? _sink;

    private System.Timers.Timer? _recurringTimer;

    private readonly ComputerInfo _computerInfo = new();

    private List<MemoryUsageSample> _memoryUsageSamples = [];

    private DateTime _dateLastTrigger;

    private DateTime _dateFirstSample = DateTime.MinValue;

    private const int _defaultIntervalMinutes = 5;

    public void Init(IEventSink sink)
    {
        _sink = sink;

        _memoryUsageSamples = [];
        _recurringTimer = new()
        {
            Enabled = false,
            AutoReset = true
        };
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;
        _recurringTimer.Enabled = true;

        MemoryEventConfig config = (MemoryEventConfig)Config;

        int checkIntervalSeconds = config.CheckIntervalSeconds;
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

            MemoryEventConfig config = (MemoryEventConfig)Config;

            if (_dateFirstSample == DateTime.MinValue)
                _dateFirstSample = DateTime.Now;

            float totalMemory = _computerInfo.TotalPhysicalMemory;
            float usedMemory = _computerInfo.TotalPhysicalMemory - _computerInfo.AvailablePhysicalMemory;
            float memoryUsage = usedMemory / totalMemory * 100; 

            Debug.WriteLine($"Samples Count before: {_memoryUsageSamples.Count}");
            if (config.TriggerIfAvgUsageIsAboveThresholdLastXMin)
                _memoryUsageSamples.Add(new MemoryUsageSample(memoryUsage));
            Debug.WriteLine($"Samples Count after: {_memoryUsageSamples.Count}");

            if (config.TriggerIfPassedXMinFromLastTrigger
                && DateTime.Now.Subtract(_dateLastTrigger).TotalMinutes < config.MinutesFromLastTrigger)
            {
                if (Config.Log)
                    Logger.Info(this, "Minimum trigger time not elapsed");
                return;
            }

            Debug.WriteLine($"Object hash: {this.GetHashCode()}, MemoryUsage: {memoryUsage}, Threshold: {config.Threshold}");

            if (config.TriggerIfUsageIsAboveThreshold)
            {
                if (memoryUsage > config.Threshold)
                    triggerEvent = true;
            }
            else if (config.TriggerIfAvgUsageIsAboveThresholdLastXMin)
            {
                DateTime dtFrom = DateTime.Now.Subtract(new TimeSpan(0, config.AvgIntervalMinutes ?? _defaultIntervalMinutes, 0));
                memoryUsage = _memoryUsageSamples.Where(t => t.SampleDateTime >= dtFrom).DefaultIfEmpty().Average(t => t == null ? 0 : t.SampleValue);

                Debug.WriteLine($"Memory Average Usage: {memoryUsage}, Threshold: {config.Threshold}, Samples Count: {_memoryUsageSamples.Count}");

                // Before starting triggering the event, we want at least some samples for duration of config.AvgIntervalMinutes minutes
                if (DateTime.Now.Subtract(_dateFirstSample).TotalMinutes > config.AvgIntervalMinutes
                    && memoryUsage > config.ThresholdLastXMin)
                    triggerEvent = true;

                _memoryUsageSamples.RemoveAll(t => t.SampleDateTime < dtFrom);
            }

            if (triggerEvent)
            {
                DateTime now = DateTime.Now;
                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
                dDataSet.TryAdd("MemoryUsagePercentage", memoryUsage);

                if (Config.Log)
                {
                    Logger.Info(this, $"Memory usage %: {memoryUsage}");
                    Logger.EventTriggering(this);
                }
                    
                _sink?.Publish(this, dDataSet, Logger);
                _dateLastTrigger = now;
            }

            if (!triggerEvent && Config.Log)
                Logger.Info(this, "Memory usage threshold not exceeded");
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever.
            Logger.EventError(this, ex);
        }
    }
}
