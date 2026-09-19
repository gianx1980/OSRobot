import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";

export class User {
  constructor(username, token, refreshToken, mustChangePassword = false) {
    this._username = username;
    this._token = token;
    this._refreshToken = refreshToken;
    this._mustChangePassword = mustChangePassword;
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

  get mustChangePassword() {
    return this._mustChangePassword;
  }
  set mustChangePassword(value) {
    this._mustChangePassword = value;
  }
}

export class Account extends ServiceBase {
  async login(username, password) {
    try {
      const response = await this._post({
        url: "/Account/Login",
        data: {
          username: username,
          password: password,
        },
      });

      response.data.responseObject =
        response.data.responseCode === Account.Ok
          ? new User(
              username,
              response.data.responseObject.token,
              response.data.responseObject.refreshToken,
              response.data.responseObject.mustChangePassword
            )
          : null;

      return response;
    } catch (e) {
      // The server responds with a non-2xx status for wrong credentials (400) or a locked-out
      // account (423), so axios rejects rather than resolving. Normalize back into the same
      // { data: { responseCode, ... } } shape a successful call has, so callers can switch on
      // responseCode the same way regardless of which HTTP status carried it.
      return {
        data: {
          responseCode: e.response?.data?.responseCode ?? Account.ErrorGeneric,
          responseMessage: e.response?.data?.responseMessage ?? null,
          responseObject: null,
        },
      };
    }
  }

  async changePassword(userChangePasswordRequest) {
    try {
      const response = await this._post({
        url: "/Account/ChangePassword",
        data: userChangePasswordRequest,
      });
      // response is the raw axios response - the server's payload is in response.data, not on
      // response directly. responseObject carries a fresh { username, token, refreshToken,
      // mustChangePassword } on success (see AccountController.ChangePassword).
      return new ServiceResponse(
        response.data.responseCode,
        response.data.responseObject
      );
    } catch (e) {
      return new ServiceResponse(
        e.response?.data?.responseCode ?? Account.ErrorGeneric,
        null
      );
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
Account.AccountLockedOut = -11;
Account.MustChangePassword = -12;
Account.Ok = 0;
Account.Alive = 10;
Account.ErrorGeneric = -32767;
