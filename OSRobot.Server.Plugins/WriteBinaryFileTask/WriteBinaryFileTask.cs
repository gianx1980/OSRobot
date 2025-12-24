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

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WriteBinaryFileTask;

public class WriteBinaryFileTask : IterationTask
{
    protected override void RunIteration(int currentIteration)
    {
        WriteBinaryFileTaskConfig tConfig = (WriteBinaryFileTaskConfig)_iterationConfig;

        // The varbinary type is handled differently from the others and therefore requires separate handling.
        List<DynamicDataInfo> dynDataInfoList = DynamicDataParser.GetDynamicDataInfo(tConfig.FileContentSource);
        if (dynDataInfoList.Count > 1)
            throw new ApplicationException("Multiple dynamic data are note allowed for Varbinary parameters.");

        DynamicDataInfo dynDataInfo = dynDataInfoList[0];
        byte[]? fileContent = (byte[]?)DynamicDataParser.GetDynamicDataValue(dynDataInfo, _dataChain, currentIteration, _subInstanceIndex);
        if (fileContent == null)
            return;

        File.WriteAllBytes(tConfig.FilePath, fileContent);
    }
}
