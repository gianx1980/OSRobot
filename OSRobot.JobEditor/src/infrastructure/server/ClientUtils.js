// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import { api } from "boot/axios";

export default class ClientUtils extends ServiceBase {
  async getOSInfo() {
    let result = null;

    try {
      const response = await this._get({ url: "/ClientUtils/OSInfo" });
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

  async getDriveList() {
    let result = null;

    try {
      const response = await this._get({ url: "/ClientUtils/Drives" });
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

  async getFolderList(path) {
    let result = null;

    try {
      const response = await this._get({
        url: "/ClientUtils/Folders",
        params: { path: path },
      });
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

  async getFileList(path) {
    let result = null;

    try {
      const response = await this._get({
        url: "/ClientUtils/Files",
        params: { path: path },
      });
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

  // The connection details travel in the body (POST): they include a password, which must not end
  // up in a URL. Placeholders such as {secret['Name']} are resolved by the server.
  async sqlServerTestConnection(connectionInfo) {
    let result = null;

    try {
      const response = await this._post({
        url: "/ClientUtils/SqlServerConnectionTest",
        data: connectionInfo,
      });

      result = new ServiceResponse(response.data.responseCode, null);
    } catch (e) {
      result = this._invalidVariableResponse(e);
    }

    return result;
  }

  async sqlServerGetDatabases(connectionInfo) {
    let result = null;

    try {
      const response = await this._post({
        url: "/ClientUtils/SqlServerDatabases",
        data: connectionInfo,
      });
      const item = response.data.responseObject;

      result = new ServiceResponse(
        response.data.responseCode,
        response.data.responseCode === 0 ? item : null
      );
    } catch (e) {
      result = this._invalidVariableResponse(e);
    }

    return result;
  }

  // A placeholder naming an undefined variable or secret is reported with a message for the user;
  // any other error is thrown as before.
  _invalidVariableResponse(e) {
    const data = e.response?.data;
    if (data?.responseCode === ClientUtils.InvalidVariable) {
      return new ServiceResponse(data.responseCode, null, data.responseMessage);
    }

    this._throwException(e);
  }
}

ClientUtils.InvalidVariable = -300;
