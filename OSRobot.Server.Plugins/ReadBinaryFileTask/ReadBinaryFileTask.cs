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

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Plugins.Infrastructure.Utilities.FileSystem;

namespace OSRobot.Server.Plugins.ReadBinaryFileTask;

public class ReadBinaryFileTask : IterationTask
{
    protected override void RunIteration(int currentIteration)
    {
        ReadBinaryFileTaskConfig tConfig = (ReadBinaryFileTaskConfig)_iterationConfig;

        DataTable dtFiles = (DataTable)_defaultRecordset;
        dtFiles.Columns.Add("FullName", typeof(string));
        dtFiles.Columns.Add("FileContent", typeof(byte[]));

        FileSystemEnumerator fileSystemEnumerator = new(tConfig.FilePath, tConfig.Recursive);

        foreach (string file in fileSystemEnumerator)
        {
            _instanceLogger?.Info(this, $"Reading file \"{file}\"...");

            byte[] fileContent = File.ReadAllBytes(file);
            DataRow dr = dtFiles.NewRow();
            dr["FullName"] = file;
            dr["FileContent"] = fileContent;
            dtFiles.Rows.Add(dr);
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
