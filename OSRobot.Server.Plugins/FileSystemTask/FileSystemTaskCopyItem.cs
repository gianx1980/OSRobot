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

namespace OSRobot.Server.Plugins.FileSystemTask;

public class FileSystemTaskCopyItem
{
    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string FilesOlderThanDays { get; set; } = string.Empty;
    public string FilesOlderThanHours { get; set; } = string.Empty;     
    public string FilesOlderThanMinutes { get; set; } = string.Empty;

    public bool OverwriteFileIfExists { get; set; }

    public bool RecursivelyCopy { get; set; }

    public override string ToString()
    {
        string result = $"{Resource.TxtFrom}: {SourcePath} {Resource.TxtTo}: {DestinationPath}";

        return $"{result} {(OverwriteFileIfExists ? Resource.TxtOverwriteIfExists : string.Empty)} {(RecursivelyCopy ? Resource.TxtRecursiveCopy : string.Empty)}";
    }
}
