// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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




