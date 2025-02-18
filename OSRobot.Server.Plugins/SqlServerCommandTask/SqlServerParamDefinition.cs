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


using System.Text.Json.Serialization;

namespace OSRobot.Server.Plugins.SqlServerCommandTask;

public enum SqlParamType
{
    Varchar,
    NVarchar,
    Xml,
    Numeric,
    Int,
    Long,
    Date,
    Datetime,
    Bit
}

public class SqlServerParamDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SqlParamType Type { get; set; }
    public string Length { get; set; } = string.Empty;
    public string Precision { get; set; } = string.Empty;

    public override string ToString()
    {
        string Result = Name;

        if (!string.IsNullOrEmpty(Value))
            Result += ": " + Value;
        else
            Result += ": " + Resource.TxtNoValue;

        return Result;
    }
}
