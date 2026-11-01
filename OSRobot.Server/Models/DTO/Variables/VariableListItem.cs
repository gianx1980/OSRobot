// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Variables;

/// <param name="value">The value of a variable; always null for a secret, whose value never leaves the server.</param>
/// <param name="isReadable">False for a secret that can't be decrypted with the current keys.</param>
public class VariableListItem(string name, bool isSecret, string? value, string? description, DateTime updatedAt, string? updatedBy, bool isReadable)
{
    public string Name { get; } = name;
    public bool IsSecret { get; } = isSecret;
    public string? Value { get; } = value;
    public string? Description { get; } = description;
    public DateTime UpdatedAt { get; } = updatedAt;
    public string? UpdatedBy { get; } = updatedBy;
    public bool IsReadable { get; } = isReadable;
}
