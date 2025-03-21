/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/
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
