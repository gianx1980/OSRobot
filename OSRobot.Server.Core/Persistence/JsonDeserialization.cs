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
using System.Text.Json.Serialization;

namespace OSRobot.Server.Core.Persistence;

public class JsonDeserialization
{
    private class FolderTreeItem
    {
        public int id { get; set; }
        public string label { get; set; } = null!;
        public string icon { get; set; } = null!;
        public FolderTreeItem[] children { get; set; } = null!;
    }

    public class StringToIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Attempt to parse the string as an integer
                string stringValue = reader.GetString() ?? "0";
                if (int.TryParse(stringValue, out int intValue))
                {
                    return intValue;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }

            throw new JsonException($"Invalid token type {reader.TokenType} for integer conversion.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    private JsonDocument _jsonDoc;

    public JsonDeserialization(JsonDocument jsonDoc)
    {
        _jsonDoc = jsonDoc;
    }

    private List<Folder> _getFolderList(Folder folder)
    {
        List<Folder> result = new List<Folder>() { folder };
        
        if (folder.Items.Count > 0)
        {
            foreach (Folder folderChild in folder)
            {
                List<Folder> folderList = _getFolderList(folderChild);
                result.AddRange(folderList);
            }
        }

        return result;
    }

    private void _buildFolderTree(Folder folder, FolderTreeItem[] folderTreeChildren)
    {
        foreach (FolderTreeItem folderTreeItem in folderTreeChildren)
        {
            Folder childFolder = new Folder();
            childFolder.ParentFolder = folder;
            childFolder.Config = new FolderConfig()
            {
                Id = folderTreeItem.id,
                Name = folderTreeItem.label,
            };
            folder.Items.Add(childFolder);

            if (folderTreeItem.children.Length > 0)
                _buildFolderTree(childFolder, folderTreeItem.children);
        }
    }

    private object? DeserializeJsonDoc()
    {
        JsonElement jsonRootElement = _jsonDoc.RootElement;

        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            Converters = { new StringToIntConverter() },
            PropertyNameCaseInsensitive = true
        };

        // Deserialize workspace's folder tree structure
        JsonElement jsonFolderTree = jsonRootElement.GetProperty("folderTree");
        FolderTreeItem[]? folderTree = jsonFolderTree.Deserialize<FolderTreeItem[]>(jsonSerializerOptions);
        if (folderTree == null)
            throw new ApplicationException("Cannot deserialize folder tree structure.");

        // Build the folder tree structure
        Folder rootFolder = new Folder();
        rootFolder.Config = new FolderConfig()
        {
            Id = 0,
            Enabled = true,
            Log = true
        };

        _buildFolderTree(rootFolder, folderTree[0].children);

        // For convenience get a flat folder list
        List<Folder> folderList = _getFolderList(rootFolder);

        // Contains a reference to all plugin instances
        List<IPluginInstance> allPluginInstances = new List<IPluginInstance>();
        
        // Deserialize events and tasks of each folder
        foreach (Folder folder in folderList)
        {
            JsonElement jsonFolder = jsonRootElement.GetProperty($"folder_{folder.Config.Id}");

            JsonElement jsonNodes = jsonFolder.GetProperty("nodes");
            foreach (JsonElement jsonPluginObject in jsonNodes.EnumerateArray())
            {
                JsonElement jsonPluginObjectConfig = jsonPluginObject.GetProperty("workspaceItemConfig");
                JsonElement jsonPluginId = jsonPluginObjectConfig.GetProperty("pluginId");
                string pluginId = jsonPluginId.ToString();

                // We don't have to consider inner folders, so skip them
                if (pluginId == "Folder")
                    continue;

                // Get plugininstance type and create an instance of it
                Type? pluginInstanceType = Type.GetType($"OSRobot.Server.Plugins.{pluginId}.{pluginId}, OSRobot.Server.Plugins");
                if (pluginInstanceType == null)
                    throw new ApplicationException($"Cannot get type for: {pluginId}");

                IPluginInstance? pluginInstance = (IPluginInstance?)Activator.CreateInstance(pluginInstanceType);
                if (pluginInstance == null)
                    throw new ApplicationException($"An error occurred while creating an instance of type {pluginId}");
                pluginInstance.ParentFolder = folder;
                folder.Add(pluginInstance);

                // Deserialize configuration of the plugin instance
                Type? pluginInstanceConfigType = Type.GetType($"OSRobot.Server.Plugins.{pluginId}.{pluginId}Config, OSRobot.Server.Plugins");
                if (pluginInstanceConfigType == null)
                    throw new ApplicationException($"Cannot get configuration type for: {pluginId}");

                IPluginInstanceConfig? pluginInstanceConfig = (IPluginInstanceConfig?)jsonPluginObjectConfig.Deserialize(pluginInstanceConfigType, jsonSerializerOptions);
                if (pluginInstanceConfig == null)
                    throw new ApplicationException($"An error occurred while creating an instance of type {pluginId}Config");
                pluginInstance.Config = pluginInstanceConfig;

                allPluginInstances.Add(pluginInstance);
            }

            JsonElement jsonEdges = jsonFolder.GetProperty("edges");
            foreach (JsonElement jsonConnection in jsonEdges.EnumerateArray())
            {
                JsonElement jsonConnectionConfig = jsonConnection.GetProperty("workspaceConnectionConfig");

                PluginInstanceConnection? pluginInstanceConnection = (PluginInstanceConnection?)jsonConnectionConfig.Deserialize(typeof(PluginInstanceConnection), jsonSerializerOptions);
                if (pluginInstanceConnection == null)
                    throw new ApplicationException($"An error occurred while creating an instance of type PluginInstanceConnection");

                JsonElement jsonSource = jsonConnectionConfig.GetProperty("source");
                int sourceId = int.Parse(jsonSource.ToString());
                JsonElement jsonTarget = jsonConnectionConfig.GetProperty("target");
                int targetId = int.Parse(jsonTarget.ToString());

                IPluginInstance? source = allPluginInstances.Where(p => p.Config.Id == sourceId).FirstOrDefault();
                if (source == null)
                    throw new ApplicationException($"Building connection: cannot find source object with Id: {sourceId}");

                IPluginInstance? target = allPluginInstances.Where(p => p.Config.Id == targetId).FirstOrDefault();
                if (target == null)
                    throw new ApplicationException($"Building connection: cannot find target object with Id: {targetId}");

                pluginInstanceConnection.ConnectTo = target;
                source.Connections.Add(pluginInstanceConnection);
            }
        }
        return rootFolder;
    }

    public object? Deserialize()
    {
        return DeserializeJsonDoc();
    }
    
}
