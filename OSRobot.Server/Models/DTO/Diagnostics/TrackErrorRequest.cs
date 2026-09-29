// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Diagnostics;

public class TrackErrorRequest(string errorMessage)
{
    public string ErrorMessage { get; set; } = errorMessage;
}
