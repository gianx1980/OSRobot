// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using OSRobot.Server.Models.DTO;

namespace OSRobot.Server.Infrastructure.Security;

/// <summary>Marks an action as reachable even while the authenticated user's token carries
/// MustChangePassword=true. Only AccountController.ChangePassword should carry this - the
/// unauthenticated Login/RefreshToken endpoints don't need it, since this filter only ever
/// blocks requests that are already authenticated.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class AllowWhenPasswordChangeRequiredAttribute : Attribute
{
}

/// <summary>
/// Server-side enforcement of "must change password before doing anything else" - registered
/// globally (see Program.cs) rather than left to the frontend to honor, since the whole point is
/// that a stale default/compromised credential shouldn't keep working just because the UI
/// happens not to nag about it. See SECURITY.md.
/// </summary>
public class MustChangePasswordFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Unauthenticated requests are already rejected by [Authorize] where required; nothing
        // to enforce here for them.
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (context.ActionDescriptor is ControllerActionDescriptor controllerAction
            && controllerAction.MethodInfo.GetCustomAttributes(typeof(AllowWhenPasswordChangeRequiredAttribute), inherit: true).Length > 0)
            return Task.CompletedTask;

        bool mustChangePassword = context.HttpContext.User.FindFirst(OSRobotClaimTypes.MustChangePassword)?.Value == "true";
        if (mustChangePassword)
        {
            ResponseModel errorResp = new(ResponseCode.MustChangePassword, "Password change required before continuing.");
            context.Result = new ObjectResult(errorResp) { StatusCode = StatusCodes.Status403Forbidden };
        }

        return Task.CompletedTask;
    }
}
