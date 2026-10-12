// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text;

namespace OSRobot.Server.Plugins.SqlServerFileLoaderTask;

/// <summary>Turns a user-written table name into a safely quoted SQL Server identifier.</summary>
public static class SqlTableName
{
    /// <summary>
    /// "MyTable", "dbo.MyTable", "[My schema].[My.Table]" or "Db.dbo.MyTable" become "[MyTable]", "[dbo].[MyTable]"...
    /// Brackets are optional; inside them a dot is part of the name.
    /// </summary>
    public static string Quote(string tableName)
    {
        List<string> parts = Split(tableName);
        if (parts.Count is < 1 or > 3 || parts.Any(string.IsNullOrWhiteSpace))
            throw new ApplicationException($"'{tableName}' is not a valid table name: use Table, Schema.Table or Database.Schema.Table.");

        return string.Join(".", parts.Select(QuoteIdentifier));
    }

    public static string QuoteIdentifier(string name) => "[" + name.Replace("]", "]]") + "]";

    private static List<string> Split(string tableName)
    {
        List<string> parts = [];
        StringBuilder current = new();
        bool bracketed = false;
        bool inBrackets = false;
        bool afterClosingBracket = false;
        string text = tableName.Trim();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inBrackets)
            {
                if (c == ']' && i + 1 < text.Length && text[i + 1] == ']')
                {
                    current.Append(']');
                    i++;
                }
                else if (c == ']')
                {
                    inBrackets = false;
                    afterClosingBracket = true;
                }
                else
                    current.Append(c);
            }
            else if (c == '.')
            {
                parts.Add(bracketed ? current.ToString() : current.ToString().Trim());
                current.Clear();
                bracketed = afterClosingBracket = false;
            }
            else if (afterClosingBracket)
            {
                // A bracketed name is complete: only spaces may follow it before the next dot.
                if (!char.IsWhiteSpace(c))
                    throw new ApplicationException($"'{tableName}' is not a valid table name: unexpected '{c}' after ']'.");
            }
            else if (c == '[' && current.ToString().Trim().Length == 0)
            {
                current.Clear();
                inBrackets = bracketed = true;
            }
            else
                current.Append(c);
        }

        if (inBrackets)
            throw new ApplicationException($"'{tableName}' is not a valid table name: a '[' is not closed.");

        parts.Add(bracketed ? current.ToString() : current.ToString().Trim());
        return parts;
    }
}
