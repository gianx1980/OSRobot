// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

public class InstanceExecResult(List<ExecResult> execResults)
{
    public List<ExecResult> ExecResults { get; private set; } = execResults;
}
