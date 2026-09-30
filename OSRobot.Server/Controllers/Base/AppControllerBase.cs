// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using OSRobot.Server.Infrastructure.Security;
using System.Security.Claims;

namespace OSRobot.Server.Controllers.Base;

public class AppControllerBase : ControllerBase
{
    private AppTokenUser? _appUser;
    protected AppTokenUser? AppUser { get => _appUser ??= new AppTokenUser((ClaimsIdentity)User.Identity!); }
}