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

  async sqlServerTestConnection(connectionInfo) {
    let result = null;

    try {
      const response = await this._get({
        url: "/ClientUtils/SqlServerConnectionTest",
        params: connectionInfo,
      });

      result = new ServiceResponse(response.data.responseCode, null);
    } catch (e) {
      this._throwException(e);
    }

    return result;
  }

  async sqlServerGetDatabases(connectionInfo) {
    let result = null;

    try {
      const response = await this._get({
        url: "/ClientUtils/SqlServerDatabases",
        params: connectionInfo,
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
}
