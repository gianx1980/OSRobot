// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

/// <summary>How many times a connection runs its target when the source task produces several results (one per iteration).</summary>
public enum EnumConnectionRunMode
{
    /// <summary>Once per result that satisfies the conditions: a task with 10 iterations can run its target 10 times.</summary>
    ForEachResult,

    /// <summary>
    /// Once, after all the iterations, with every result collected in the source's "Results" recordset.
    /// The conditions are evaluated once, against the collected result.
    /// </summary>
    OnceWithAllResults
}

/// <summary>When a collected result (<see cref="EnumConnectionRunMode.OnceWithAllResults"/>) counts as successful.</summary>
public enum EnumCollectedResultRule
{
    AllSucceeded,
    AnySucceeded
}
