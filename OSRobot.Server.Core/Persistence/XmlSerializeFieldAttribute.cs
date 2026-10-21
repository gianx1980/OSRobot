// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Persistence;

[AttributeUsage(AttributeTargets.Field)]
[Obsolete("Only read by the legacy XmlSerialization.")]
public class XmlSerializeFieldAttribute : Attribute
{

}
