<template>
  <q-btn
    square
    size="xs"
    icon="description"
    color="primary"
    @click="_showFileBrowserClick"
    ><q-tooltip>{{ _$t("browseFiles") }}</q-tooltip></q-btn
  >
</template>

<script setup>
import FileBrowserDialog from "src/components/FileBrowserDialog.vue";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();

const _props = defineProps(["modelValue", "modelValueKey"]);
const _emit = defineEmits(["update:modelValue"]);
const _propsRef = ref(_props);

function _showFileBrowserClick() {
  _$q
    .dialog({
      component: FileBrowserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
      },
    })
    .onOk((ev) => {
      _propsRef.value.modelValue[_propsRef.value.modelValueKey] =
        _propsRef.value.modelValue[_propsRef.value.modelValueKey] +
        ev.selectedValue;
    });
}
</script>
