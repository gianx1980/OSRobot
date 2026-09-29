// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.SqlServerBulkCopyTask;

public class SqlServerBulkCopyTaskPlugin : IPlugin
{
    public string Id => "SqlServerBulkCopyTask";

    public string Title => Resource.TxtSqlServerBulkCopyTask;

    public EnumPluginType PluginType => EnumPluginType.Task; 

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Sql Server bulk copy task 1");

    public IPluginInstance GetInstance() => new SqlServerBulkCopyTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new SqlServerBulkCopyTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
