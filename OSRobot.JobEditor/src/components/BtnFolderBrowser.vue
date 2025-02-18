<template>
  <q-btn
    square
    size="xs"
    icon="folder_open"
    color="primary"
    @click="_showFolderBrowserClick"
    ><q-tooltip>{{ _$t("browseFolder") }}</q-tooltip></q-btn
  >
</template>

<script setup>
import FolderBrowserDialog from "src/components/FolderBrowserDialog.vue";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();

const _props = defineProps(["modelValue", "modelValueKey"]);
const _emit = defineEmits(["update:modelValue"]);
const _propsRef = ref(_props);

function _showFolderBrowserClick() {
  _$q
    .dialog({
      component: FolderBrowserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
      },
    })
    .onOk((ev) => {
      _propsRef.value.modelValue[_propsRef.value.modelValueKey] =
        _propsRef.value.modelValue[_propsRef.value.modelValueKey] +
        ev.selectedFolder;
    });
}
</script>
