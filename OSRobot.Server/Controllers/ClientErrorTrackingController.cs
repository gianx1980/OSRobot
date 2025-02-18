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
using OSRobot.Server.Models.DTO.Diagnostics;

namespace OSRobot.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientErrorTrackingController : AppControllerBase
{
    //private readonly IRepository _repository;

    public ClientErrorTrackingController(/*IRepository repository*/)
    {
//        _repository = repository;
    }

    [HttpPost]
    [Authorize]
    [Route("TrackError")]
    public async Task<IActionResult> TrackError(TrackErrorRequest trackErrorMessageRequest)
    {
        await Task.FromResult(0);
        /*
        await _repository.ClientErrorSave(trackErrorMessageRequest.ErrorMessage);

        MainResponse<object?> mainResponse = new MainResponse<object?>(MainResponse<object?>.ResponseOk, null, null);
        return Ok(mainResponse);
        */
        return Ok();
    }
}
