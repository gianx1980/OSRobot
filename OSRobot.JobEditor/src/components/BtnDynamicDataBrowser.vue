<template>
  <q-btn
    square
    size="xs"
    icon="bolt"
    color="primary"
    @click="_showDynamicDataBrowserClick"
    ><q-tooltip>{{ _$t("browseDynamicData") }}</q-tooltip></q-btn
  >
</template>

<script setup>
import DynamicDataBrowserDialog from "src/components/DynamicDataBrowserDialog.vue";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";

const _i18n = useI18n();
const _$t = _i18n.t;

const _$q = useQuasar();

const _props = defineProps(["modelValue", "modelValueKey", "folderItems"]);
const _emit = defineEmits(["update:modelValue"]);
const _propsRef = ref(_props);

function _showDynamicDataBrowserClick() {
  // Create a copy of the containingFolderItems array (remove itself from the array)
  const containingFolderItems = _propsRef.value.folderItems.filter(
    (t) => t.id !== _propsRef.value.modelValue.id && t.type !== "folder"
  );

  _$q
    .dialog({
      component: DynamicDataBrowserDialog,
      componentProps: {
        cancel: true,
        persistent: true,
        containingFolderItems: containingFolderItems,
      },
    })
    .onOk((ev) => {
      /* Build dynamic data string like this {Object[55].ExecutionStartDateYear} */
      let dynamicData = `{object[${ev.objectSelected.id}].${ev.dynDataSelected.internalName}}`;

      _propsRef.value.modelValue[_propsRef.value.modelValueKey] =
        (_propsRef.value.modelValue[_propsRef.value.modelValueKey] ?? "") +
        dynamicData;

      console.log(_propsRef.value.modelValue[_propsRef.value.modelValueKey]);
    });
}
</script>
