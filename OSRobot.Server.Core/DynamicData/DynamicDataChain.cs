// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;

namespace OSRobot.Server.Core.DynamicData;

public class DynamicDataChain : ConcurrentDictionary<int, DynamicDataSet>
{
    public DynamicDataChain Clone()
    {
        DynamicDataChain dictionaryCloned = new();

        foreach (var kvp in this)
        {
            dictionaryCloned.TryAdd(kvp.Key, kvp.Value);
        }

        return dictionaryCloned;
    }
}
