// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WriteBinaryFileTask;

public class WriteBinaryFileTask : SingleIterationTask
{
    protected override async Task RunSingleIterationTaskAsync()
    {
        for (int i = 0; i < _iterationsCount; i++)
        {
            WriteBinaryFileTaskConfig? config = (WriteBinaryFileTaskConfig?)CoreHelpers.CloneObjects(Config) ?? throw new ApplicationException("Cloning configuration returned null");
            DynamicDataParser.Parse(config, _dataChain, i, _subInstanceIndex);

            // The varbinary type is handled differently from the others and therefore requires separate handling.
            List<DynamicDataInfo> dynDataInfoList = DynamicDataParser.GetDynamicDataInfo(config.FileContentSource);
            if (dynDataInfoList.Count > 1)
                throw new ApplicationException("Multiple dynamic data are note allowed for Varbinary parameters.");

            DynamicDataInfo dynDataInfo = dynDataInfoList[0];
            byte[]? fileContent = (byte[]?)DynamicDataParser.GetDynamicDataValue(dynDataInfo, _dataChain, i, _subInstanceIndex);
            if (fileContent == null)
                return;

            await File.WriteAllBytesAsync(config.FilePath, fileContent, _cancellationToken);
        }
    }
}
