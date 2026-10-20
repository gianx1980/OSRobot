// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Persistence;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Obsolete("Never encrypted anything: only read by the legacy XmlSerialization, whose encryption is commented out.")]
public class XmlEncryptFieldAttribute : Attribute
{

}
