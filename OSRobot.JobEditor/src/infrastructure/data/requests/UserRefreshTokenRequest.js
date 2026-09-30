// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

export default class UserRefreshTokenRequest {
  constructor(token, refreshToken) {
    this.token = token;
    this.refreshToken = refreshToken;
  }
}
