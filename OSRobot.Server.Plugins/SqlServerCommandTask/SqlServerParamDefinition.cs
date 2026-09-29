// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
    Bit,
    VarBinary
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
