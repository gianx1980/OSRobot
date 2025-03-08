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

namespace OSRobot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotController : AppControllerBase
{
    private readonly IJobEngine _jobEngine;
    private readonly AppSettings _appSettings;

    public RobotController(IJobEngine jobEngine, IOptions<AppSettings> appSettings)
    {
        _jobEngine = jobEngine;
        _appSettings = appSettings.Value;
    }

    [HttpGet]
    [Route("Objects")]
    [Authorize]
    public IActionResult Objects()
    {
        List<PluginListItem> pluginList = _jobEngine.GetPlugins().Select(t => new PluginListItem(t.Id, t.Title, t.PluginType.ToString().ToLowerInvariant(), t.GetPluginDefaultConfig(), t.SupportedOSPlatforms)).ToList();
        MainResponse<List<PluginListItem>> mainResponse = new MainResponse<List<PluginListItem>>(MainResponse<List<PluginListItem>>.ResponseOk, null, pluginList);
        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("DynDataSamples")]
    [Authorize]
    public IActionResult DynDataSamples(string pluginId)
    {
        IPlugin? plugin = _jobEngine.GetPlugin(pluginId);
        if (plugin == null)
            return NotFound();

        List<PluginDynDataSampleListItem> dynDataSamplesList = plugin.SampleDynamicData.Select(t => new PluginDynDataSampleListItem(t.Description, t.Example, t.InternalName)).ToList();

        MainResponse<List<PluginDynDataSampleListItem>> mainResponse = new MainResponse<List<PluginDynDataSampleListItem>>(MainResponse<List<PluginDynDataSampleListItem>>.ResponseOk, null, dynDataSamplesList);
        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("WorkspaceJobs")]
    [Authorize]
    public IActionResult WorkspaceJobs()
    {
        // Check the existence of data
        if (!System.IO.File.Exists(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json")))
        {
            MainResponse<object> responseNotExists = new MainResponse<object>(MainResponse<object>.ResponseOk, null, null);
            return Ok(responseNotExists);
        }

        string configuration = System.IO.File.ReadAllText(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json"));
        if (string.IsNullOrEmpty(configuration))
        {
            MainResponse<object> responseEmptyFile = new MainResponse<object>(MainResponse<object>.ResponseOk, null, null);
            return Ok(responseEmptyFile);
        }

        // Deserialize the json configuration and return to client
        var configJson = JsonSerializer.Deserialize<object>(configuration);        
        MainResponse<object> mainResponse = new MainResponse<object>(MainResponse<object>.ResponseOk, null, configJson);
        return Ok(mainResponse);
    }

    [HttpPost]
    [Route("WorkspaceJobs")]
    [Authorize]
    public IActionResult WorkspaceJobs([FromBody] object requestBody)
    {
        string? workspaceJobs = requestBody.ToString();

        try
        {
            System.IO.File.WriteAllText(Path.Combine(_appSettings.JobEngineConfig.DataPath, "jobs.json"), workspaceJobs);

            MainResponse<object> mainResponse = new MainResponse<object>(MainResponse<object>.ResponseOk, null, null);
            return Ok(mainResponse);
        }
        catch
        {
            MainResponse<object> mainResponse = new MainResponse<object>(MainResponse<object>.ResponseGenericError, "An error occurred while saving jobs.", null);
            return BadRequest(mainResponse);
        }
    }

    [HttpPost]
    [Route("StartTask")]
    [Authorize]
    public IActionResult StartTask([FromQuery] int taskId)
    {
        bool result = _jobEngine.StartTask(taskId);

        MainResponse<object> mainResponse;
        int responseCode;
        string? responseMessage = null;

        if (result)
        {
            responseCode = MainResponse<object>.ResponseOk;
        }
        else
        {
            responseCode = MainResponse<object>.ResponseGenericError;
            responseMessage = "An error occurred while starting the task";
        }
        mainResponse = new MainResponse<object>(responseCode, responseMessage, null);

        return Ok(mainResponse);
    }

    [HttpPost]
    [Route("ReloadJobsConfig")]
    [Authorize]
    public IActionResult ReloadJobsConfig()
    {
        ReloadJobsReturnValues result = _jobEngine.ReloadJobs();

        MainResponse<object> mainResponse;
        int responseCode;
        string? responseMessage = null;

        if (result == ReloadJobsReturnValues.Ok)
        {
            responseCode = MainResponse<object>.ResponseOk;
        }
        else if (result == ReloadJobsReturnValues.CannotReloadWhileRunningTask)
        {
            responseCode = MainResponse<object>.CannotReloadWhileRunningTasks;
            responseMessage = "Cannot reload jobs because there are running jobs, plese retry later.";
        }
        else
        {
            responseCode = MainResponse<object>.ResponseGenericError;
        }
        mainResponse = new MainResponse<object>(responseCode, responseMessage, null);

        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("FolderLogs")]
    [Authorize]
    public IActionResult FolderLogs([FromQuery] int folderId)
    {
        List<LogInfoItem> folderLogs = _jobEngine.GetFolderLogs(folderId);

        List<LogInfoListItem> logInfoList =
        [
            ..folderLogs.Select(folderLog => new LogInfoListItem(folderLog.EventId,
                                                                   folderLog.ExecDateTime,
                                                                   folderLog.FileName
                                                                   ))
        ];

        MainResponse<List<LogInfoListItem>> mainResponse = new(MainResponse<object>.ResponseOk, null, logInfoList);

        return Ok(mainResponse);
    }


}
