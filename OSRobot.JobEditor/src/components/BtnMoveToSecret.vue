<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<!-- Next to a password field: stores the password typed there as a secret and replaces it with
     the {secret['Name']} placeholder, so jobs.json no longer holds it. Shown only while the field
     holds a plain-text value. -->
<template>
  <q-btn
    v-if="_isPlainText"
    flat
    dense
    round
    size="sm"
    icon="lock"
    color="primary"
    @click.stop="_moveClick"
    ><q-tooltip>{{ _$t("moveToSecret") }}</q-tooltip></q-btn
  >
</template>

<script setup>
import { computed, ref } from "vue";
import { useQuasar } from "quasar";
import { useI18n } from "vue-i18n";
import VariableEditDialog from "src/components/VariableEditDialog.vue";
import { variablePlaceholder } from "src/infrastructure/server/Variables.js";

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

// modelValue: the config object; modelValueKey: its password property; suggestedName: the secret's default name.
const _props = defineProps(["modelValue", "modelValueKey", "suggestedName"]);
const _propsRef = ref(_props);

// Same idea as the server's plain-text password warning: a value that takes the password from
// elsewhere (a placeholder or a [CODE] expression) is not stored in jobs.json.
const _placeholderPattern = /\{(object\[\d+\]\.|environment\[|var\[|secret\[)/i;

const _isPlainText = computed(() => {
  const value = _props.modelValue[_props.modelValueKey];
  return (
    !!value && !value.startsWith("[CODE]") && !_placeholderPattern.test(value)
  );
});

function _moveClick() {
  _$q
    .dialog({
      component: VariableEditDialog,
      componentProps: {
        initialName: _props.suggestedName ?? "Password",
        initialValue: _props.modelValue[_props.modelValueKey],
        initialIsSecret: true,
      },
    })
    .onOk((ev) => {
      _propsRef.value.modelValue[_propsRef.value.modelValueKey] =
        variablePlaceholder(ev.name, ev.isSecret);

      _$q.notify({
        color: "green",
        message: _$t("passwordMovedToSecret"),
        position: "top",
        timeout: 8000,
      });
    });
}
</script>
