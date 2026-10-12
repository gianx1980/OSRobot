// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

public class SqlServerFileLoaderTaskPlugin : IPlugin
{
    public string Id => "SqlServerFileLoaderTask";

    public string Title => Resource.TxtSqlServerFileLoaderTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> samples = CommonDynamicData.BuildStandardDynamicDataSamples("Sql Server auto loader 1");
            samples.Add(new DynamicDataSample(SqlServerFileLoaderTaskCommon.DynDataKeyRowsLoaded, Resource.TxtDynDataRowsLoaded, "1500"));
            samples.Add(new DynamicDataSample(SqlServerFileLoaderTaskCommon.DynDataKeyTableName, Resource.TxtDynDataLoadedTableName, "dbo.Customers"));
            samples.Add(new DynamicDataSample(SqlServerFileLoaderTaskCommon.DynDataKeyTableCreated, Resource.TxtDynDataTableCreated, "True"));
            return samples;
        }
    }

    public IPluginInstance GetInstance() => new SqlServerFileLoaderTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new SqlServerFileLoaderTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
