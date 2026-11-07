// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO.Enumeration;

namespace OSRobot.Server.Plugins.ServiceProcessWatchEvent;

/// <summary>
/// One or more names separated by ";", where * stands for any characters and ? for one. The whole name must
/// match, case-insensitively, as Windows compares service and program names. Every other character is literal
/// (a backslash would escape the next one, but these names can't contain it), so names like MSSQL$SQLEXPRESS
/// can be typed as they are.
/// </summary>
public sealed class NamePattern
{
    private readonly string[] _patterns;

    public NamePattern(string patterns)
    {
        _patterns = patterns.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>No name was given: nothing matches.</summary>
    public bool IsEmpty => _patterns.Length == 0;

    public bool IsMatch(string name) => _patterns.Any(p => FileSystemName.MatchesSimpleExpression(p, name, ignoreCase: true));
}
