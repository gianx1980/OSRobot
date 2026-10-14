// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using System.ComponentModel; 

namespace OSRobot.Server.Plugins.DateTimeEvent;  

public class DateTimeEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new DateTimeEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    private IEventSink? _sink;

    private readonly System.Timers.Timer _oneTimeTimer = new();

    private readonly System.Timers.Timer _recurringTimer = new();

    private double _recurringIntervalMs;

    public void Init(IEventSink sink)
    {
        _sink = sink;

        _oneTimeTimer.Enabled = false;
        _oneTimeTimer.AutoReset = false;
        _oneTimeTimer.Elapsed += OnTimeTimer_Elapsed;

        _recurringTimer.Enabled = false;
        _recurringTimer.AutoReset = true;
        _recurringTimer.Elapsed += RecurringTimer_Elapsed;

        DateTimeEventConfig config = (DateTimeEventConfig)Config;

        TimeSpan recurringInterval = TimeSpan.Zero;
        if (config.EveryDaysHoursSecs)
            recurringInterval = new TimeSpan(config.EveryNumDays, config.EveryNumHours, config.EveryNumMinutes, 0);
        else if (config.EverySeconds)
            recurringInterval = new TimeSpan(0, 0, 0, config.EveryNumSeconds);

        if (recurringInterval > TimeSpan.Zero)
        {
            _recurringIntervalMs = recurringInterval.TotalMilliseconds;
            _recurringTimer.Interval = _recurringIntervalMs;
        }

        DateTime now = DateTime.Now;
        if (config.AtDate > now)
        {
            _oneTimeTimer.Interval = config.AtDate.Subtract(now).TotalMilliseconds;
            _oneTimeTimer.Enabled = true;
        }
        else if (recurringInterval > TimeSpan.Zero)
        {
            // AtDate has already passed (e.g. the service was restarted): the first occurrence must be
            // the next one of the AtDate-based schedule, not a full interval from now. Otherwise every
            // restart shifts the schedule, and restarting more often than the interval prevents the event
            // from ever firing. RecurringTimer_Elapsed restores the regular interval after the first occurrence.
            long elapsedPeriods = (now - config.AtDate).Ticks / recurringInterval.Ticks;
            DateTime next = config.AtDate.AddTicks(recurringInterval.Ticks * (elapsedPeriods + 1));
            _recurringTimer.Interval = next.Subtract(now).TotalMilliseconds;
            _recurringTimer.Enabled = true;
        }
    }

    public void Destroy()
    {
        _oneTimeTimer.Dispose();
        _recurringTimer.Dispose();
    }

    private void OnTimeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            // Start the recurring timer first, so the time spent publishing does not delay the schedule.
            DateTimeEventConfig config = (DateTimeEventConfig)Config;
            if (!config.OneTime && _recurringIntervalMs > 0)
                _recurringTimer.Enabled = true;

            if (Config.Log)
                logger.EventTriggered(this);

            DateTime now = DateTime.Now;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

            if (Config.Log)
                logger.EventTriggering(this);
            _sink?.Publish(this, dDataSet, logger);
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever.
            logger.EventError(this, ex);
        }
    }

    private void RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // After an aligned first occurrence (see Init) switch back to the regular interval.
        if (_recurringTimer.Interval != _recurringIntervalMs)
            _recurringTimer.Interval = _recurringIntervalMs;

        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (Config.Log)
                logger.EventTriggered(this);
            DateTime now = DateTime.Now;
            DateTimeEventConfig config = (DateTimeEventConfig)Config;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

            if ((config.OnDays.Where(i => i.Equals(now.DayOfWeek)).Any()) || config.OnAllDays)
            {
                if (Config.Log)
                    logger.EventTriggering(this);
                _sink?.Publish(this, dDataSet, logger);
            }
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to
            // report failure through, so without this the event would fail silently forever
            // (here that includes the recurring timer never being re-armed).
            logger.EventError(this, ex);
        }
    }
}
