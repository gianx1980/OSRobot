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

using Microsoft.Win32;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using System.ComponentModel;

namespace OSRobot.Server.Plugins.SystemEventsEvent;

public class SystemEventsEvent : IEvent
{
    public IFolder? ParentFolder { get; set; }
    public int Id { get; set; }
    public IPluginInstanceConfig Config { get; set; } = new SystemEventsEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = new List<PluginInstanceConnection>();

    public event EventTriggeredDelegate? EventTriggered;

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
        /*
        new Thread(() =>
        {
            SystemEventsEventConfig TConfig = (SystemEventsEventConfig)Config;
            WndHiddenForm FormEvents = new WndHiddenForm();

            if (TConfig.EventDisplaySettingsChanged)
            {
                FormEvents.EventDisplaySettingsChanged = true;
                FormEvents.DisplaySettingsChanged += FormEvents_DisplaySettingsChanged;
            }

            if (TConfig.EventInstalledFontsChanged)
            {
                FormEvents.EventInstalledFontsChanged = true;
                FormEvents.InstalledFontsChanged += FormEvents_InstalledFontsChanged;
            }

            if (TConfig.EventPaletteChanged)
            {
                FormEvents.EventPaletteChanged = true;
                FormEvents.PaletteChanged += FormEvents_PaletteChanged;
            }

            if (TConfig.EventPowerModeChanged)
            {
                FormEvents.EventPowerModeChanged = true;
                FormEvents.PowerModeChanged += FormEvents_PowerModeChanged;
            }

            if (TConfig.EventSessionEnded)
            {
                FormEvents.EventSessionEnded = true;
                FormEvents.SessionEnded += FormEvents_SessionEnded;
            }

            if (TConfig.EventSessionSwitch)
            {
                FormEvents.EventSessionSwitch = true;
                FormEvents.SessionSwitch += FormEvents_SessionSwitch;
            }

            if (TConfig.EventTimeChanged)
            {
                FormEvents.EventTimeChanged = true;
                FormEvents.TimeChanged += FormEvents_TimeChanged;
            }

            if (TConfig.EventUserPreferenceChanged)
            {
                FormEvents.EventUserPreferenceChanged = true;
                FormEvents.UserPreferenceChanged += FormEvents_UserPreferenceChanged;
            }

            Application.Run(FormEvents);

        }).Start();
        */
    }

    public void Destroy()
    {
        
    }

    private void TriggerEvent(string eventCode)
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            if (Config.Log)
                logger.EventTriggered(this);

            DateTime now = DateTime.Now;
            DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);
            dDataSet.Add(SystemEventsEventCommon.DynDataKeyEventCode, eventCode);

            if (Config.Log)
            {
                logger.Info(this, $"Detected event: {eventCode}");
                logger.EventTriggering(this);
            }
                
            OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));
        }
        catch (Exception ex)
        {
            if (Config.Log)
                logger.EventError(this, ex);
        }
    }

    private void FormEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeUserPreferenceChanged);
    }

    private void FormEvents_TimeChanged(object sender, EventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeTimeChanged);
    }

    private void FormEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeSessionSwitch);
    }

    private void FormEvents_SessionEnded(object sender, SessionEndedEventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeSessionEnded);
    }

    private void FormEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodePowerModeChanged);
    }

    private void FormEvents_PaletteChanged(object sender, EventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodePaletteChanged);
    }

    private void FormEvents_InstalledFontsChanged(object sender, EventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeInstalledFontsChanged);
    }

    private void FormEvents_DisplaySettingsChanged(object sender, EventArgs e)
    {
        TriggerEvent(SystemEventsEventCommon.EventCodeDisplaySettingsChanged);
    }
}
