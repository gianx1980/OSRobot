import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";

export class User {
  constructor(username, token, refreshToken) {
    this._username = username;
    this._token = token;
    this._refreshToken = refreshToken;
  }

  get username() {
    return this._username;
  }
  set username(value) {
    this._username = value;
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
}

export class Account extends ServiceBase {
  async login(username, password) {
    const response = await this._post({
      url: "/Account/Login",
      data: {
        username: username,
        password: password,
      },
    });

    response.data.responseObject =
      response.data.responseCode === 0
        ? new User(
            username,
            response.data.responseObject.token,
            response.data.responseObject.refreshToken
          )
        : null;

    return response;
  }

  async changePassword(userChangePasswordRequest) {
    try {
      const response = await this._post({
        url: "/Account/ChangePassword",
        data: userChangePasswordRequest,
      });
      return new ServiceResponse(response.responseCode, null);
    } catch (e) {
      return new ServiceResponse(e.responseCode, e);
    }
  }

  async refreshToken(userRefreshTokenRequest) {
    try {
      return await this._doRefreshToken(userRefreshTokenRequest);
    } catch (e) {
      return new ServiceResponse(e.responseCode, e);
    }
  }

  async heartBeat() {
    try {
      await this._post({ url: "/Account/HeartBeat" });

      return new ServiceResponse(Account.Alive);
    } catch (e) {
      return new ServiceResponse(e.responseCode, e);
    }
  }
}

Account.WrongCredentials = -10;
Account.Ok = 0;
Account.Alive = 10;
