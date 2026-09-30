<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      :showToggleLogField="false"
      :showToggleEnableField="false"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
  </div>
</template>
<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);

const _i18n = useI18n();
const _$t = _i18n.t;

const _formData = ref(_props);
</script>
