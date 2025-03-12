import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import { api } from "boot/axios";

export default class Robot extends ServiceBase {
  async getObjects() {
    let result = null;

    try {
      const response = await this._get({ url: "/Robot/Objects" });
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

  async getWorkspaceJobs() {
    let result = null;

    try {
      const response = await this._get({ url: "/Robot/WorkspaceJobs" });
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

  async getDynDataSamples(pluginId) {
    let result = null;

    try {
      const response = await this._get({
        url: "/Robot/DynDataSamples",
        params: { pluginId: pluginId },
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

  async getFolderLogs(folderId) {
    let result = null;

    try {
      const response = await this._get({
        url: "/Robot/FolderLogs",
        params: { folderId: folderId },
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

  async getFolderInfo(folderId) {
    let result = null;

    try {
      const response = await this._get({
        url: "/Robot/FolderInfo",
        params: { folderId: folderId },
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

  async getLogContent(folderId, logFileName) {
    let result = null;

    try {
      const response = await this._get({
        url: "/Robot/LogContent",
        params: { folderId: folderId, logFileName: logFileName },
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
