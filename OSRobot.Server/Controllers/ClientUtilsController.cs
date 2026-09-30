// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.ClientUtils;
using OSRobot.Server.Plugins.Infrastructure.Utilities.SqlClient;

namespace OSRobot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientUtilsController : AppControllerBase
{
    [HttpGet]
    [Route("OSInfo")]
    [Authorize]
    public ActionResult<ResponseModel<OSInfoResponse>> OSInfo()
    {
        ResponseModel<OSInfoResponse> response = new(ResponseCode.ResponseOk, null, new OSInfoResponse());
        return Ok(response);
    }


    [HttpGet]
    [Route("Drives")]
    [Authorize]
    public ActionResult<ResponseModel<List<DriveListItem>>> Drives()
    {
        DriveInfo[] systemDrives = DriveInfo.GetDrives();
        
        List<DriveListItem> responseList = systemDrives.Select(drive => new DriveListItem(drive.Name)).ToList();
        ResponseModel<List<DriveListItem>> response = new(ResponseCode.ResponseOk, null, responseList);

        return Ok(response);
    }

    [HttpGet]
    [Route("Folders")]
    [Authorize]
    public ActionResult<ResponseModel<List<string>>> Folders(string path)
    {
        List<string> folders = [];

        try
        {
            if (path == @"\" || path == "/")
            {
                DriveInfo[] systemDrives = DriveInfo.GetDrives();
                folders.AddRange(systemDrives.Select(drive => drive.Name));
            }
            else
            {
                string[] tempFolders = Directory.GetDirectories(path, string.Empty, SearchOption.TopDirectoryOnly);
                folders.AddRange(tempFolders.Select(dir => new DirectoryInfo(dir).Name));
            }
        }
        catch (UnauthorizedAccessException)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseAccessDenied, "Access denied to the path");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }
        catch
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "Generic error occurred");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        ResponseModel<List<string>> response = new(ResponseCode.ResponseOk, null, folders);
        return Ok(response);
    }

    [HttpGet]
    [Route("Files")]
    [Authorize]
    public ActionResult<ResponseModel<List<string>>> Files(string path)
    {
        List<string> fileList = [];
     
        try
        {
            fileList.AddRange([.. Directory
                                    .GetFiles(path)
                                    .Select(f => Path.GetFileName(f))]);
        }
        catch (UnauthorizedAccessException)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseAccessDenied, "Access denied to the path");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }
        catch
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "Generic error occurred");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        ResponseModel<List<string>> response = new(ResponseCode.ResponseOk, null, fileList);
        return Ok(response);
    }

    [HttpGet]
    [Route("SqlServerConnectionTest")]
    [Authorize]
    public ActionResult<ResponseModel> SqlServerConnectionTest([FromQuery] SqlServerConnectionRequest connectionInfo)
    {
        if (connectionInfo.Server == null
            || connectionInfo.Username == null
            || connectionInfo.Password == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseWrongCredentials, "Missing one or more parameters");
            return BadRequest(errorResp);
        }

        bool testResult = SqlServer.TestConnection(connectionInfo.Server, connectionInfo.Database, connectionInfo.Username, connectionInfo.Password, connectionInfo.ConnectionStringOptions);
        if (!testResult)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "Connection error");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }
        
        ResponseModel response = new(ResponseCode.ResponseOk, "Connection established");
        return Ok(response);
    }

    [HttpGet]
    [Route("SqlServerDatabases")]
    [Authorize]
    public ActionResult<ResponseModel<List<DatabaseListItem>>> SqlServerDatabases([FromQuery] SqlServerConnectionRequest connectionInfo)
    {
        if (connectionInfo.Server == null
            || connectionInfo.Username == null
            || connectionInfo.Password == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseWrongCredentials, "Missing one or more parameters");
            return BadRequest(errorResp);
        }

        List<SqlServerDatabaseListItem>? sqlDatabaseList = SqlServer.GetDatabaseList(connectionInfo.Server, connectionInfo.Username, connectionInfo.Password, connectionInfo.ConnectionStringOptions);
        if (sqlDatabaseList == null)
        {
            ResponseModel errorResp = new(ResponseCode.ResponseGenericError, "Connection error");
            return StatusCode(StatusCodes.Status500InternalServerError, errorResp);
        }

        List<DatabaseListItem>? databaseList = [.. sqlDatabaseList.Select(t => new DatabaseListItem(t.Id, t.Name))];
        ResponseModel<List<DatabaseListItem>> response = new(ResponseCode.ResponseOk, null, databaseList);

        return Ok(response);
    }
}
