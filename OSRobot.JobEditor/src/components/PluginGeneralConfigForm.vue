<template>
  <q-card>
    <q-card-section>
      <div class="text-h6">{{ _$t("general") }}</div>
    </q-card-section>

    <q-card-section class="q-pt-none">
      <q-input
        filled
        v-model="_propsRef.modelValue.id"
        :label="_$t('objectId')"
        lazy-rules
        readonly
        dense
      />

      <q-input
        filled
        v-model="_propsRef.modelValue.name"
        :label="_$t('name')"
        lazy-rules
        :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
        dense
        maxlength="500"
      />

      <q-toggle
        v-if="_showToggleEnableField"
        v-model="_propsRef.modelValue.enabled"
        :label="_$t('enabled')"
        left-label
        dense
      />

      <q-toggle
        v-if="_showToggleLogField"
        class="q-ml-sm"
        v-model="_propsRef.modelValue.log"
        :label="_$t('log')"
        left-label
        dense
      />
    </q-card-section>
  </q-card>
</template>
<script setup>
import { ref, watch } from "vue";

import { useI18n } from "vue-i18n";
const _i18n = useI18n();
const _$t = _i18n.t;

const _props = defineProps([
  "modelValue",
  "showToggleLogField",
  "showToggleEnableField",
]);
const _emit = defineEmits(["update:modelValue", "nodeNeedsUpdate"]);
const _propsRef = ref(_props);

const _showToggleLogField = _propsRef.value.showToggleLogField ?? true;
const _showToggleEnableField = _propsRef.value.showToggleEnableField ?? true;

watch(
  () => _propsRef.value.modelValue.name,
  (newName, oldName) => {
    const eventArgs = {
      nodeId: _propsRef.value.modelValue.id,
      pluginId: _propsRef.value.modelValue.pluginId,
      newName: newName,
      oldName: oldName,
    };
    _emit("nodeNeedsUpdate", eventArgs);
  }
);
</script>
