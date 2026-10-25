// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Data.Sqlite;

namespace OSRobot.Server.Infrastructure.DataAccess;

/// <summary>Additive schema steps for config.db, run at every startup (there are no EF migrations).</summary>
public static class ConfigDatabaseSchema
{
    /// <summary>
    /// Creates the Variables table if missing: a new database and one created before variables existed
    /// get it the same way. Names are unique regardless of case, like the placeholders that use them.
    /// Value holds the plain value of a variable, or the Data Protection ciphertext of a secret.
    /// </summary>
    public static void UpgradeVariablesSchema(string dbConnectionString)
    {
        using SqliteConnection connection = new(dbConnectionString);
        connection.Open();

        using SqliteCommand create = new("""
            CREATE TABLE IF NOT EXISTS Variables
            (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                Value       TEXT    NOT NULL,
                IsSecret    INTEGER NOT NULL DEFAULT 0,
                Description TEXT    NULL,
                UpdatedAt   TEXT    NOT NULL,
                UpdatedBy   TEXT    NULL
            );
            """, connection);
        create.ExecuteNonQuery();
    }
}
