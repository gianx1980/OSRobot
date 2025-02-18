import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import { api } from "boot/axios";

export class ServerConfig {
  constructor(
    requestNewTokenIfMinutesLeft,
    appTitle,
    staticFilesUrl,
    heartBeatInterval,
    notificationServerSentEventsEnabled,
    notificationPollingInterval
  ) {
    this._requestNewTokenIfMinutesLeft = requestNewTokenIfMinutesLeft;
    this._appTitle = appTitle;
    this._staticFilesUrl = staticFilesUrl;
    this._heartBeatInterval = heartBeatInterval;
    this._notificationServerSentEventsEnabled =
      notificationServerSentEventsEnabled;
    this._notificationPollingInterval = notificationPollingInterval;
  }

  get requestNewTokenIfMinutesLeft() {
    return this._requestNewTokenIfMinutesLeft;
  }
  set requestNewTokenIfMinutesLeft(value) {
    this._requestNewTokenIfMinutesLeft = value;
  }

  get appTitle() {
    return this._appTitle;
  }
  set appTitle(value) {
    this._appTitle = value;
  }

  get staticFilesUrl() {
    return this._staticFilesUrl;
  }
  set staticFilesUrl(value) {
    this._staticFilesUrl = value;
  }

  get heartBeatInterval() {
    return this._heartBeatInterval;
  }
  set heartBeatInterval(value) {
    this._heartBeatInterval = value;
  }

  get notificationServerSentEventsEnabled() {
    return this._notificationServerSentEventsEnabled;
  }
  set notificationServerSentEventsEnabled(value) {
    this._notificationServerSentEventsEnabled = value;
  }

  get notificationPollingInterval() {
    return this._notificationPollingInterval;
  }
  set notificationPollingInterval(value) {
    this._notificationPollingInterval = value;
  }
}

export class Config extends ServiceBase {
  async getConfig() {
    let result = null;

    try {
      const response = await this._post({ url: "/Config/GetConfig" });
      const item = response.data.responseObject;

      result = new ServiceResponse(
        response.data.responseCode,
        response.data.responseCode === 0
          ? new ServerConfig(
              item.requestNewTokenIfMinutesLeft,
              item.appTitle,
              item.staticFilesUrl,
              item.heartBeatInterval,
              item.notificationServerSentEventsEnabled,
              item.notificationPollingInterval
            )
          : null
      );
    } catch (e) {
      this._throwException(e);
    }

    return { data: result };
  }
}
