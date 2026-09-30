// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.FileSystemTask;

public enum FileSystemTaskCommandType
{
    Copy,
    Delete,
    CreateFolder,
    CheckExistence,
    List,
    Rename
}

public class FileSystemTaskConfig : ITaskConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Log { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IterationMode PluginIterationMode { get; set; }
    public string IterationObject { get; set; } = string.Empty;
    public int IterationsCount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FileSystemTaskCommandType Command { get; set; }


    public List<FileSystemTaskCopyItem> CopyItems { get; set; } = [];

    public List<FileSystemTaskDeleteItem> DeleteItems { get; set; } = [];

    [DynamicData]
    public string CheckExistenceFilePath { get; set; } = string.Empty;

    [DynamicData]
    public string CreateFolderPath { get; set; } = string.Empty;


    [DynamicData]
    public string ListFolderPath { get; set; } = string.Empty;
    public bool ListFiles { get; set; } = true;
    public bool ListFolders { get; set; } = true;

    public bool ListSubfoldersContent { get; set; }

    [DynamicData]
    public string RenameFromPath { get; set; } = string.Empty;

    [DynamicData]
    public string RenameToPath { get; set; } = string.Empty;

}
