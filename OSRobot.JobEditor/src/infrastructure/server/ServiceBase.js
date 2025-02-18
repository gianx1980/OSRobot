import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import UnauthorizedException from "src/infrastructure/server/UnauthorizedException.js";
import Utility from "src/infrastructure/Utility.js";
import { api } from "boot/axios";
import { useAppStore } from "src/stores/appStore.js";

export default class ServiceBase {
  constructor(token, refreshToken) {
    this._token = token;
    this._refreshToken = refreshToken;
  }

  get token() {
    return this._token;
  }
  set token(value) {
    this._token = value;
  }

  get refreshToken() {
    return this._refreshToken;
  }
  set refreshToken(value) {
    this._refreshToken = value;
  }

  async _renewToken() {
    const resultRefresh = await this._doRefreshToken();

    if (resultRefresh.data.responseCode === ServiceBase.Ok) {
      const appStore = useAppStore();
      const user = appStore.getLoggedUser();
      user.token = resultRefresh.data.responseObject.token;
      appStore.setLoggedUser(user);
      this._token = resultRefresh.data.responseObject.token;
    } else throw new ServiceException(ServiceBase.NotAuthorized, null);
  }

  async _post(config) {
    config.method = "post";

    if (this._token) {
      if (Utility.jwtShouldRenew(this._token)) {
        await this._renewToken();
      }

      config.headers = { Authorization: `Bearer ${this._token}` };
    }

    const response = await api.request(config);
    return response;
  }

  async _get(config) {
    config.method = "get";

    if (this._token) {
      if (Utility.jwtShouldRenew(this._token)) {
        await this._renewToken();
      }

      config.headers = { Authorization: `Bearer ${this._token}` };
    }

    const response = api.request(config);
    return await response;
  }

  async _doRefreshToken(userRefreshTokenRequest) {
    if (!userRefreshTokenRequest) {
      userRefreshTokenRequest = {
        token: this._token,
        refreshToken: this._refreshToken,
      };
    }

    return await api.request({
      method: "post",
      url: "/Account/RefreshToken",
      data: userRefreshTokenRequest,
    });
  }

  _throwException(e) {
    if (e.response.status === 401) throw new UnauthorizedException(e);
    else throw new ServiceException(ServiceResponse.ErrorGeneric, e);
  }
}

ServiceBase.Ok = 0;
ServiceBase.NotAuthorized = -32766;
ServiceBase.ErrorGeneric = -32767;
