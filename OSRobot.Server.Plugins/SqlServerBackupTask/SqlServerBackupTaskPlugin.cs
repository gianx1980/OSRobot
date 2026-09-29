// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
namespace OSRobot.Server.Plugins.SqlServerBackupTask;

public class SqlServerBackupTaskPlugin : IPlugin
{
    public string Id => "SqlServerBackupTask";

    public string Title => Resource.TxtSqlServerBackupTask; 

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> Samples = CommonDynamicData.BuildStandardDynamicDataSamples("Sql Server backup task 1");
            Samples.Add(new DynamicDataSample(SqlServerBackupTaskCommon.DynDataKeySuccessfulBackupsNumber, Resource.TxtSuccessfulBackupsNumber, "5"));
            Samples.Add(new DynamicDataSample(SqlServerBackupTaskCommon.DynDataKeyFailedBackupsNumber, Resource.TxtFailedBackupsNUmber, "0"));
            return Samples;
        }
    }

    public IPluginInstance GetInstance() => new SqlServerBackupTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new SqlServerBackupTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
