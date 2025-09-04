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
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.DateTimeEvent;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestDateTimeEvent
{
    [TestMethod]
    public void TestAtTime()
    {
        // ---------
        // Arrange
        // ---------
        int withinMinutes = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            OneTime = true,
            AtDate = DateTime.Now.AddMinutes(withinMinutes)
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        eventObj.EventTriggered += (sender, e) =>
        {
            lock (objSync)
            {
                eventTriggered = true;
            }

            mre.Set();
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, withinMinutes, toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered && (Math.Abs(DateTime.Now.Subtract(config.AtDate).TotalSeconds) <= toleranceSec), "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestEverySecond()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 5;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        int repeatCount = 0;
        DateTime expectedLastTrigger = DateTime.Now.AddSeconds(everyNumSeconds * repeatNumber);
        eventObj.EventTriggered += (sender, e) =>
        {
            repeatCount++;

            if (repeatCount == repeatNumber)
            {
                lock (objSync)
                {
                    eventTriggered = true;
                }

                mre.Set();
                return;
            }
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered && (Math.Abs(DateTime.Now.Subtract(expectedLastTrigger).TotalSeconds) <= toleranceSec), "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestEveryMinute()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumMinutes = 1;
        int repeatNumber = 5;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EveryDaysHoursSecs = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumDays = 0,
            EveryNumHours = 0,
            EveryNumMinutes = everyNumMinutes
        };

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggeredInTime = false;
        int repeatCount = 0;
        DateTime expectedLastTrigger = DateTime.Now.AddMinutes(everyNumMinutes * repeatNumber);
        eventObj.EventTriggered += (sender, e) =>
        {
            repeatCount++;

            if (repeatCount == repeatNumber)
            {
                lock (objSync)
                {
                    eventTriggeredInTime = true;
                }

                mre.Set();
                return;
            }
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, 0, (everyNumMinutes * 60 * repeatNumber) + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggeredInTime && (Math.Abs(DateTime.Now.Subtract(expectedLastTrigger).TotalSeconds) <= toleranceSec), "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestEverySecondOnDaysTrue()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnDays.Add(DateTime.Now.DayOfWeek);

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        int repeatCount = 0;
        DateTime expectedLastTrigger = DateTime.Now.AddSeconds(everyNumSeconds * repeatNumber);
        eventObj.EventTriggered += (sender, e) =>
        {
            repeatCount++;

            if (repeatCount == repeatNumber)
            {
                lock (objSync)
                {
                    eventTriggered = true;
                }

                mre.Set();
                return;
            }
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered && (Math.Abs(DateTime.Now.Subtract(expectedLastTrigger).TotalSeconds) <= toleranceSec), "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestEverySecondOnAllDays()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnAllDays = true;

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        int repeatCount = 0;
        DateTime expectedLastTrigger = DateTime.Now.AddSeconds(everyNumSeconds * repeatNumber);
        eventObj.EventTriggered += (sender, e) =>
        {
            repeatCount++;

            if (repeatCount == repeatNumber)
            {
                lock (objSync)
                {
                    eventTriggered = true;
                }

                mre.Set();
                return;
            }
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered && (Math.Abs(DateTime.Now.Subtract(expectedLastTrigger).TotalSeconds) <= toleranceSec), "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestEverySecondOnDaysFalse()
    {
        // ---------
        // Arrange
        // ---------
        int everyNumSeconds = 5;
        int repeatNumber = 1;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DateTimeEventConfig config = new()
        {
            Id = 1,
            Name = "DateTimeEvent 1",
            EverySeconds = true,
            AtDate = DateTime.Now.AddSeconds(-10), // Start with AtTime in the past
            EveryNumSeconds = everyNumSeconds
        };
        config.OnDays.Clear();
        config.OnAllDays = false;
        foreach (DayOfWeek D in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (D != DateTime.Now.DayOfWeek)
            {
                config.OnDays.Add(D);
                break;
            }
        }

        DateTimeEvent eventObj = new()
        {
            ParentFolder = folder
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        int repeatCount = 0;
        DateTime expectedLastTrigger = DateTime.Now.AddSeconds(everyNumSeconds * repeatNumber);
        eventObj.EventTriggered += (sender, e) =>
        {
            repeatCount++;

            if (repeatCount == repeatNumber)
            {
                lock (objSync)
                {
                    eventTriggered = true;
                }

                mre.Set();
                return;
            }
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        mre.WaitOne(new TimeSpan(0, 0, (everyNumSeconds * repeatNumber) + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsFalse(eventTriggered, "The event did not occur at the expected time.");
        }
    }
}
