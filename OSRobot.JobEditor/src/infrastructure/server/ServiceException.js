// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

export default class ServiceException extends Error {
  constructor(responseCode, sourceError) {
    super(
      sourceError?.message || "",
      sourceError?.fileName || "",
      sourceError?.lineNumber || 0
    );
    this._responseCode = responseCode;
    this._sourceError = sourceError;
  }

  get responseCode() {
    return this._responseCode;
  }
  set responseCode(value) {
    this._responseCode = value;
  }

  get sourceError() {
    return this._sourceError;
  }
  set sourceError(value) {
    this._sourceError = value;
  }
}
