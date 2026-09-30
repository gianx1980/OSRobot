// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.ClientUtils;

public class OSInfoResponse
{
    public OSInfoResponse()
    {
        MachineName = Environment.MachineName;
        OSVersion = Environment.OSVersion.ToString();
        Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
        ProcessorCount = Environment.ProcessorCount;
        DirectorySeparatorChar = Path.DirectorySeparatorChar;
    }

    public string MachineName { get; } 
    public string OSVersion { get; }
    public bool Is64BitOperatingSystem { get; }
    public int ProcessorCount { get; }  
    public char DirectorySeparatorChar { get; }
}
