// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

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
    public async Task<ActionResult> TrackError(TrackErrorRequest trackErrorMessageRequest)
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
