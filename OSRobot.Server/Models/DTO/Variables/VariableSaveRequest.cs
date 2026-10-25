// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Variables;

/// <summary>Creates or updates (by name, ignoring case) a variable or a secret.</summary>
public class VariableSaveRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The new value. For an existing secret, null or empty keeps the current value (the editor never
    /// has it to send back).
    /// </summary>
    public string? Value { get; set; }

    public bool IsSecret { get; set; }

    public string? Description { get; set; }
}
