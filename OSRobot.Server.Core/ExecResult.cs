// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Core;

public class ExecResult(bool result, DynamicDataSet data)
{
    public bool Result { get; private set; } = result;
    public DynamicDataSet Data { get; private set; } = data;
}
