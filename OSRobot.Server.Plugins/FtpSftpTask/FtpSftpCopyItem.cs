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

namespace OSRobot.Server.Plugins.FtpSftpTask;


public class FtpSftpCopyItem
{
    public bool LocalToRemote { get; set; }

    public string LocalPath { get; set; } = string.Empty;
    
    public string RemotePath { get; set; } = string.Empty;

    public bool OverwriteFileIfExists { get; set; }

    public bool RecursivelyCopyDirectories { get; set; }

    public override string ToString()
    {
        string Result = LocalToRemote ? $"{Resource.TxtFrom}: {LocalPath} {Resource.TxtTo}: {RemotePath}" : $"{Resource.TxtFrom}: {RemotePath} {Resource.TxtTo}: {LocalPath}";

        return $"{Result} {(OverwriteFileIfExists ? Resource.TxtOverwriteIfExists : string.Empty)} {(RecursivelyCopyDirectories ? Resource.TxtRecursiveCopy : string.Empty)}";
    }
}
