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
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.ClientUtils;
using OSRobot.Server.Plugins.Infrastructure.Utilities;

namespace OSRobot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientUtilsController : AppControllerBase
{
    [HttpGet]
    [Route("OSInfo")]
    [Authorize]
    public IActionResult OSInfo()
    {
        MainResponse<OSInfoResponse> mainResponse = new(MainResponse<OSInfoResponse>.ResponseOk, null, new OSInfoResponse());

        return Ok(mainResponse);
    }


    [HttpGet]
    [Route("Drives")]
    [Authorize]
    public IActionResult Drives()
    {
        DriveInfo[] systemDrives = DriveInfo.GetDrives();

        List<DriveListItem> responseList = new List<DriveListItem>();
        foreach (DriveInfo drive in systemDrives)
        {
            responseList.Add(new DriveListItem(drive.Name));
        }

        MainResponse<List<DriveListItem>> mainResponse = new(MainResponse<object>.ResponseOk, null, responseList);

        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("Folders")]
    [Authorize]
    public IActionResult Folders(string path)
    {
        MainResponse<List<string>> mainResponse;
        List<string> folders = new List<string>();
        int responseCode = MainResponse<string>.ResponseOk;
        string? responseMessage = null;

        try
        {
            if (path == @"\" || path == "/")
            {
                DriveInfo[] systemDrives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in systemDrives)
                {
                    folders.Add(drive.Name);
                }
            }
            else
            {
                string[] tempFolders = Directory.GetDirectories(path, string.Empty, SearchOption.TopDirectoryOnly);
                folders.AddRange(tempFolders.Select(t => new DirectoryInfo(t).Name));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // TODO: log exception
            responseCode = MainResponse<string>.ResponseAccessDenied;
            responseMessage = "Access denied";
        }
        catch
        {
            responseCode = MainResponse<string>.ResponseGenericError;

            // TODO: log exception
        }

        mainResponse = new(responseCode, responseMessage, folders);

        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("Files")]
    [Authorize]
    public IActionResult Files(string path)
    {
        MainResponse<List<string>> mainResponse;
        List<string> fileList = new List<string>();
        int responseCode = MainResponse<string>.ResponseOk;
        string? responseMessage = null;

        try
        {
            fileList.AddRange(Directory
                                .GetFiles(path)
                                .Select(f => Path.GetFileName(f))
                                .ToList());
        }
        catch (UnauthorizedAccessException)
        {
            // TODO: log exception
            responseCode = MainResponse<string>.ResponseAccessDenied;
            responseMessage = "Access denied";
        }
        catch
        {
            responseCode = MainResponse<string>.ResponseGenericError;

            // TODO: log exception
        }

        mainResponse = new(responseCode, responseMessage, fileList);

        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("SqlServerConnectionTest")]
    [Authorize]
    public IActionResult SqlServerConnectionTest([FromQuery] SqlServerConnectionRequest connectionInfo)
    {
        if (connectionInfo.Server == null
            || connectionInfo.Username == null
            || connectionInfo.Password == null)
            return BadRequest();

        bool testResult = SqlServer.TestConnection(connectionInfo.Server, connectionInfo.Database, connectionInfo.Username, connectionInfo.Password, connectionInfo.ConnectionStringOptions);
        int responseCode = testResult ? MainResponse<object>.ResponseOk : MainResponse<object>.ResponseGenericError;
        string responseMessage = testResult ? "Connection established" : "Connection error";

        MainResponse<object> mainResponse = new MainResponse<object>(responseCode, responseMessage, null);

        return Ok(mainResponse);
    }

    [HttpGet]
    [Route("SqlServerDatabases")]
    [Authorize]
    public IActionResult SqlServerDatabases([FromQuery] SqlServerConnectionRequest connectionInfo)
    {
        if (connectionInfo.Server == null
            || connectionInfo.Username == null
            || connectionInfo.Password == null)
            return BadRequest();

        List<SqlServerDatabaseListItem>? sqlDatabaseList = SqlServer.GetDatabaseList(connectionInfo.Server, connectionInfo.Username, connectionInfo.Password, connectionInfo.ConnectionStringOptions);
        int responseCode = sqlDatabaseList != null ? MainResponse<object>.ResponseOk : MainResponse<object>.ResponseGenericError;
        List<DatabaseListItem>? databaseList = sqlDatabaseList != null ? sqlDatabaseList.Select(t => new DatabaseListItem(t.Id, t.Name)).ToList() : null;

        MainResponse<List<DatabaseListItem>> mainResponse = new MainResponse<List<DatabaseListItem>>(responseCode, null, databaseList);

        return Ok(mainResponse);
    }
}
