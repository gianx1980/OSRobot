// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OSRobot.Server.Configuration;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Infrastructure.Security;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.Config;
using System.Reflection;

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
        [AllowWhenPasswordChangeRequired]
        public ActionResult<ResponseModel<ConfigResponse>> GetConfig()
        {
            ConfigResponse configResponse = new(_appSettings.JWT.RequestNewTokenIfMinutesLeft, 
                                                    _appSettings.ClientSettings.AppTitle, 
                                                    _appSettings.ClientSettings.StaticFilesUrl,
                                                    _appSettings.ClientSettings.HeartBeatInterval, 
                                                    _appSettings.ClientSettings.NotificationServerSentEventsEnabled, 
                                                    _appSettings.ClientSettings.NotificationPollingInterval,
                                                    Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty);

            ResponseModel<ConfigResponse> response = new(ResponseCode.ResponseOk, null, configResponse);

            return Ok(response);
        }
    }
}
