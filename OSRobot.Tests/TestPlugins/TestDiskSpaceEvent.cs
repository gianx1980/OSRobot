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
using OSRobot.Server.Plugins.DiskSpaceEvent;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestDiskSpaceEvent
{
    [TestMethod]
    public void TestThresholdToBytesMegaBytes()
    {
        // 549755813888: 512GB space
        // 99999999 is the max value the user can insert in the configuration window
        DiskSpaceEvent eventObj = new();
        long? result = (long?)Common.CallPrivateMethod(eventObj, "ThresholdToBytes", [(long)549755813888, (int)99999999, DiskThresholdUnitMeasure.Megabytes]);
        Assert.IsNotNull(result, "Result is null");
        Assert.AreEqual(result, 99999999L * 1024L * 1024L);
    }

    [TestMethod]
    public void TestThresholdToBytesGigaBytes()
    {
        // 99999999 is the max value the user can insert in the configuration window
        DiskSpaceEvent eventObj = new();
        long? result = (long?)Common.CallPrivateMethod(eventObj, "ThresholdToBytes", [(long)549755813888, (int)99999999, DiskThresholdUnitMeasure.Gigabytes]);

        Assert.IsNotNull(result, "Result is null");
        Assert.AreEqual(result, 99999999L * 1024L * 1024L * 1024L);
    }

    [TestMethod]
    public void TestThresholdToBytesTeraBytes()
    {
        // 999999 is the max value the user can insert in the configuration window
        DiskSpaceEvent eventObj = new();
        long? result = (long?)Common.CallPrivateMethod(eventObj, "ThresholdToBytes", [(long)549755813888, (int)999999, DiskThresholdUnitMeasure.Terabytes]);

        Assert.IsNotNull(result, "Result is null");
        Assert.AreEqual(result, 999999L * 1024L * 1024L * 1024L * 1024L);
    }


    [TestMethod]
    public void TestSpaceLessThan()
    {
        // ---------
        // Arrange
        // ---------
        int checkIntervalEverySeconds = 3;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DiskSpaceEventConfig config = new()
        {
            Id = 1,
            Name = "Disk space event 1",
            CheckIntervalSeconds = checkIntervalEverySeconds
        };

        DriveInfo? driveC = DriveInfo.GetDrives().Where(D => D.Name == @"C:\").FirstOrDefault();
        Assert.IsNotNull(driveC, "Drive C: doesn't exist");

        int diskFreeSpaceToCheckGB = (int)(((double)driveC.AvailableFreeSpace) / 1024 / 1024 / 1024);
        diskFreeSpaceToCheckGB *= 2;

        DiskThreshold dt = new()
        {
            Disk = @"C:\",
            CheckOperator = CheckOperator.LessThan,
            ThresholdValue = diskFreeSpaceToCheckGB,
            UnitMeasure = DiskThresholdUnitMeasure.Gigabytes
        };

        config.DiskThresholds.Add(dt);

        DiskSpaceEvent eventObj = new()
        {
            ParentFolder = folder
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

        mre.WaitOne(new TimeSpan(0, 0, checkIntervalEverySeconds + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered);
        }

        eventObj.Destroy();
    }

    [TestMethod]
    public void TestSpaceGreaterThan()
    {
        // ---------
        // Arrange
        // ---------
        int checkIntervalEverySeconds = 3;
        int toleranceSec = 1;
        Folder folder = Common.CreateRootFolder();

        DiskSpaceEventConfig config = new()
        {
            Id = 1,
            Name = "Disk space event 1",
            CheckIntervalSeconds = checkIntervalEverySeconds
        };

        DriveInfo? driveC = DriveInfo.GetDrives().Where(D => D.Name == @"C:\").FirstOrDefault();
        Assert.IsNotNull(driveC, "Drive C: doesn't exist");

        int diskFreeSpaceToCheckGB = (int)(((double)driveC.AvailableFreeSpace) / 1024 / 1024 / 1024);
        diskFreeSpaceToCheckGB /= (int)2d;

        DiskThreshold dt = new()
        {
            Disk = @"C:\",
            CheckOperator = CheckOperator.GreaterThan,
            ThresholdValue = diskFreeSpaceToCheckGB,
            UnitMeasure = DiskThresholdUnitMeasure.Gigabytes
        };

        config.DiskThresholds.Add(dt);

        DiskSpaceEvent eventObj = new()
        {
            ParentFolder = folder
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

        mre.WaitOne(new TimeSpan(0, 0, checkIntervalEverySeconds + toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered);
        }

        eventObj.Destroy();
    }
}
