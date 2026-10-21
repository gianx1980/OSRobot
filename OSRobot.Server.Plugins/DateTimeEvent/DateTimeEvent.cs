// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Plugins.DateTimeEvent;

/// <summary>
/// Fires on the schedule computed by <see cref="DateTimeEventScheduler"/>.
///
/// Rather than a repeating timer (which drifts, and gets out of step with the clock after computer sleep,
/// a clock change or a daylight saving change), a one-shot timer is armed for the next occurrence and
/// re-armed after each one. No single wait is longer than <see cref="MaxWait"/>: each wake-up re-reads the
/// clock, so the schedule corrects itself within MaxWait whatever happened to the clock in the meantime,
/// and the final wait before an occurrence is exact.
/// </summary>
public class DateTimeEvent : IEvent
{
    /// <summary>The longest the timer waits before it re-checks the clock.</summary>
    internal static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// An occurrence noticed later than this is skipped instead of fired: it was missed because the service
    /// was not running, the computer was asleep or the clock moved forward.
    /// </summary>
    internal static readonly TimeSpan MissedOccurrenceTolerance = TimeSpan.FromMinutes(1);

    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new DateTimeEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    private readonly object _gate = new();
    private IEventSink? _sink;
    private Timer? _timer;                 // guarded by _gate; null once destroyed
    private DateTime? _nextOccurrenceUtc;  // guarded by _gate; null when nothing is left to fire

    public void Init(IEventSink sink)
    {
        _sink = sink;
        DateTimeEventConfig config = (DateTimeEventConfig)Config;

        // An invalid configuration must not throw: the engine initializes all the events in one go.
        string? configError = DateTimeEventScheduler.Validate(config);
        if (configError != null)
        {
            PluginInstanceLogger.GetLogger(this).EventError(this, new ApplicationException(configError));
            return;
        }

        lock (_gate)
        {
            // Occurrences that are already due are missed: they are not fired at startup.
            _nextOccurrenceUtc = DateTimeEventScheduler.GetNextOccurrence(config, DateTime.UtcNow, TimeZoneInfo.Local);
            _timer = new Timer(Timer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
            ArmTimer();
        }
    }

    public void Destroy()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _nextOccurrenceUtc = null;
        }
    }

    // Called under _gate.
    private void ArmTimer()
    {
        if (_timer == null || _nextOccurrenceUtc == null)
            return;

        TimeSpan wait = _nextOccurrenceUtc.Value - DateTime.UtcNow;
        if (wait > MaxWait)
            wait = MaxWait;
        else if (wait < TimeSpan.Zero)
            wait = TimeSpan.Zero;

        _timer.Change(wait, Timeout.InfiniteTimeSpan);
    }

    private void Timer_Elapsed(object? state)
    {
        DateTime? dueOccurrenceUtc = null;
        bool fire = false;

        try
        {
            lock (_gate)
            {
                if (_timer == null || _nextOccurrenceUtc == null)
                    return;

                DateTime nowUtc = DateTime.UtcNow;
                if (nowUtc < _nextOccurrenceUtc.Value)
                {
                    // An intermediate wake-up (see MaxWait), or the clock moved back: wait some more.
                    ArmTimer();
                    return;
                }

                dueOccurrenceUtc = _nextOccurrenceUtc.Value;
                fire = nowUtc - dueOccurrenceUtc.Value <= MissedOccurrenceTolerance;

                // Schedule the next occurrence before publishing, so the time spent publishing doesn't delay it.
                // Computed from now, not from the due occurrence: occurrences missed in between are skipped.
                _nextOccurrenceUtc = DateTimeEventScheduler.GetNextOccurrence((DateTimeEventConfig)Config, nowUtc, TimeZoneInfo.Local);
                ArmTimer();
            }
        }
        catch (Exception ex)
        {
            // Errors are always logged, regardless of Config.Log: an event has no ExecResult to report
            // failure through, so without this the event would stop firing silently.
            PluginInstanceLogger.GetLogger(this).EventError(this, ex);
            return;
        }

        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (!fire)
            {
                if (Config.Log)
                    logger.Info(this, $"Occurrence of {dueOccurrenceUtc!.Value.ToLocalTime().ToIsoDate()} skipped: it was missed (service stopped, computer asleep or clock changed).");
                return;
            }

            if (Config.Log)
                logger.EventTriggering(this);

            DateTime now = DateTime.Now;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
            _sink?.Publish(this, dDataSet, logger);
        }
        catch (Exception ex)
        {
            logger.EventError(this, ex);
        }
    }
}
