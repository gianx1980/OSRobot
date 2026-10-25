// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRobot.Server.Controllers.Base;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Infrastructure.Variables;
using OSRobot.Server.Models.DTO;
using OSRobot.Server.Models.DTO.Variables;

namespace OSRobot.Server.Controllers;

/// <summary>
/// Variables and secrets used by the {var['Name']} and {secret['Name']} placeholders. A secret's value
/// can be set but is never returned. Changes are audited by name only, never by value.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VariablesController(IVariableStore variableStore, IAuditLogger auditLogger) : AppControllerBase
{
    private readonly IVariableStore _variableStore = variableStore;
    private readonly IAuditLogger _auditLogger = auditLogger;

    [HttpGet]
    [Route("List")]
    [Authorize]
    public ActionResult<ResponseModel<List<VariableListItem>>> List()
    {
        ResponseModel<List<VariableListItem>> response = new(ResponseCode.ResponseOk, null, _variableStore.List());
        return Ok(response);
    }

    [HttpPost]
    [Route("Save")]
    [Authorize]
    public ActionResult<ResponseModel> Save([FromBody] VariableSaveRequest request)
    {
        bool created;
        try
        {
            created = _variableStore.Save(request, AppUser?.Username);
        }
        catch (VariableValidationException ex)
        {
            ResponseModel errorResp = new(ResponseCode.InvalidVariable, ex.Message);
            return BadRequest(errorResp);
        }

        string what = request.IsSecret ? "secret" : "variable";
        _auditLogger.Info($"User '{AppUser?.Username}' {(created ? "created" : "updated")} the {what} '{request.Name.Trim()}'.");

        ResponseModel response = new(ResponseCode.ResponseOk, null);
        return Ok(response);
    }

    [HttpPost]
    [Route("Delete")]
    [Authorize]
    public ActionResult<ResponseModel> Delete([FromQuery] string name)
    {
        if (!_variableStore.Delete(name))
        {
            ResponseModel errorResp = new(ResponseCode.VariableNotFound, $"There is no variable or secret named '{name}'.");
            return BadRequest(errorResp);
        }

        _auditLogger.Info($"User '{AppUser?.Username}' deleted the variable or secret '{name}'.");

        ResponseModel response = new(ResponseCode.ResponseOk, null);
        return Ok(response);
    }
}
