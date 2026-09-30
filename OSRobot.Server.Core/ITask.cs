// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;

namespace OSRobot.Server.Core;

public interface ITask : IPluginInstance
{
    Task<InstanceExecResult> RunAsync(DynamicDataChain dataChain, DynamicDataSet lastDynamicDataSet, int? subInstanceIndex,
                                       IPluginInstanceLogger instanceLogger, CancellationToken cancellationToken);
}
