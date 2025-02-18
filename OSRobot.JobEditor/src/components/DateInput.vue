<template>
  <q-input
    filled
    v-model="_dateValue"
    :label="_props.label"
    dense
    :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
  >
    <template v-slot:append>
      <q-icon name="event" class="cursor-pointer">
        <q-popup-proxy cover transition-show="scale" transition-hide="scale">
          <q-date v-model="_dateValue" :mask="_props.format">
            <div class="row items-center justify-end">
              <q-btn v-close-popup :label="_$t('close')" color="primary" flat />
            </div>
          </q-date>
        </q-popup-proxy>
      </q-icon>
      <q-icon name="access_time" class="cursor-pointer">
        <q-popup-proxy cover transition-show="scale" transition-hide="scale">
          <q-time v-model="_dateValue" :mask="_props.format" format24h>
            <div class="row items-center justify-end">
              <q-btn v-close-popup :label="_$t('close')" color="primary" flat />
            </div>
          </q-time>
        </q-popup-proxy>
      </q-icon>
    </template>
  </q-input>
</template>
<script setup>
import { date } from "quasar";
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const _props = defineProps(["label", "modelValue", "format"]);
const _emit = defineEmits(["update:modelValue"]);

const _formData = ref(_props);
const _dateValue = ref("");

const _date = Date.parse(_formData.value.modelValue);
if (!isNaN(_date)) {
  _dateValue.value = date.formatDate(_date, _props.format);
}

const _i18n = useI18n();
const _$t = _i18n.t;

watch(_dateValue, (newValue) => {
  const extractedDate = date.extractDate(newValue, _props.format);
  _emit(
    "update:modelValue",
    date.formatDate(extractedDate, "YYYY-MM-DDTHH:mm:ss.SSS") + "Z"
  );
});
</script>
