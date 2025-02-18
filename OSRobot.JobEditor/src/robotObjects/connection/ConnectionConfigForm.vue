<template>
  <div class="q-pa-md">
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("general") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row q-mb-sm">
          <div class="col">
            <q-input
              filled
              v-model="_propsRef.modelValue.waitSeconds"
              :label="$t('waitSecondsBeforeNextObj')"
              lazy-rules
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.enabled"
              :label="$t('enabled')"
              left-label
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <ExecuteConditions
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
      conditionType="executeConditions"
    />

    <ExecuteConditions
      v-model="_propsRef.modelValue"
      :containingFolderItems="_propsRef.containingFolderItems"
      conditionType="dontExecuteConditions"
    />
  </div>
</template>
<script setup>
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import ExecuteConditions from "src/components/ExecuteConditions.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _formData = ref(_props);
</script>
