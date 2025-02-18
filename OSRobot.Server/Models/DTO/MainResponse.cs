﻿/*======================================================================================
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
namespace OSRobot.Server.Models.DTO;

public class MainResponse<T>
{
    public const int ResponseOk = 0;
    public const int ResponseAccessDenied = -1;
    public const int ConfirmPasswordMismatch = -2;
    public const int CannotReloadWhileRunningTasks = -3;
    public const int ResponseGenericError = int.MinValue;

    public MainResponse(int responseCode, string? responseMessage, T? responseObject)
    {
        ResponseCode = responseCode;
        ResponseMessage = responseMessage;
        ResponseObject = responseObject;
    }

    public int ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public T? ResponseObject { get; set; }
}
