// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Persistence;
using System.Xml;

namespace OSRobot.Server.Core;

public static class CoreHelpers
{
    private readonly static object _objectCloning = new();

    public static string ToIsoDate(this DateTime date)
    {
        return date.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static object? CloneObjects(object pluginInstance)
    {
        XmlSerialization serializer = new()
        {
            CheckSerializeAttribute = true
        };
        
        string output = serializer.SerializeToXmlString(pluginInstance, "OSRobot");

        // TODO: consider removing lock
        lock (_objectCloning)
        {
            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(output);
            XmlDeserialization deserializer = new(xmlDoc)
            {
                CheckSerializeAttribute = true
            };
            return deserializer.Deserialize();            
        }
    }
}
