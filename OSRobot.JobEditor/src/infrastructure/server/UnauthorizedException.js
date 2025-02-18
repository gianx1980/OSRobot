export default class UnauthorizedException extends Error {
  constructor(responseCode, sourceError) {
    super(
      sourceError?.message || "",
      sourceError?.fileName || "",
      sourceError?.lineNumber || 0
    );
    this._sourceError = sourceError;
  }

  get sourceError() {
    return this._sourceError;
  }
  set sourceError(value) {
    this._sourceError = value;
  }
}
