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

// Suppress supported platform warning
#pragma warning disable CA1416

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using System.ComponentModel;
using System.Diagnostics;

namespace OSRobot.Server.Plugins.CpuEvent;

public class CpuUsageSample(float sampleValue)
{
    public DateTime SampleDateTime { get; set; } = DateTime.Now;

    public float SampleValue { get; set; } = sampleValue;
}

public class CpuEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new CpuEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    public event EventTriggeredDelegate? EventTriggered;

    private readonly System.Timers.Timer _recurringTimer = new();

    private PerformanceCounter? _perfCounter;

    private CounterSample _lastSample;

    private readonly List<CpuUsageSample> _cpuUsageSamples = [];

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
        _recurringTimer.Enabled = false;
        _recurringTimer.AutoReset = true;
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;

        CpuEventConfig tConfig = (CpuEventConfig)Config;

        int checkIntervalSeconds = tConfig.CheckIntervalSeconds;
        if (checkIntervalSeconds <= 0)
            checkIntervalSeconds = 1;

        _recurringTimer.Interval = new TimeSpan(0, 0, checkIntervalSeconds).TotalMilliseconds;

        _recurringTimer.Enabled = true;
    }

    public void Destroy()
    {
        _recurringTimer.Dispose();
    }

    private void RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            bool triggerEvent = false;

            if (Config.Log)
                logger.Info(this, "Checking CPU usage...");

            CpuEventConfig tConfig = (CpuEventConfig)Config;

            _perfCounter ??= new PerformanceCounter("Processor information", "% processor utility", "_Total");

            if (_dateFirstSample == DateTime.MinValue)
                _dateFirstSample = DateTime.Now;

            CounterSample currentSample = _perfCounter.NextSample();
            float cpuUsage = CounterSample.Calculate(_lastSample, currentSample);
            _lastSample = currentSample;

            Debug.WriteLine($"Samples Count before: {_cpuUsageSamples.Count}");
            if (tConfig.TriggerIfAvgUsageIsAboveThresholdLastXMin)
                _cpuUsageSamples.Add(new CpuUsageSample(cpuUsage));
            Debug.WriteLine($"Samples Count after: {_cpuUsageSamples.Count}");

            if (tConfig.TriggerIfPassedXMinFromLastTrigger
                && DateTime.Now.Subtract(_dateLastTrigger).TotalMinutes < tConfig.MinutesFromLastTrigger)
            {
                if (Config.Log)
                    logger.Info(this, "Minimum trigger time not elapsed");
                return;
            }

            Debug.WriteLine($"Object hash: {this.GetHashCode()}, CpuUsage: {cpuUsage}, Threshold: {tConfig.Threshold}");

            if (tConfig.TriggerIfUsageIsAboveThreshold)
            {    
                if (cpuUsage > tConfig.Threshold)
                    triggerEvent = true;
            }
            else if (tConfig.TriggerIfAvgUsageIsAboveThresholdLastXMin)
            {
                DateTime dtFrom = DateTime.Now.Subtract(new TimeSpan(0, tConfig.AvgIntervalMinutes ?? _defaultIntervalMinutes, 0));
                cpuUsage = _cpuUsageSamples.Where(t => t.SampleDateTime >= dtFrom).DefaultIfEmpty().Average(t => t == null ? 0 : t.SampleValue);

                Debug.WriteLine($"Cpu Average Usage: {cpuUsage}, Threshold: {tConfig.Threshold}, Samples Count: {_cpuUsageSamples.Count}");

                // Before starting triggering the event, we want at least samples for duaration of TConfig.AvgIntervalMinutes minutes
                if (DateTime.Now.Subtract(_dateFirstSample).TotalMinutes > tConfig.AvgIntervalMinutes
                    && cpuUsage > tConfig.ThresholdLastXMin)
                    triggerEvent = true;

                _cpuUsageSamples.RemoveAll(t => t.SampleDateTime < dtFrom);
            }
            
            if (triggerEvent)
            {
                DateTime now = DateTime.Now;
                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
                dDataSet.Add("CpuUsagePercentage", cpuUsage);

                if (Config.Log)
                {
                    logger.Info(this, "$Cpu usage detected: {CpuUsage}");
                    logger.EventTriggering(this);
                }
                    
                OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));
                _dateLastTrigger = now;
            }

            if (!triggerEvent && Config.Log)
                logger.Info(this, "Cpu usage threshold not exceeded");
        }
        catch (Exception ex)
        {
            if (Config.Log)
                logger.EventError(this, ex);
        }
    }
}
