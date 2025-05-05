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
