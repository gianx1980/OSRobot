// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO;

public class MainResponse<T>(int responseCode, string? responseMessage, T? responseObject)
{
    public const int ResponseOk = 0;
    public const int ResponseAccessDenied = -1;
    public const int ConfirmPasswordMismatch = -2;
    public const int CannotReloadWhileRunningTasks = -3;
    public const int ResponseGenericError = int.MinValue;

    public int ResponseCode { get; set; } = responseCode;
    public string? ResponseMessage { get; set; } = responseMessage;
    public T? ResponseObject { get; set; } = responseObject;
}
