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

using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OSRobot.Server.Plugins.Infrastructure.Utilities;

public class SqlServerDatabaseListItem
{
    public SqlServerDatabaseListItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; set; } 
    public string Name { get; set; }
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

    public static bool TestConnection(string server, string? database, string username, string password, string? connectionStringOptions)
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
            System.Diagnostics.Debug.WriteLine(ex.Message);
            // TODO: Log error?
        }

        return false;
    }

    public static List<SqlServerDatabaseListItem>? GetDatabaseList(string server, string username, string password, string? connectionStringOptions, bool onlyUserDatabases = false)
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
                
                if (!onlyUserDatabases || (onlyUserDatabases && dbName != "master" && dbName != "model" && dbName != "msdb" && dbName != "tempdb"))
                    databaseList.Add(new SqlServerDatabaseListItem(dbId, dbName));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            // TODO: Log error?
            databaseList = null;
        }

        return databaseList;
    }

}
