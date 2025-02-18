import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import { api } from "boot/axios";

export default class Workspace extends ServiceBase {
  async list() {
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

  async save(workspaceData) {
    let result = null;

    try {
      const response = await this._post({
        url: "/Robot/WorkspaceJobs",
        data: workspaceData,
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

  async startTask(taskID) {
    let result = null;

    try {
      const response = await this._post({
        url: "/Robot/StartTask",
        params: { taskID: taskID },
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

  async reloadJobsConfig() {
    let result = null;

    try {
      const response = await this._post({
        url: "/Robot/ReloadJobsConfig",
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
