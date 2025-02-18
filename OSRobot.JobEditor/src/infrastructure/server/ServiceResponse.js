export default class ServiceResponse {
  constructor(responseCode, responseObject) {
    this._responseCode = responseCode;
    this._responseObject = responseObject;
  }

  get responseCode() {
    return this._responseCode;
  }
  set responseCode(value) {
    this._responseCode = value;
  }

  get responseObject() {
    return this._responseObject;
  }
  set responseObject(value) {
    this._responseObject = value;
  }
}
