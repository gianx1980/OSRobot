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
namespace OSRobot.Server.Core.DynamicData;

public class DynamicDataSample(string internalName, string description, string example, bool isRecordset = false)
{
    public string InternalName { get; private set; } = internalName;
    public string Description { get; private set; } = description;
    public string Example { get; private set; } = example;
    public bool IsRecordset { get; private set; } = isRecordset;

    public override string ToString()
    {
        return $"{Description} ({InternalName})"; 
    }
}
