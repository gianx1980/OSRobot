// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.DynamicData;

public class DynamicDataObjectSamples(int id, string description, EnumPluginType type, List<DynamicDataSample> dynamicDataSampleList)
{
    public int Id { get; private set; } = id;

    public string Description { get; private set; } = description;

    public EnumPluginType Type { get; private set; } = type;

    public List<DynamicDataSample> DynamicDataSampleList { get; private set; } = dynamicDataSampleList;
}
