<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <q-dialog ref="dialogRef" @hide="onDialogHide" persistent>
    <q-card class="q-dialog-plugin" style="width: 500px; max-width: 90vw">
      <q-card-section>
        <div class="text-h6">
          {{
            _isNew ? _$t("addVariableOrSecret") : _$t("editVariableOrSecret")
          }}
        </div>
      </q-card-section>
      <q-card-section>
        <q-form class="q-gutter-md" @submit="_formSubmit">
          <q-input
            filled
            dense
            v-model="_model.name"
            :label="_$t('name')"
            :readonly="!_isNew"
            maxlength="100"
            lazy-rules
            :rules="[
              (val) => !!val || _$t('thisFieldIsMandatory'),
              (val) => _namePattern.test(val) || _$t('variableNameRule'),
            ]"
          />
          <q-toggle v-model="_model.isSecret" :label="_$t('secret')" />
          <div class="text-caption text-grey-8">
            {{ _model.isSecret ? _$t("secretHint") : _$t("variableHint") }}
          </div>
          <q-input
            filled
            dense
            v-model="_model.value"
            :label="_$t('value')"
            :type="_model.isSecret && !_showValue ? 'password' : 'text'"
            :hint="_valueHint"
            autocomplete="new-password"
            maxlength="32768"
            lazy-rules
            :rules="[
              (val) => !_valueRequired || !!val || _$t('thisFieldIsMandatory'),
            ]"
          >
            <template v-slot:append v-if="_model.isSecret">
              <q-icon
                :name="_showValue ? 'visibility_off' : 'visibility'"
                class="cursor-pointer"
                @click="_showValue = !_showValue"
              />
            </template>
          </q-input>
          <q-input
            filled
            dense
            v-model="_model.description"
            :label="_$t('description')"
            maxlength="500"
          />
          <div class="text-caption">
            {{ _$t("useItAs") }}
            <code>{{ _placeholder }}</code>
          </div>
          <div class="row" align="right">
            <div class="col">
              <q-btn
                class="q-ml-sm"
                color="primary"
                :label="_$t('cancel')"
                @click="onDialogCancel"
              />
              <q-btn
                class="q-ml-sm"
                type="submit"
                color="primary"
                :label="_$t('save')"
                :loading="_saving"
              />
            </div>
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useQuasar, useDialogPluginComponent } from "quasar";
import { useI18n } from "vue-i18n";
import { useAppStore } from "src/stores/appStore.js";
import Variables, {
  variablePlaceholder,
} from "src/infrastructure/server/Variables.js";
import Utility from "src/infrastructure/Utility.js";

const _$q = useQuasar();
const _router = useRouter();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

// variable: the item to edit (as listed by the server), or null to add one.
// initialValue / initialIsSecret / initialName: prefill a new item (e.g. "Move to secret").
const _props = defineProps([
  "variable",
  "initialName",
  "initialValue",
  "initialIsSecret",
]);
// Dialog props don't change while it is open: read once.
const _initial = ref(_props).value;

defineEmits([...useDialogPluginComponent.emits]);

const { dialogRef, onDialogHide, onDialogOK, onDialogCancel } =
  useDialogPluginComponent();

// Same rule as the server (VariableStore).
const _namePattern = /^[A-Za-z_][A-Za-z0-9_.-]*$/;

const _isNew = !_initial.variable;
const _wasSecret = !!_initial.variable?.isSecret;

const _model = ref({
  name: _initial.variable?.name ?? _initial.initialName ?? "",
  isSecret: _initial.variable?.isSecret ?? _initial.initialIsSecret ?? false,
  // A secret's value is never sent back by the server.
  value: _initial.variable?.isSecret
    ? ""
    : _initial.variable?.value ?? _initial.initialValue ?? "",
  description: _initial.variable?.description ?? "",
});

const _showValue = ref(false);
const _saving = ref(false);

// A new secret needs a value; an existing one keeps its value when the field is left empty. (A
// secret turned into a variable needs it too: the server says so.)
const _valueRequired = computed(() => _isNew && _model.value.isSecret);

const _valueHint = computed(() => {
  if (_wasSecret && _model.value.isSecret)
    return _$t("leaveEmptyToKeepTheCurrentValue");
  if (_wasSecret && !_model.value.isSecret)
    return _$t("enterTheValueAgainToMakeItVisible");
  return "";
});

const _placeholder = computed(() =>
  variablePlaceholder(_model.value.name || "Name", _model.value.isSecret)
);

async function _formSubmit() {
  try {
    _saving.value = true;
    const variables = new Variables(_user.token, _user.refreshToken);
    const result = await variables.save({
      name: _model.value.name,
      value: _model.value.value,
      isSecret: _model.value.isSecret,
      description: _model.value.description,
    });

    if (result.responseCode === Variables.Ok) {
      onDialogOK({ name: _model.value.name, isSecret: _model.value.isSecret });
    } else {
      _$q.notify({
        color: "red",
        message: result.responseMessage ?? _$t("genericError"),
        position: "top",
      });
    }
  } catch (e) {
    Utility.manageException(_$q, _$t, e, _router);
  } finally {
    _saving.value = false;
  }
}
</script>
