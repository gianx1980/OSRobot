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

using System.Collections;

namespace OSRobot.Server.Plugins.Infrastructure.Utilities.FileSystem;

public class FileSystemEnumerator(string pathPattern, bool recursive = false) : IEnumerable<string>
{
    public IEnumerator<string> GetEnumerator()
    {
        foreach (string item in EnumerateFiles(pathPattern))
        {
            yield return item;
        }
    }

    private IEnumerable<string> EnumerateFiles(string enumPathPattern)
    {
        // Path is a single file
        if (File.Exists(enumPathPattern))
        {
            yield return enumPathPattern;
            yield break;
        }

        // Path is a directory or a pattern 
        // Remove trailing slash
        if (enumPathPattern.EndsWith(Path.DirectorySeparatorChar))
            enumPathPattern = enumPathPattern.Substring(0, enumPathPattern.Length - 1);

        bool pathPatternIsDirectory = Directory.Exists(enumPathPattern);
        string? pathDirectory = Path.GetDirectoryName(enumPathPattern);
        string pathLastSegment = Path.GetFileName(enumPathPattern);

        string? searchPattern;
        DirectoryInfo pathPatternDirectoryInfo;

        if (pathPatternIsDirectory)
        {
            searchPattern = "*";
            pathPatternDirectoryInfo = new DirectoryInfo(enumPathPattern);
        }
        else
        {
            searchPattern = pathLastSegment;
            pathDirectory = Path.GetDirectoryName(enumPathPattern);
            pathPatternDirectoryInfo = new DirectoryInfo(pathDirectory!);
        }

        foreach (FileInfo file in pathPatternDirectoryInfo.GetFiles(searchPattern))
        {
            yield return file.FullName;
        }

        DirectoryInfo[] subDirectories = pathPatternDirectoryInfo.GetDirectories();

        if (recursive)
        {
            foreach (DirectoryInfo subDir in subDirectories)
            {
                foreach (string item in EnumerateFiles(subDir.FullName))
                {
                    yield return item;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}




