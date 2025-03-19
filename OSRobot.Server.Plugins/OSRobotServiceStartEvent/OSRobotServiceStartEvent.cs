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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OSRobot.Server.Plugins.OSRobotServiceStartEvent;

public partial class OSRobotServiceStartEvent : IEvent
{
    [LibraryImport("kernel32")]
    private static partial ulong GetTickCount64();

    public IFolder? ParentFolder { get; set; }
    public int Id { get; set; }

    public IPluginInstanceConfig Config { get; set; } = new OSRobotServiceStartEventConfig();

    public List<PluginInstanceConnection> Connections { get; set; } = [];

    [field: NonSerialized]
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
                    syncInvoke.Invoke(singleCast, [this, e]);
                else
                    singleCast(this, e);
            }
        }
    }

    private int GetSystemStartedForMinutes()
    {
        /*
        According to: https://docs.microsoft.com/en-us/dotnet/api/system.environment.tickcount?view=netframework-4.8 
        Because the value of the TickCount property value is a 32-bit signed integer, if the system runs continuously, TickCount will increment from zero 
        to Int32.MaxValue for approximately 24.9 days, then jump to Int32.MinValue, which is a negative number, then increment back 
        to zero during the next 24.9 days. You can work around this issue by calling the Windows GetTickCount function, which resets to zero after approximately 49.7 days, 
        or by calling the GetTickCount64 function.

        So we use GetTickCount64, not very cross platform but maybe we will improve later...
        */


        ulong minutesUptime = GetTickCount64() / 1000 / 64;

        // Int is sufficient for our purpose
        if (minutesUptime > int.MaxValue)
            return int.MaxValue;

        return (int)minutesUptime;
    }

    public void Init()
    {
        IPluginInstanceLogger logger = PluginInstanceLogger.GetLogger(this);

        try
        {
            OSRobotServiceStartEventConfig tConfig = (OSRobotServiceStartEventConfig)Config;

            int systemStartedForMinutes = GetSystemStartedForMinutes();

            if ((tConfig.MinutesWithin != null && systemStartedForMinutes <= tConfig.MinutesWithin)
                || (tConfig.MinutesAfter != null && systemStartedForMinutes >= tConfig.MinutesAfter))
            {
                DateTime now = DateTime.Now;
                DynamicDataSet dDataSet = CommonDynamicData.BuildStandardDynamicDataSet(this, true, 0, now, now, 1);

                if (tConfig.Log)
                {
                    logger.Info(this, $"System up time (minutes): {systemStartedForMinutes}");
                    logger.EventTriggering(this);
                }

                OnEventTriggered(new EventTriggeredEventArgs(dDataSet, logger));
            }
        }
        catch (Exception ex)
        {
            if (Config.Log)
                logger.EventError(this, ex);
        }
    }

    public void Destroy()
    {
        
    }
}
