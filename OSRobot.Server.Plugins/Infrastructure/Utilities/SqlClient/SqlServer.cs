// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Data.SqlClient;
using OSRobot.Server.Core.Logging.Abstract;
using System.Data;
using System.Text;

namespace OSRobot.Server.Plugins.Infrastructure.Utilities.SqlClient;

public class SqlServerDatabaseListItem(int id, string name)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
}

public static class SqlServer
{
    private static string BuildConnectionString(string server, string? database, string username, string password, string? connectionStringOptions)
    {
        string connectionString = $"Server={server};";
        if (database != null)
            connectionString += $"Database={database};";
        connectionString += $"User Id={username};Password={password};{connectionStringOptions}";

        return connectionString;
    }

    public static bool TestConnection(string server, string? database, string username, string password, string? connectionStringOptions, IPluginInstanceLogger? logger = null)
    {
        string connectionString = BuildConnectionString(server, database, username, password, connectionStringOptions);

        try
        {
            using SqlConnection cnt = new(connectionString);
            cnt.Open();

            return true;
        }
        catch (Exception ex)
        {
            logger?.Error($"Connection test against server '{server}' failed", ex);
        }

        return false;
    }

    public static List<SqlServerDatabaseListItem>? GetDatabaseList(string server, string username, string password, string? connectionStringOptions, bool onlyUserDatabases = false, IPluginInstanceLogger? logger = null)
    {
        string connectionString = BuildConnectionString(server, null, username, password, connectionStringOptions);

        List<SqlServerDatabaseListItem>? databaseList = [];

        using SqlConnection cnt = new(connectionString);
        using SqlCommand cmdDatabases = new("SELECT database_id, name FROM sys.databases", cnt);

        try
        {
            cnt.Open();
            using SqlDataReader reader = cmdDatabases.ExecuteReader();

            while (reader.Read())
            {
                int dbId = reader.GetInt32("database_id"); 
                string dbName = reader.GetString("name");
                
                if (!onlyUserDatabases || onlyUserDatabases && dbName != "master" && dbName != "model" && dbName != "msdb" && dbName != "tempdb")
                    databaseList.Add(new SqlServerDatabaseListItem(dbId, dbName));
            }
        }
        catch (Exception ex)
        {
            logger?.Error($"Could not retrieve the database list from server '{server}'", ex);
            databaseList = null;
        }

        return databaseList;
    }
}
