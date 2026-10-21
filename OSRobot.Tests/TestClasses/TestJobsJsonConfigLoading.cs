// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Persistence;
using OSRobot.Server.Plugins.FtpSftpTask;
using OSRobot.Server.Plugins.SqlServerBackupTask;
using OSRobot.Tests.Support;

namespace OSRobot.Tests.TestClasses;

/// <summary>Configs must come out of jobs.json with everything the editor saved in them.</summary>
[TestClass]
public sealed class TestJobsJsonConfigLoading
{
    private static T LoadConfig<T>(object workspaceItemConfig) where T : IPluginInstanceConfig
    {
        using JsonDocument jsonDoc = JsonDocument.Parse(new JobGraph().Node(workspaceItemConfig).ToJson());
        Folder rootFolder = (Folder)new JsonDeserialization(jsonDoc).Deserialize()!;
        return (T)rootFolder.Items.OfType<ITask>().Single().Config;
    }

    [TestMethod]
    public void FtpSftpTask_copy_and_delete_items_are_loaded()
    {
        FtpSftpTaskConfig config = LoadConfig<FtpSftpTaskConfig>(new
        {
            pluginId = "FtpSftpTask",
            id = 2,
            name = "ftp",
            copyItems = new[] { new { localPath = "C:\\out" }, new { localPath = "C:\\in" } },
            deleteItems = new[] { new { remotePath = "/old" } }
        });

        Assert.HasCount(2, config.CopyItems);
        Assert.AreEqual("C:\\out", config.CopyItems[0].LocalPath);
        Assert.HasCount(1, config.DeleteItems);
        Assert.AreEqual("/old", config.DeleteItems[0].RemotePath);
    }

    [TestMethod]
    public void SqlServerBackupTask_database_lists_are_loaded()
    {
        SqlServerBackupTaskConfig config = LoadConfig<SqlServerBackupTaskConfig>(new
        {
            pluginId = "SqlServerBackupTask",
            id = 2,
            name = "backup",
            databasesToBackup = "SelectedDatabases",
            databasesList = new[] { "db1", "db2" },
            selectedDatabases = new[] { "db2" }
        });

        CollectionAssert.AreEqual(new[] { "db1", "db2" }, config.DatabasesList);
        CollectionAssert.AreEqual(new[] { "db2" }, config.SelectedDatabases);
    }
}
