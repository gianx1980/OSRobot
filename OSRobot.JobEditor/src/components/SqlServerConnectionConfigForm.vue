<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <q-card class="q-mt-sm q-mb-sm">
    <q-card-section>
      <div class="text-h6">{{ _$t("connection") }}</div>
    </q-card-section>

    <q-card-section class="q-pt-none">
      <div class="row">
        <div class="col-11">
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
        <div class="col-1">
          <BtnDynamicDataBrowser
            class="q-ml-sm"
            v-model="_propsRef.modelValue"
            :folderItems="_propsRef.containingFolderItems ?? []"
            modelValueKey="server"
          />
        </div>
      </div>
      <div class="row" v-if="_showDatabaseField">
        <div class="col-11">
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
        <div class="col-1">
          <BtnDynamicDataBrowser
            class="q-ml-sm"
            v-model="_propsRef.modelValue"
            :folderItems="_propsRef.containingFolderItems ?? []"
            modelValueKey="database"
          />
        </div>
      </div>
      <div class="row">
        <div class="col-11">
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
        <div class="col-1">
          <BtnDynamicDataBrowser
            class="q-ml-sm"
            v-model="_propsRef.modelValue"
            :folderItems="_propsRef.containingFolderItems ?? []"
            modelValueKey="username"
          />
        </div>
      </div>
      <div class="row">
        <div class="col-11">
          <q-input
            filled
            v-model="_propsRef.modelValue.password"
            :label="_$t('password')"
            lazy-rules
            :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            dense
            maxlength="500"
          >
            <template v-slot:append>
              <BtnMoveToSecret
                :modelValue="_propsRef.modelValue"
                modelValueKey="password"
                suggestedName="SqlServerPassword"
              />
            </template>
          </q-input>
        </div>
        <div class="col-1">
          <BtnDynamicDataBrowser
            class="q-ml-sm"
            v-model="_propsRef.modelValue"
            :folderItems="_propsRef.containingFolderItems ?? []"
            modelValueKey="password"
          />
        </div>
      </div>
      <div class="row">
        <div class="col-11">
          <q-input
            filled
            v-model="_propsRef.modelValue.connectionStringOptions"
            :label="_$t('connectionStringOptions')"
            lazy-rules
            dense
            maxlength="500"
          />
        </div>
        <div class="col-1">
          <BtnDynamicDataBrowser
            class="q-ml-sm"
            v-model="_propsRef.modelValue"
            :folderItems="_propsRef.containingFolderItems ?? []"
            modelValueKey="connectionStringOptions"
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
import Utility from "src/infrastructure/Utility.js";
import BtnMoveToSecret from "src/components/BtnMoveToSecret.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";
import { useAppStore } from "src/stores/appStore.js";

const _$q = useQuasar();

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();
const _router = useRouter();

import { useI18n } from "vue-i18n";
const _i18n = useI18n();
const _$t = _i18n.t;

// containingFolderItems: the objects of the folder, for the dynamic data buttons (as in the task forms).
const _props = defineProps([
  "modelValue",
  "containingFolderItems",
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

    // e.g. a {secret['Name']} that is not defined
    if (result.responseCode === ClientUtils.InvalidVariable) {
      _$q.notify({
        color: "red",
        message: result.responseMessage,
        position: "top",
      });
    }

    const eventArgs = { result: result.responseCode === 0 };
    _emit("attemptedConnection", eventArgs);
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  }
}
</script>
