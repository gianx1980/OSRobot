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
using System.Text.Json;
using System.Xml;

namespace OSRobot.Server.Core.Persistence;

public static class JobsPersistence
{
    public static void SaveXML(string dataPath, string fileName, Folder rootFolderData)
    {
        string filePathName = Path.Combine(dataPath, fileName);
        XmlSerialization serializer = new();
        XmlDocument xmlDoc = serializer.Serialize(rootFolderData, "OSRobot");

        xmlDoc.Save(filePathName);
    }

    public static Folder? LoadXML(string dataPath, string fileName)
    {
        string filePathName = Path.Combine(dataPath, fileName);
        if (!File.Exists(filePathName))
            return null;

        XmlDocument xmlDoc = new();
        xmlDoc.Load(filePathName);

        XmlDeserialization deserializer = new(xmlDoc);
        return (Folder?)deserializer.Deserialize();
    }

    public static Folder? LoadJobEditorJSON(string dataPath, string fileName)
    {
        string filePathName = Path.Combine(dataPath, fileName);
        if (!File.Exists(filePathName))
            return null;

        string jsonFile = File.ReadAllText(filePathName);
        using JsonDocument jsonDoc = JsonDocument.Parse(jsonFile);
        JsonDeserialization deserializer = new(jsonDoc);

        return (Folder?)deserializer.Deserialize();
    }
}
