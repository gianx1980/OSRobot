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
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Core.Persistence;
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
public class RobotController(IJobEngine jobEngine, IOptions<AppSettings> appSettings, IAuditLogger auditLogger, ILogger<RobotController> logger) : AppControllerBase
{
    private readonly IJobEngine _jobEngine = jobEngine;
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly IAuditLogger _auditLogger = auditLogger;
    private readonly ILogger<RobotController> _logger = logger;

    [HttpGet]
    [Route("Objects")]
    [Authorize]
    public ActionResult<ResponseModel<List<PluginListItem>>> Objects()
    {
        List<PluginListItem> pluginList = [.. _jobEngine.GetPlugins().Select(t => new PluginListItem(t.Id, t.Title, t.PluginType.ToString().ToLowerInvariant(), t.GetPluginDefaultConfig(), t.SupportedOSPlatforms))];
        ResponseModel<List<PluginListItem>> response = new(ResponseCode.ResponseOk, null, pluginList);
        return Ok(response);
    }

    [HttpGet]
    [Route("DynDataSamples")]
    [Authorize]
    public ActionResult<ResponseModel<List<PluginDynDataSampleListItem>>> DynDataSamples(string pluginId)
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
    public ActionResult<ResponseModel<object>> WorkspaceJobs()
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
    public ActionResult<ResponseModel> WorkspaceJobs([FromBody] JsonElement requestBody)
    {
        if (requestBody.ValueKind != JsonValueKind.Object)
        {
            ResponseModel errorResp = new(ResponseCode.ErrorSavingJobs, "Jobs configuration is empty");
            return BadRequest(errorResp);
        }

        string workspaceJobs = requestBody.GetRawText();

        // Validate structurally before touching anything on disk, using the exact same
        // deserializer JobEngine uses to load jobs.json at startup/reload. If this can't build a
        // valid Folder tree (an unknown plugin id, a connection pointing at a nonexistent
        // object, a malformed plugin config, ...) nothing gets written - a bad save can no
        // longer corrupt the live workspace or silently break the next engine reload.
        try
        {
            using JsonDocument jsonDoc = JsonDocument.Parse(workspaceJobs);
            _ = new JsonDeserialization(jsonDoc).Deserialize();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rejected an invalid job configuration submitted by user '{Username}'.", AppUser?.Username);
            ResponseModel errorResp = new(ResponseCode.InvalidJobsConfiguration, $"The submitted job configuration is invalid: {ex.Message}");
            return BadRequest(errorResp);
        }

        string jobsFilePath = Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json");
        string tempFilePath = jobsFilePath + ".tmp";
        string backupFilePath = jobsFilePath + ".bak";

        try
        {
            // Write to a temp file, then swap it into place with a single atomic filesystem
            // operation (File.Replace also preserves the previous live file as a .bak). A crash
            // or power loss mid-write can now only ever leave the .tmp file incomplete - never
            // the live jobs.json, which either fully updates or isn't touched at all.
            System.IO.File.WriteAllText(tempFilePath, workspaceJobs);

            if (System.IO.File.Exists(jobsFilePath))
                System.IO.File.Replace(tempFilePath, jobsFilePath, backupFilePath);
            else
                System.IO.File.Move(tempFilePath, jobsFilePath);

            _auditLogger.Info($"User '{AppUser?.Username}' saved the job configuration ({workspaceJobs.Length} bytes).");

            ResponseModel response = new(ResponseCode.ResponseOk, null);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving jobs.json.");

            try
            {
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temporary file '{TempFilePath}' after a failed save.", tempFilePath);
            }

            ResponseModel errResp = new(ResponseCode.ErrorSavingJobs, "An error occurred while saving the jobs.");
            return BadRequest(errResp);
        }
    }

    [HttpPost]
    [Route("StartTask")]
    [Authorize]
    public async Task<ActionResult<ResponseModel>> StartTask([FromQuery] int taskId)
    {
        _auditLogger.Info($"User '{AppUser?.Username}' manually started task {taskId}.");

        bool result = await _jobEngine.StartTaskAsync(taskId, HttpContext.RequestAborted);

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
    public ActionResult<ResponseModel> ReloadJobsConfig()
    {
        _auditLogger.Info($"User '{AppUser?.Username}' reloaded the job configuration.");

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
    public ActionResult<ResponseModel<List<LogInfoListItem>>> FolderLogs([FromQuery] int folderId)
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
    public ActionResult<ResponseModel<FolderInfo>> FolderInfo([FromQuery] int folderId)
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
    public ActionResult<ResponseModel<string>> LogContent([FromQuery] int folderId, [FromQuery] string logFileName)
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
