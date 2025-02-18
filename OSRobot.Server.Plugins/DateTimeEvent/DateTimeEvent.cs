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
using System.ComponentModel; 

namespace OSRobot.Server.Plugins.DateTimeEvent;  

public class DateTimeEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new DateTimeEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = new List<PluginInstanceConnection>();

    public event EventTriggeredDelegate? EventTriggered;

    private System.Timers.Timer _oneTimeTimer = new System.Timers.Timer();

    private System.Timers.Timer _recurringTimer = new System.Timers.Timer();

    protected virtual void OnEventTriggered(EventTriggeredEventArgs e)
    {
        EventTriggeredDelegate? handler = EventTriggered;
        if (handler != null)
        {
            foreach (EventTriggeredDelegate singleCast in handler.GetInvocationList())
            {
                ISynchronizeInvoke? syncInvoke = singleCast.Target as ISynchronizeInvoke;
                if ((syncInvoke != null) && (syncInvoke.InvokeRequired))
                    syncInvoke.Invoke(singleCast, new object[] { this, e });
                else
                    singleCast(this, e);
            }
        }
    }


    public void Init()
    {
        _oneTimeTimer.Enabled = false;
        _oneTimeTimer.AutoReset = false;
        _oneTimeTimer.Elapsed += _OnTimeTimer_Elapsed;

        _recurringTimer.Enabled = false;
        _recurringTimer.AutoReset = true;
        _recurringTimer.Elapsed += _RecurringTimer_Elapsed;

        bool enableOneTimeTimer = false;
        bool enableRecurringTimer = false;
        DateTimeEventConfig tConfig = (DateTimeEventConfig)Config;

        DateTime now = DateTime.Now;
        if (tConfig.AtDate > now)
        {
            _oneTimeTimer.Interval = tConfig.AtDate.Subtract(now).TotalMilliseconds;
            enableOneTimeTimer = true;
        }

        if (tConfig.EveryDaysHoursSecs)
        {
            _recurringTimer.Interval = new TimeSpan(tConfig.EveryNumDays, tConfig.EveryNumHours, tConfig.EveryNumMinutes, 0).TotalMilliseconds;
            enableRecurringTimer = true;
        }
        else if (tConfig.EverySeconds)
        {
            _recurringTimer.Interval = new TimeSpan(0, 0, 0, tConfig.EveryNumSeconds).TotalMilliseconds;
            enableRecurringTimer = true;
        }

        if (enableOneTimeTimer)
            _oneTimeTimer.Enabled = true;
        else if (enableRecurringTimer)
            _recurringTimer.Enabled = true;
    }

    public void Destroy()
    {
        _oneTimeTimer.Dispose();
        _recurringTimer.Dispose();
    }

    private void _OnTimeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (Config.Log)
                logger.EventTriggered(this);
            
            DateTime now = DateTime.Now;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

            if (Config.Log)
                logger.EventTriggering(this);
            OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));

            DateTimeEventConfig tConfig = (DateTimeEventConfig)Config;
            if (!tConfig.OneTime)
                _recurringTimer.Enabled = true;
        }
        catch (Exception ex)
        {
            if (Config.Log)
                logger.EventError(this, ex);
        }
    }

    private void _RecurringTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (Config.Log)
                logger.EventTriggered(this);
            DateTime now = DateTime.Now;
            DateTimeEventConfig tConfig = (DateTimeEventConfig)Config;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

            if ((tConfig.OnDays.Where(i => i.Equals(now.DayOfWeek)).Count() > 0) || tConfig.OnAllDays)
            {
                if (Config.Log)
                    logger.EventTriggering(this);
                OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));
            }
        }
        catch (Exception ex)
        {
            if (Config.Log)
                logger.EventError(this, ex);
        }
    }
}
