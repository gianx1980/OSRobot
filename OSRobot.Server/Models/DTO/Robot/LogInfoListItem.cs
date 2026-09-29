// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Models.DTO.Robot;

public class LogInfoListItem(int eventId, DateTime execDateTime, string fileName)
{
    public int EventId { get; set; } = eventId;
    public DateTime ExecDateTime { get; set; } = execDateTime;
    public string FileName { get; set; } = fileName;
}
