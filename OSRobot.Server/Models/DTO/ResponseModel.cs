// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO;

public enum ResponseCode : int
{
    ResponseOk = 0,
    ResponseAccessDenied = -1,
    ConfirmPasswordMismatch = -2,
    CannotReloadWhileRunningTasks = -3,
    ResponseWrongCredentials = -10,
    AccountLockedOut = -11,
    MustChangePassword = -12,
    ErrorLoadingJobs = -100,
    ErrorSavingJobs = -101,
    InvalidJobsConfiguration = -102,
    CannotStartTask = -200,
    ResponseGenericError = int.MinValue
}

public class ResponseModel(ResponseCode responseCode, string? responseMessage)
{
    public ResponseCode ResponseCode { get; set; } = responseCode;
    public string? ResponseMessage { get; set; } = responseMessage;
}

public class ResponseModel<T>(ResponseCode responseCode, string? responseMessage, T? responseObject)
{
    public ResponseCode ResponseCode { get; set; } = responseCode;
    public string? ResponseMessage { get; set; } = responseMessage;
    public T? ResponseObject { get; set; } = responseObject;
}


