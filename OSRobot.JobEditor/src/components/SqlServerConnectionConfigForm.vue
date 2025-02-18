<template>
  <q-card class="q-mt-sm q-mb-sm">
    <q-card-section>
      <div class="text-h6">{{ _$t("connection") }}</div>
    </q-card-section>

    <q-card-section class="q-pt-none">
      <div class="row">
        <div class="col">
          <q-input
            filled
            v-model="_propsRef.modelValue.server"
            :label="_$t('server')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          />
        </div>
      </div>
      <div class="row" v-if="_showDatabaseField">
        <div class="col">
          <q-input
            filled
            v-model="_propsRef.modelValue.database"
            :label="_$t('database')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          />
        </div>
      </div>
      <div class="row">
        <div class="col">
          <q-input
            filled
            v-model="_propsRef.modelValue.username"
            :label="_$t('username')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          />
        </div>
      </div>
      <div class="row">
        <div class="col">
          <q-input
            filled
            v-model="_propsRef.modelValue.password"
            :label="_$t('password')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          />
        </div>
      </div>
      <div class="row">
        <div class="col">
          <q-input
            filled
            v-model="_propsRef.modelValue.connectionStringOptions"
            :label="_$t('connectionStringOptions')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          />
        </div>
      </div>
      <div class="row" v-if="_showTestConnectionButton">
        <div class="col">
          <q-btn
            :loading="_connecting"
            color="primary"
            icon="power"
            :label="_testConnectionButtonLabel"
            class="q-mt-sm"
            size="md"
            @click="_testRefreshConnection"
          />
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>
<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useQuasar } from "quasar";
import ClientUtils from "src/infrastructure/server/ClientUtils.js";
import { useAppStore } from "src/stores/appStore.js";

const _$q = useQuasar();

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();
const _router = useRouter();

import { useI18n } from "vue-i18n";
const _i18n = useI18n();
const _$t = _i18n.t;

const _props = defineProps([
  "modelValue",
  "showDatabaseField",
  "showTestConnectionButton",
  "testConnectionButtonLabel",
]);
const _emit = defineEmits(["update:modelValue", "attemptedConnection"]);
const _propsRef = ref(_props);
const _connecting = ref(false);

const _showDatabaseField = _propsRef.value.showDatabaseField || false;
const _showTestConnectionButton =
  _propsRef.value.showTestConnectionButton || true;
const _testConnectionButtonLabel =
  _propsRef.value.testConnectionButtonLabel || _$t("testRefresh");

async function _testRefreshConnection() {
  try {
    const connectionInfo = {
      server: _propsRef.value.modelValue.server,
      database: _propsRef.value.modelValue.database,
      username: _propsRef.value.modelValue.username,
      password: _propsRef.value.modelValue.password,
      connectionStringOptions:
        _propsRef.value.modelValue.connectionStringOptions,
    };

    const utils = new ClientUtils(_user.token, _user.refreshToken);

    _connecting.value = true;
    const result = await utils.sqlServerTestConnection(connectionInfo);
    _connecting.value = false;

    const eventArgs = { result: result.responseCode === 0 };
    _emit("attemptedConnection", eventArgs);
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
}
</script>
