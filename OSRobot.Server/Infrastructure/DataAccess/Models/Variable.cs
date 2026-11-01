// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Infrastructure.DataAccess.Models;

/// <summary>A variable or a secret. See ConfigDatabaseSchema.UpgradeVariablesSchema.</summary>
public class Variable
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>The value of a variable, or the Data Protection ciphertext of a secret.</summary>
    public string Value { get; set; } = null!;

    public bool IsSecret { get; set; }

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
