// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";

// Variables and secrets, used in jobs as {var['Name']} and {secret['Name']}. The server never
// returns a secret's value: a listed secret has value null.
export default class Variables extends ServiceBase {
  async list() {
    let result = null;

    try {
      const response = await this._get({ url: "/Variables/List" });
      const item = response.data.responseObject;

      result = new ServiceResponse(
        response.data.responseCode,
        response.data.responseCode === 0 ? item : null
      );
    } catch (e) {
      this._throwException(e);
    }

    return result;
  }

  // variable: { name, value, isSecret, description }. For an existing secret, an empty value keeps
  // the current one.
  async save(variable) {
    let result = null;

    try {
      const response = await this._post({
        url: "/Variables/Save",
        data: variable,
      });

      result = new ServiceResponse(response.data.responseCode, null);
    } catch (e) {
      result = this._errorResponse(e);
    }

    return result;
  }

  async delete(name) {
    let result = null;

    try {
      const response = await this._post({
        url: "/Variables/Delete",
        params: { name: name },
      });

      result = new ServiceResponse(response.data.responseCode, null);
    } catch (e) {
      result = this._errorResponse(e);
    }

    return result;
  }

  // A refused save or delete comes back as 400 with a message meant for the user.
  _errorResponse(e) {
    const data = e.response?.data;
    if (e.response?.status === 400 && data?.responseCode) {
      return new ServiceResponse(data.responseCode, null, data.responseMessage);
    }

    this._throwException(e);
  }
}

Variables.InvalidVariable = -300;
Variables.VariableNotFound = -301;

// The placeholder that inserts a variable or a secret in a job.
export function variablePlaceholder(name, isSecret) {
  return isSecret ? `{secret['${name}']}` : `{var['${name}']}`;
}
