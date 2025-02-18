import { defineStore } from "pinia";
import { User } from "src/infrastructure/server/Account.js";
import { ServerConfig } from "src/infrastructure/server/Config.js";

export const useAppStore = defineStore("appStore", () => {
  const userStorageKey = "OSRClientUser";
  const serverConfigStorageKey = "OSRServerConfig";
  const connectedStorageKey = "OSRConnected";

  let _loggedUser = null;
  let _serverConfig = null;
  let _connected = null;

  function setLoggedUser(user) {
    if (user === null) localStorage.removeItem(userStorageKey);
    else localStorage.setItem(userStorageKey, JSON.stringify(user));
    _loggedUser = user;
  }

  function getLoggedUser() {
    if (_loggedUser !== null) {
      return _loggedUser;
    } else {
      const userSerialized = localStorage.getItem(userStorageKey);
      if (userSerialized !== null) {
        const tempUser = JSON.parse(userSerialized);
        _loggedUser = new User(
          tempUser._username,
          tempUser._token,
          tempUser._refreshToken
        );
      }

      return _loggedUser;
    }
  }

  function logOut() {
    _loggedUser = null;
    _serverConfig = null;
    _connected = null;
    localStorage.removeItem(userStorageKey);
    localStorage.removeItem(serverConfigStorageKey);
    localStorage.removeItem(connectedStorageKey);
  }

  function setServerConfig(serverConfig) {
    if (serverConfig === null) localStorage.removeItem(serverConfigStorageKey);
    else
      localStorage.setItem(
        serverConfigStorageKey,
        JSON.stringify(serverConfig)
      );
    _serverConfig = serverConfig;
  }

  function getServerConfig() {
    if (_serverConfig !== null) {
      return _serverConfig;
    } else {
      const configSerialized = localStorage.getItem(serverConfigStorageKey);
      if (configSerialized !== null) {
        const tempConfig = JSON.parse(configSerialized);
        _serverConfig = new ServerConfig(
          tempConfig._requestNewTokenIfMinutesLeft,
          tempConfig._appTitle,
          tempConfig._staticFilesUrl,
          tempConfig._heartBeatInterval,
          tempConfig._notificationServerSentEventsEnabled,
          tempConfig._notificationPollingInterval
        );
      }

      return _serverConfig;
    }
  }

  function setConnected(connected) {
    if (connected === null) localStorage.removeItem(connectedStorageKey);
    else localStorage.setItem(connectedStorageKey, JSON.stringify(connected));
    _connected = connected;
  }

  function getConnected() {
    if (_connected !== null) {
      return _connected;
    } else {
      const connectedSerialized = localStorage.getItem(connectedStorageKey);
      if (connectedSerialized !== null) {
        _connected = JSON.parse(connectedSerialized);
      }

      return _connected;
    }
  }

  return {
    getLoggedUser,
    setLoggedUser,
    logOut,
    setServerConfig,
    getServerConfig,
    getConnected,
    setConnected,
  };
});
