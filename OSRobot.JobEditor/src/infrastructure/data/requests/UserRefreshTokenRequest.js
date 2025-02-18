export default class UserRefreshTokenRequest {
  constructor(token, refreshToken) {
    this.token = token;
    this.refreshToken = refreshToken;
  }
}
