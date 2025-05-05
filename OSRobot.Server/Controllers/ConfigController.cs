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
using OSRobot.Server.Configuration;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.Config;

namespace OSRobot.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController(IOptions<AppSettings> appSettings) : AppControllerBase
    {
        private readonly AppSettings _appSettings = appSettings.Value;

        [HttpPost]
        [Route("GetConfig")]
        [Authorize]
        public IActionResult GetConfig()
        {
            ConfigResponse configResponse = new(_appSettings.JWT.RequestNewTokenIfMinutesLeft, 
                                                    _appSettings.ClientSettings.AppTitle, 
                                                    _appSettings.ClientSettings.StaticFilesUrl,
                                                    _appSettings.ClientSettings.HeartBeatInterval, 
                                                    _appSettings.ClientSettings.NotificationServerSentEventsEnabled, 
                                                    _appSettings.ClientSettings.NotificationPollingInterval);

            ResponseModel<ConfigResponse> response = new(ResponseCode.ResponseOk, null, configResponse);

            return Ok(response);
        }
    }
}
