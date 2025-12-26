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
using System.Diagnostics;

namespace OSRobot.Server.Plugins.RunProgramTask;

public class RunProgramTask : MultipleIterationTask
{
    protected override void RunMultipleIterationTask(int currentIteration)
    {
        RunProgramTaskConfig config = (RunProgramTaskConfig)_iterationTaskConfig;

        ProcessStartInfo pInfo = new(config.ProgramPath, config.Parameters);
        string defaultWorkingFolder = Path.GetDirectoryName(config.ProgramPath) ?? string.Empty;
        pInfo.WorkingDirectory = string.IsNullOrEmpty(config.WorkingFolder) ? defaultWorkingFolder : config.WorkingFolder;
        _instanceLogger.Info(this, $"Running program: {config.ProgramPath} Parameters: {config.Parameters} Working folder: {pInfo.WorkingDirectory}");

        using Process? newProc = Process.Start(pInfo);
        if (newProc == null)
        {
            _instanceLogger.Error(this, "Run program failed: Process.Start returned null.");
            return;
        }
        
        newProc.WaitForExit();    
    }
}
