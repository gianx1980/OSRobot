<template>
  <q-layout view="hHh Lpr lff">
    <q-page-container>
      <q-page
        class="window-height window-width row justify-center items-center"
      >
        <div class="column">
          <div class="row">
            <h5 class="text-h5 text-white q-my-md">{{ _$t("osRobot") }}</h5>
          </div>
          <div class="row">
            <q-card square bordered class="q-pa-xl shadow-1">
              <q-card-section>
                <q-form class="q-gutter-md">
                  <q-input
                    square
                    filled
                    clearable
                    type="text"
                    :label="_$t('username')"
                    v-model="_formData.username"
                  />
                  <q-input
                    square
                    filled
                    clearable
                    type="password"
                    :label="_$t('password')"
                    v-model="_formData.password"
                  />
                </q-form>
              </q-card-section>
              <q-card-actions class="q-px-md">
                <q-btn
                  unelevated
                  color="light-blue-7"
                  size="lg"
                  class="full-width"
                  :label="_$t('login')"
                  :loading="_formIsSubmitting"
                  @click="_login()"
                />
              </q-card-actions>
            </q-card>
          </div>
        </div>
      </q-page>
    </q-page-container>
  </q-layout>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAppStore } from "../stores/appStore.js";

import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import Utility from "src/infrastructure/Utility.js";
import { Account } from "src/infrastructure/server/Account.js";
import { Config, ServerConfig } from "src/infrastructure/server/Config.js";

const _appStore = useAppStore();
const _router = useRouter();
const _i18n = useI18n();
const _$t = _i18n.t;
const _$q = useQuasar();
const _formData = ref({ username: "admin", password: "admin" });
const _formIsSubmitting = ref(false);

if (process.env.NODE_ENV !== "production") {
  _formData.value.username = "admin";
  _formData.value.password = "admin";
}

async function _login() {
  _formIsSubmitting.value = true;

  try {
    const account = new Account();
    const resultLogin = await account.login(
      _formData.value.username,
      _formData.value.password
    );

    if (
      resultLogin.data.responseCode === 0 &&
      resultLogin.data.responseObject !== null
    ) {
      // Login OK
      // Get and store server configuration
      const config = new Config(resultLogin.data.responseObject.token);
      const serverConfig = await config.getConfig();
      _appStore.setServerConfig(serverConfig.data.responseObject);

      // Redirect to home page
      _appStore.setLoggedUser(resultLogin.data.responseObject);
      _appStore.setConnected(true);
      _router.push("/");
    } else {
      _formIsSubmitting.value = false;
      if (resultLogin.data.responseCode === Account.WrongCredentials) {
        Utility.showErrorDialog(_$q, _$t, _$t("wrongUserNameOrPassword"));
      } else {
        Utility.showErrorDialog(_$q, _$t);
      }
    }
  } catch (e) {
    _formIsSubmitting.value = false;
    Utility.showErrorDialog(_$q, _$t);
  }
}
</script>
