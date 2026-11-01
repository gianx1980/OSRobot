<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("call") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <q-input
          filled
          dense
          v-model="_formData.modelValue.path"
          :label="_$t('webhookPath')"
          maxlength="100"
          lazy-rules
          :rules="[
            (val) => !!val || _$t('thisFieldIsMandatory'),
            (val) => _pathPattern.test(val) || _$t('webhookPathRule'),
          ]"
        />
        <q-input
          filled
          dense
          readonly
          :model-value="_url"
          :label="_$t('webhookUrl')"
          class="q-mb-md"
        >
          <template v-slot:append>
            <q-btn
              flat
              dense
              round
              size="sm"
              icon="content_copy"
              @click="_copy(_url)"
              ><q-tooltip>{{ _$t("copy") }}</q-tooltip></q-btn
            >
          </template>
        </q-input>
        <q-select
          v-model="_formData.modelValue.authMode"
          :options="_authModeOptions"
          :label="_$t('webhookAuthentication')"
          dense
          map-options
          emit-value
          class="q-mb-md"
        />
        <q-input
          filled
          dense
          v-model="_formData.modelValue.token"
          :label="_isSignature ? _$t('webhookSigningKey') : _$t('webhookToken')"
          lazy-rules
          :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
        >
          <template v-slot:append>
            <BtnMoveToSecret
              :modelValue="_formData.modelValue"
              modelValueKey="token"
              :suggestedName="_secretName"
            />
          </template>
        </q-input>
        <q-btn
          color="primary"
          icon="key"
          size="md"
          class="q-mb-md"
          :label="_$t('generateWebhookToken')"
          :loading="_generating"
          @click="_generateClick"
        />
        <q-input
          v-if="_isSignature"
          filled
          dense
          v-model="_formData.modelValue.signatureHeader"
          :label="_$t('webhookSignatureHeader')"
          lazy-rules
          :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
        />
        <q-input
          filled
          dense
          v-model.number="_formData.modelValue.maxCallsPerMinute"
          :label="_$t('webhookMaxCallsPerMinute')"
          :hint="_$t('webhookMaxCallsPerMinuteHint')"
          type="number"
          min="0"
          lazy-rules
          :rules="[
            (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
            (val) =>
              val >= 0 || _$t('mustBeAValueBetweenXAndY', ['0', '1000000']),
          ]"
        />
        <div class="text-caption text-grey-8">{{ _$t("example") }}:</div>
        <pre
          class="text-caption q-mt-xs q-mb-none"
          style="white-space: pre-wrap"
          >{{ _example }}</pre
        >
        <q-btn
          flat
          dense
          size="sm"
          icon="content_copy"
          :label="_$t('copy')"
          @click="_copy(_example)"
        />
      </q-card-section>
    </q-card>
    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("webhookBody") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <q-input
          filled
          dense
          v-model="_formData.modelValue.jsonPathToData"
          :label="_$t('jsonPathToData')"
          :hint="_$t('webhookJsonPathHint')"
        />
        <q-toggle
          class="q-mt-md"
          v-model="_formData.modelValue.returnsRecordset"
          :label="_$t('returnsRecordset')"
          :disable="!_formData.modelValue.jsonPathToData"
          left-label
          dense
        />
      </q-card-section>
    </q-card>
    <div class="text-caption text-grey-8 q-mt-sm">
      {{ _$t("webhookUntrustedDataWarning") }}
    </div>
  </div>
</template>
<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, copyToClipboard } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import BtnMoveToSecret from "src/components/BtnMoveToSecret.vue";
import Variables, {
  variablePlaceholder,
} from "src/infrastructure/server/Variables.js";
import Utility from "src/infrastructure/Utility.js";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);
const _formData = ref(_props);

const _$q = useQuasar();
const _router = useRouter();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

// Same rule as the server (WebhookPath).
const _pathPattern = /^[A-Za-z0-9_-]+$/;

const _authModeOptions = [
  { label: _$t("webhookAuthToken"), value: "Token" },
  { label: _$t("webhookAuthSignature"), value: "HmacSha256" },
];

const _generating = ref(false);

// The server's default config is one object shared by all new hooks: the path must be made here.
if (!_formData.value.modelValue.path) {
  _formData.value.modelValue.path = _randomHex(6);
}

const _isSignature = computed(
  () => _formData.value.modelValue.authMode === "HmacSha256"
);

const _secretName = computed(() => `Webhook_${_formData.value.modelValue.id}`);

const _url = computed(
  () =>
    `${AppConfig.apiUrl.replace(/\/api\/?$/, "")}/hooks/${
      _formData.value.modelValue.path
    }`
);

const _example = computed(() => {
  const credential = _isSignature.value
    ? `${_formData.value.modelValue.signatureHeader}: sha256=<HMAC-SHA256 of the body>`
    : "Authorization: Bearer <token>";

  return `curl -X POST -H "${credential}" -H "Content-Type: application/json" -d "{}" ${_url.value}`;
});

function _randomHex(byteCount) {
  const bytes = crypto.getRandomValues(new Uint8Array(byteCount));
  return Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
}

function _copy(text) {
  copyToClipboard(text).then(() => {
    _$q.notify({ color: "green", message: _$t("copied"), position: "top" });
  });
}

function _generateClick() {
  if (!_formData.value.modelValue.token) {
    _generate();
    return;
  }

  _$q
    .dialog({
      title: _$t("generateWebhookToken"),
      message: _$t("generateWebhookTokenConfirm"),
      cancel: true,
      persistent: true,
    })
    .onOk(_generate);
}

// The token is stored as a secret, so it is shown now and never again: the caller needs it.
async function _generate() {
  const token = _randomHex(32);

  try {
    _generating.value = true;
    const variables = new Variables(_user.token, _user.refreshToken);
    const result = await variables.save({
      name: _secretName.value,
      value: token,
      isSecret: true,
      description: _formData.value.modelValue.name,
    });

    if (result.responseCode !== Variables.Ok) {
      _$q.notify({
        color: "red",
        message: result.responseMessage ?? _$t("genericError"),
        position: "top",
      });
      return;
    }

    _formData.value.modelValue.token = variablePlaceholder(
      _secretName.value,
      true
    );

    _$q
      .dialog({
        title: _$t("webhookTokenGenerated"),
        message: `${_$t("webhookTokenGeneratedMessage")}\n\n${token}`,
        style: "white-space: pre-line; word-break: break-all;",
        persistent: true,
        cancel: { label: _$t("copy"), flat: true },
        ok: { label: _$t("close") },
      })
      .onCancel(() => _copy(token));
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  } finally {
    _generating.value = false;
  }
}
</script>
