// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Plugins.DiskSpaceEvent;
using OSRobot.Tests.Support;

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
        Assert.AreEqual(99999999L * 1024L * 1024L, result);
    }

    [TestMethod]
    public void TestThresholdToBytesGigaBytes()
    {
        // 99999999 is the max value the user can insert in the configuration window
        DiskSpaceEvent eventObj = new();
        long? result = (long?)Common.CallPrivateMethod(eventObj, "ThresholdToBytes", [(long)549755813888, (int)99999999, DiskThresholdUnitMeasure.Gigabytes]);

        Assert.IsNotNull(result, "Result is null");
        Assert.AreEqual(99999999L * 1024L * 1024L * 1024L, result);
    }

    [TestMethod]
    public void TestThresholdToBytesTeraBytes()
    {
        // 999999 is the max value the user can insert in the configuration window
        DiskSpaceEvent eventObj = new();
        long? result = (long?)Common.CallPrivateMethod(eventObj, "ThresholdToBytes", [(long)549755813888, (int)999999, DiskThresholdUnitMeasure.Terabytes]);

        Assert.IsNotNull(result, "Result is null");
        Assert.AreEqual(999999L * 1024L * 1024L * 1024L * 1024L, result);
    }


    [TestMethod]
    public async Task TestSpaceLessThan()
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
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(1, new TimeSpan(0, 0, checkIntervalEverySeconds + toleranceSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(triggeredAt, "The event did not occur.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }

    [TestMethod]
    public async Task TestSpaceGreaterThan()
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
            ParentFolder = folder,
            Config = config
        };

        RecordingEventSink sink = new();

        try
        {
            // ---------
            // Act
            // ---------
            eventObj.Init(sink);

            DateTime? triggeredAt = await sink.WaitForOccurrenceAsync(1, new TimeSpan(0, 0, checkIntervalEverySeconds + toleranceSec));

            // ---------
            // Assert
            // ---------
            Assert.IsNotNull(triggeredAt, "The event did not occur.");
        }
        finally
        {
            eventObj.Destroy();
        }
    }
}
