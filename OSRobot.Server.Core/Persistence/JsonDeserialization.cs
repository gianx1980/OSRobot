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

public class JsonDeserialization(JsonDocument jsonDoc)
{
    private class FolderTreeItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("label")]
        public string Label { get; set; } = null!;
        [JsonPropertyName("Icon")]
        public string Icon { get; set; } = null!;
        [JsonPropertyName("children")]
        public FolderTreeItem[] Children { get; set; } = null!;
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

    private readonly JsonDocument _jsonDoc = jsonDoc;

    private List<Folder> GetFolderList(Folder folder)
    {
        List<Folder> result = [ folder ];
        
        if (folder.Items.Count > 0)
        {
            foreach (Folder folderChild in folder.Cast<Folder>())
            {
                List<Folder> folderList = GetFolderList(folderChild);
                result.AddRange(folderList);
            }
        }

        return result;
    }

    private void BuildFolderTree(Folder folder, FolderTreeItem[] folderTreeChildren)
    {
        foreach (FolderTreeItem folderTreeItem in folderTreeChildren)
        {
            Folder childFolder = new()
            {
                ParentFolder = folder,
                Config = new FolderConfig()
                {
                    Id = folderTreeItem.Id,
                    Name = folderTreeItem.Label,
                }
            };
            folder.Items.Add(childFolder);

            if (folderTreeItem.Children.Length > 0)
                BuildFolderTree(childFolder, folderTreeItem.Children);
        }
    }

    #pragma warning disable CA1859
    private object? DeserializeJsonDoc()
    {
        JsonElement jsonRootElement = _jsonDoc.RootElement;

        JsonSerializerOptions jsonSerializerOptions = new()
        {
            Converters = { new StringToIntConverter() },
            PropertyNameCaseInsensitive = true
        };

        // Deserialize workspace's folder tree structure
        JsonElement jsonFolderTree = jsonRootElement.GetProperty("folderTree");
        FolderTreeItem[]? folderTree = jsonFolderTree.Deserialize<FolderTreeItem[]>(jsonSerializerOptions) ?? throw new ApplicationException("Cannot deserialize folder tree structure.");

        // Build the folder tree structure
        Folder rootFolder = new()
        {
            Config = new FolderConfig()
            {
                Id = 0,
                Enabled = true,
                Log = true
            }
        };

        BuildFolderTree(rootFolder, folderTree[0].Children);

        // For convenience get a flat folder list
        List<Folder> folderList = GetFolderList(rootFolder);

        // Contains a reference to all plugin instances
        List<IPluginInstance> allPluginInstances = [];
        
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
                Type? pluginInstanceType = Type.GetType($"OSRobot.Server.Plugins.{pluginId}.{pluginId}, OSRobot.Server.Plugins") ?? throw new ApplicationException($"Cannot get type for: {pluginId}");
                IPluginInstance? pluginInstance = (IPluginInstance?)Activator.CreateInstance(pluginInstanceType) ?? throw new ApplicationException($"An error occurred while creating an instance of type {pluginId}");
                pluginInstance.ParentFolder = folder;
                folder.Add(pluginInstance);

                // Deserialize configuration of the plugin instance
                Type? pluginInstanceConfigType = Type.GetType($"OSRobot.Server.Plugins.{pluginId}.{pluginId}Config, OSRobot.Server.Plugins") ?? throw new ApplicationException($"Cannot get configuration type for: {pluginId}");
                IPluginInstanceConfig? pluginInstanceConfig = (IPluginInstanceConfig?)jsonPluginObjectConfig.Deserialize(pluginInstanceConfigType, jsonSerializerOptions) ?? throw new ApplicationException($"An error occurred while creating an instance of type {pluginId}Config");
                pluginInstance.Config = pluginInstanceConfig;

                allPluginInstances.Add(pluginInstance);
            }

            JsonElement jsonEdges = jsonFolder.GetProperty("edges");
            foreach (JsonElement jsonConnection in jsonEdges.EnumerateArray())
            {
                JsonElement jsonConnectionConfig = jsonConnection.GetProperty("workspaceConnectionConfig");

                PluginInstanceConnection? pluginInstanceConnection = (PluginInstanceConnection?)jsonConnectionConfig.Deserialize(typeof(PluginInstanceConnection), jsonSerializerOptions) ?? throw new ApplicationException($"An error occurred while creating an instance of type PluginInstanceConnection");
                JsonElement jsonSource = jsonConnectionConfig.GetProperty("source");
                int sourceId = int.Parse(jsonSource.ToString());
                JsonElement jsonTarget = jsonConnectionConfig.GetProperty("target");
                int targetId = int.Parse(jsonTarget.ToString());

                IPluginInstance? source = allPluginInstances.Where(p => p.Config.Id == sourceId).FirstOrDefault() ?? throw new ApplicationException($"Building connection: cannot find source object with Id: {sourceId}");
                IPluginInstance? target = allPluginInstances.Where(p => p.Config.Id == targetId).FirstOrDefault() ?? throw new ApplicationException($"Building connection: cannot find target object with Id: {targetId}");
                pluginInstanceConnection.ConnectTo = target;
                source.Connections.Add(pluginInstanceConnection);
            }
        }
        return rootFolder;
    }
    #pragma warning restore CA1859

    public object? Deserialize()
    {
        return DeserializeJsonDoc();
    }
    
}
