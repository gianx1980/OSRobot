import ServiceResponse from "src/infrastructure/server/ServiceResponse.js";
import ServiceException from "src/infrastructure/server/ServiceException.js";
import ServiceBase from "src/infrastructure/server/ServiceBase.js";
import TrackErrorRequest from "src/infrastructure/data/requests/TrackErrorRequest.js";
import { api } from "boot/axios";
import { useAppStore } from "src/stores/appStore.js";
import { date } from "quasar";
import UnauthorizedException from "./server/UnauthorizedException.js";

export default class Utility {
  static showErrorDialog($q, $t, e, message = null) {
    const appStore = useAppStore();
    const serverConfig = appStore.getServerConfig();

    const appTitle = serverConfig ? serverConfig.appTitle : "";

    const config = {
      title: appTitle,
      message:
        message ??
        ((e instanceof ServiceException &&
          e.responseCode === ServiceBase.NotAuthorized) ||
          e instanceof UnauthorizedException)
          ? $t("sessionExpired")
          : $t("anErrorOccurredDuringTheOperation"),
    };

    Utility.showDialog($q, config);
  }

  static showDialog($q, config, priority, onOk, onCancel, onDismiss) {
    priority = priority || false;

    if (Utility.dialogVisible && !priority) return;

    Utility.dialogVisible = true;
    const dialog = $q
      .dialog(config)
      .onOk(() => {
        if (onOk) onOk();
      })
      .onCancel(() => {
        if (onCancel) onCancel();
      })
      .onDismiss(() => {
        if (onDismiss) onDismiss();

        Utility.dialogVisible = false;
      });

    return dialog;
  }

  static async trackError(user, errorException, details, err) {
    let result = null;
    let error = "";

    if (errorException) error = errorException;
    if (details) error += details;

    // Track error to the console
    if (err) console.error(err);

    try {
      // Send error to server
      const config = {
        headers: { Authorization: `Bearer ${user.token}` },
      };

      const response = await api.post(
        "/ClientErrorTracking/TrackError",
        new TrackErrorRequest(error),
        config
      );

      result = new ServiceResponse(response.data.responseCode, null);
    } catch (e) {
      console.error("Failed to send the error to the server.");
      console.error(e);
    }

    return result;
  }

  static jwtGetExpirationDate(jwtToken) {
    const base64Url = jwtToken.split(".")[1];
    const base64 = base64Url.replace("-", "+").replace("_", "/");
    const decodedPayload = JSON.parse(atob(base64));

    // The 'exp' claim in the payload contains the expiration timestamp in seconds
    const expirationTimestamp = decodedPayload.exp;

    // Convert the expiration timestamp to a JavaScript Date object
    const expirationDate = new Date(expirationTimestamp * 1000);

    return expirationDate;
  }

  static jwtShouldRenew(token) {
    const appStore = useAppStore();
    const serverConfig = appStore.getServerConfig();
    const requestNewTokenIfMinutesLeft =
      serverConfig !== null &&
      serverConfig.requestNewTokenIfMinutesLeft !== null
        ? serverConfig.requestNewTokenIfMinutesLeft
        : 1;
    const tokenExpDate = Utility.jwtGetExpirationDate(token);
    const tokenRenewableDate = date.subtractFromDate(tokenExpDate, {
      minutes: requestNewTokenIfMinutesLeft,
    });
    const now = new Date();

    return now >= tokenRenewableDate;
  }

  static manageException($q, $t, e, router) {
    if (e instanceof UnauthorizedException) {
      $q.dialog({
        title: $t("osRobot"),
        message: $t("yourSessionHasExpiredLoginAgain"),
      }).onOk(() => {
        router.push("/logout");
      });
    } else {
      console.error(e);
      $q.notify({
        color: "red",
        message: $t("anErrorOccurredDuringTheOperation"),
        position: "top",
      });
    }
  }
}

Utility.dialogVisible = false;
