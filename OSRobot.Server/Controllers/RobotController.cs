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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OSRobot.Server.Core;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.JobEngineLib.Infrastructure.Abstract;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.Robot;
using OSRobot.Server.Configuration;
using System.Text.Json;
using OSRobot.Server.Models.DTO.ClientUtils;
using MySqlX.XDevAPI.Common;

namespace OSRobot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotController(IJobEngine jobEngine, IOptions<AppSettings> appSettings) : AppControllerBase
{
    private readonly IJobEngine _jobEngine = jobEngine;
    private readonly AppSettings _appSettings = appSettings.Value;

    [HttpGet]
    [Route("Objects")]
    [Authorize]
    public IActionResult Objects()
    {
        List<PluginListItem> pluginList = [.. _jobEngine.GetPlugins().Select(t => new PluginListItem(t.Id, t.Title, t.PluginType.ToString().ToLowerInvariant(), t.GetPluginDefaultConfig(), t.SupportedOSPlatforms))];
        ResponseModel<List<PluginListItem>> response = new(ResponseCode.ResponseOk, null, pluginList);
        return Ok(response);
    }

    [HttpGet]
    [Route("DynDataSamples")]
    [Authorize]
    public IActionResult DynDataSamples(string pluginId)
    {
        IPlugin? plugin = _jobEngine.GetPlugin(pluginId);
        if (plugin == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "PluginId not found.");
            return BadRequest(errorResp);
        }

        List<PluginDynDataSampleListItem> dynDataSamplesList = [.. plugin.SampleDynamicData.Select(t => new PluginDynDataSampleListItem(t.Description, t.Example, t.InternalName))];

        ResponseModel<List<PluginDynDataSampleListItem>> response = new(ResponseCode.ResponseOk, null, dynDataSamplesList);
        return Ok(response);
    }

    [HttpGet]
    [Route("WorkspaceJobs")]
    [Authorize]
    public IActionResult WorkspaceJobs()
    {
        // Check the existence of data
        if (!System.IO.File.Exists(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json")))
        {
            ResponseModel errorResp = new(ResponseCode.ErrorLoadingJobs, "Job configuration not found");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        string configuration = System.IO.File.ReadAllText(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json"));
        if (string.IsNullOrEmpty(configuration))
        {
            ResponseModel errorResp = new(ResponseCode.ErrorLoadingJobs, "Job configuration empty");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        // Deserialize the json configuration and return to client
        var configJson = JsonSerializer.Deserialize<object>(configuration);        
        ResponseModel<object> response = new(ResponseCode.ResponseOk, null, configJson);
        return Ok(response);
    }

    [HttpPost]
    [Route("WorkspaceJobs")]
    [Authorize]
    public IActionResult WorkspaceJobs([FromBody] object requestBody)
    {
        string? workspaceJobs = requestBody.ToString();
        if (string.IsNullOrEmpty(workspaceJobs))
        {
            ResponseModel errorResp = new(ResponseCode.ErrorSavingJobs, "Jobs configuration is empty");
            return BadRequest(errorResp);
        }

        try
        {
            System.IO.File.WriteAllText(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json"), workspaceJobs);

            ResponseModel response = new(ResponseCode.ResponseOk, null);
            return Ok(response);
        }
        catch
        {
            ResponseModel errResp = new(ResponseCode.ErrorSavingJobs, "An error occurred while saving the jobs.");
            return BadRequest(errResp);
        }
    }

    [HttpPost]
    [Route("StartTask")]
    [Authorize]
    public IActionResult StartTask([FromQuery] int taskId)
    {
        bool result = _jobEngine.StartTask(taskId);

        if (!result)
        {
            ResponseModel errorResp = new(ResponseCode.CannotStartTask, "An error occurred while starting the task");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        ResponseModel response = new(ResponseCode.ResponseOk, null);
        return Ok(response);
    }

    [HttpPost]
    [Route("ReloadJobsConfig")]
    [Authorize]
    public IActionResult ReloadJobsConfig()
    {
        ReloadJobsReturnValues result = _jobEngine.ReloadJobs();

        if (result == ReloadJobsReturnValues.CannotReloadWhileRunningTask)
        {
            ResponseModel errorResp = new(ResponseCode.CannotStartTask, "Cannot reload jobs because there are running jobs, plese retry later.");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        if (result == ReloadJobsReturnValues.GenericError)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, null);
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }


        ResponseModel response = new(ResponseCode.ResponseOk, null);
        return Ok(response);
    }

    [HttpGet]
    [Route("FolderLogs")]
    [Authorize]
    public IActionResult FolderLogs([FromQuery] int folderId)
    {
        List<LogInfo> folderLogs = _jobEngine.GetFolderLogs(folderId);

        List<LogInfoListItem> logInfoList =
        [
            ..folderLogs.Select(folderLog => new LogInfoListItem(folderLog.EventId,
                                                                   folderLog.ExecDateTime,
                                                                   folderLog.FileName
                                                                   ))
        ];

        ResponseModel<List<LogInfoListItem>> response = new(ResponseCode.ResponseOk, null, logInfoList);

        return Ok(response);
    }

    [HttpGet]
    [Route("FolderInfo")]
    [Authorize]
    public IActionResult FolderInfo([FromQuery] int folderId)
    {
        FolderInfo? folderInfo = _jobEngine.GetFolderInfo(folderId);
        if (folderInfo == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "FolderId not found.");
            return BadRequest(errorResp);
        }
            
        ResponseModel<FolderInfo> response = new(ResponseCode.ResponseOk, null, (FolderInfo)folderInfo);

        return Ok(response);
    }

    [HttpGet]
    [Route("LogContent")]
    [Authorize]
    public IActionResult LogContent([FromQuery] int folderId, [FromQuery] string logFileName)
    {
        string? logContent = _jobEngine.GetLogContent(folderId, logFileName);
        if (logContent == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "Log file not found.");
            return BadRequest(errorResp);
        }
        
        ResponseModel<string> response = new(ResponseCode.ResponseOk, null, logContent);

        return Ok(response);
    }
}
