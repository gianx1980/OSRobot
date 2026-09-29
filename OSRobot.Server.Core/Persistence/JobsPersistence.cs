// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
