// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

export default class ServiceResponse {
  // responseMessage: the server's explanation of an error, when it gives one meant for the user.
  constructor(responseCode, responseObject, responseMessage = null) {
    this._responseCode = responseCode;
    this._responseObject = responseObject;
    this._responseMessage = responseMessage;
  }

  get responseCode() {
    return this._responseCode;
  }
  set responseCode(value) {
    this._responseCode = value;
  }

  get responseObject() {
    return this._responseObject;
  }
  set responseObject(value) {
    this._responseObject = value;
  }

  get responseMessage() {
    return this._responseMessage;
  }
  set responseMessage(value) {
    this._responseMessage = value;
  }
}
