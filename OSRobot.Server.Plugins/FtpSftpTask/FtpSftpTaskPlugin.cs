// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpTaskPlugin : IPlugin
{
    public string Id => "FtpSftpTask";

    public string Title => Resource.TxtFtpSftpTask;

    public EnumPluginType PluginType => EnumPluginType.Task;

    public List<DynamicDataSample> SampleDynamicData => CommonDynamicData.BuildStandardDynamicDataSamples("Ftp / Sftp task 1");

    public IPluginInstance GetInstance() => new FtpSftpTask();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new FtpSftpTaskConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
