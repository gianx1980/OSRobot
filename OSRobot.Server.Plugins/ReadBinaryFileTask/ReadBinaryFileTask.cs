// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Plugins.Infrastructure.Utilities.FileSystem;
using System.Data;

namespace OSRobot.Server.Plugins.ReadBinaryFileTask;

public class ReadBinaryFileTask : MultipleIterationTask
{
    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        ReadBinaryFileTaskConfig config = (ReadBinaryFileTaskConfig)_iterationTaskConfig;

        DataTable dtFiles = (DataTable)_defaultRecordset;
        dtFiles.Columns.Add("FullName", typeof(string));
        dtFiles.Columns.Add("Name", typeof(string));
        dtFiles.Columns.Add("Extension", typeof(string));
        dtFiles.Columns.Add("Size", typeof(long));
        dtFiles.Columns.Add("CreationTime", typeof(DateTime));
        dtFiles.Columns.Add("LastAccessTime", typeof(DateTime));
        dtFiles.Columns.Add("LastWriteTime", typeof(DateTime));
        dtFiles.Columns.Add("FileContent", typeof(byte[]));

        FileSystemEnumerator fileSystemEnumerator = new(config.FilePath, config.Recursive);

        foreach (string file in fileSystemEnumerator)
        {
            _instanceLogger?.Info(this, $"Reading file \"{file}\"...");

            byte[] fileContent = await File.ReadAllBytesAsync(file, _cancellationToken);
            FileInfo fileInfo = new FileInfo(file);
            DataRow dr = dtFiles.NewRow();
            dr["FullName"] = fileInfo.FullName;
            dr["Name"] = fileInfo.Name;
            dr["Extension"] = fileInfo.Extension;
            dr["Size"] = fileInfo.Length;
            dr["CreationTime"] = fileInfo.CreationTime;
            dr["LastAccessTime"] = fileInfo.LastAccessTime;
            dr["LastWriteTime"] = fileInfo.LastWriteTime;
            dr["FileContent"] = fileContent;
            dtFiles.Rows.Add(dr);
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
