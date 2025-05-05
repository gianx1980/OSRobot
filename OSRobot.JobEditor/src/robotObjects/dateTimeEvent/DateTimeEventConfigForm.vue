<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_formData.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />

    <q-card class="q-mt-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("periodicity") }}</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row">
          <div class="col">
            <date-input
              :label="_$t('atDate')"
              v-model="_formData.modelValue.atDate"
              format="DD/MM/YYYY HH:mm"
            ></date-input>
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.periodicity"
              val="OneTime"
              :label="_$t('oneTime')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.periodicity"
              val="EveryDaysHoursMins"
              :label="_$t('every')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-input
              filled
              v-model.number="_formData.modelValue.everyNumDays"
              :label="_$t('days')"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.periodicity !== 'EveryDaysHoursMins'
              "
              type="number"
              min="0"
              max="9999"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 9999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '9999']),
              ]"
            />
          </div>
          <div class="col">
            <q-input
              filled
              v-model.number="_formData.modelValue.everyNumHours"
              :label="_$t('hours')"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.periodicity !== 'EveryDaysHoursMins'
              "
              type="number"
              min="0"
              max="999"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '999']),
              ]"
            />
          </div>
          <div class="col">
            <q-input
              filled
              v-model.number="_formData.modelValue.everyNumMinutes"
              :label="_$t('minutes')"
              lazy-rules
              dense
              :disable="
                _formData.modelValue.periodicity !== 'EveryDaysHoursMins'
              "
              type="number"
              min="0"
              max="999"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '999']),
              ]"
            />
          </div>
        </div>
        <div class="row q-mb-xs">
          <div class="col">
            <q-radio
              v-model="_formData.modelValue.periodicity"
              val="EverySeconds"
              :label="_$t('every')"
              left-label
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-4">
            <q-input
              filled
              v-model.number="_formData.modelValue.everyNumSeconds"
              :label="_$t('seconds')"
              lazy-rules
              type="number"
              min="0"
              max="9999"
              dense
              :disable="_formData.modelValue.periodicity !== 'EverySeconds'"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 999) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '9999']),
              ]"
            />
          </div>
        </div>
        <div>{{ _$t("onDays") }}</div>
        <div class="row">
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onMonday"
              :label="_$t('monday')"
              dense
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onTuesday"
              :label="_$t('tuesday')"
              dense
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onWednesday"
              :label="_$t('wednesday')"
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onThursday"
              :label="_$t('thursday')"
              dense
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onFriday"
              :label="_$t('friday')"
              dense
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onSaturday"
              :label="_$t('saturday')"
              dense
            />
          </div>
        </div>
        <div class="row">
          <div class="col-4">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onSunday"
              :label="_$t('sunday')"
              dense
            />
          </div>
          <div class="col-4">
            <q-checkbox
              right-label
              @click="_allClick"
              v-model="_onAllDays"
              :label="_$t('allDays')"
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref, computed, watch, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import DateInput from "src/components/DateInput.vue";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);

const _i18n = useI18n();
const _$t = _i18n.t;

const _formData = ref(_props);
const _onAllDays = ref(true);

// Handle days selection properties
const _dayMonday = 1;
const _dayTuesday = 2;
const _dayWednesday = 3;
const _dayThursday = 4;
const _dayFriday = 5;
const _daySaturday = 6;
const _daySunday = 0;

function _resyncDaysArray() {
  _formData.value.modelValue.onDays = [];

  if (_formData.value.modelValue.onMonday)
    _formData.value.modelValue.onDays.push(_dayMonday);

  if (_formData.value.modelValue.onTuesday)
    _formData.value.modelValue.onDays.push(_dayTuesday);

  if (_formData.value.modelValue.onWednesday)
    _formData.value.modelValue.onDays.push(_dayWednesday);

  if (_formData.value.modelValue.onThursday)
    _formData.value.modelValue.onDays.push(_dayThursday);

  if (_formData.value.modelValue.onFriday)
    _formData.value.modelValue.onDays.push(_dayFriday);

  if (_formData.value.modelValue.onSaturday)
    _formData.value.modelValue.onDays.push(_daySaturday);

  if (_formData.value.modelValue.onSunday)
    _formData.value.modelValue.onDays.push(_daySunday);
}

if (!_formData.value.modelValue.hasOwnProperty("onMonday"))
  _formData.value.modelValue.onMonday = true;
if (!_formData.value.modelValue.hasOwnProperty("onTuesday"))
  _formData.value.modelValue.onTuesday = true;
if (!_formData.value.modelValue.hasOwnProperty("onWednesday"))
  _formData.value.modelValue.onWednesday = true;
if (!_formData.value.modelValue.hasOwnProperty("onThursday"))
  _formData.value.modelValue.onThursday = true;
if (!_formData.value.modelValue.hasOwnProperty("onFriday"))
  _formData.value.modelValue.onFriday = true;
if (!_formData.value.modelValue.hasOwnProperty("onSaturday"))
  _formData.value.modelValue.onSaturday = true;
if (!_formData.value.modelValue.hasOwnProperty("onSunday"))
  _formData.value.modelValue.onSunday = true;

watch(
  () => [
    _formData.value.modelValue.onMonday,
    _formData.value.modelValue.onTuesday,
    _formData.value.modelValue.onWednesday,
    _formData.value.modelValue.onThursday,
    _formData.value.modelValue.onFriday,
    _formData.value.modelValue.onSaturday,
    _formData.value.modelValue.onSunday,
  ],
  () => {
    _resyncDaysArray();
  }
);

const _allDaysSelected = computed(() => {
  return (
    _formData.value.modelValue.onMonday &&
    _formData.value.modelValue.onTuesday &&
    _formData.value.modelValue.onWednesday &&
    _formData.value.modelValue.onThursday &&
    _formData.value.modelValue.onFriday &&
    _formData.value.modelValue.onSaturday &&
    _formData.value.modelValue.onSunday
  );
});

_onAllDays.value = _allDaysSelected.value;

watch(_allDaysSelected, (newValue) => {
  _onAllDays.value = newValue;
});

function _allClick() {
  _formData.value.modelValue.onMonday = _onAllDays.value;
  _formData.value.modelValue.onTuesday = _onAllDays.value;
  _formData.value.modelValue.onWednesday = _onAllDays.value;
  _formData.value.modelValue.onThursday = _onAllDays.value;
  _formData.value.modelValue.onFriday = _onAllDays.value;
  _formData.value.modelValue.onSaturday = _onAllDays.value;
  _formData.value.modelValue.onSunday = _onAllDays.value;
}

// Handle "Periodicity" property
if (!_formData.value.modelValue.hasOwnProperty("periodicity")) {
  _formData.value.modelValue.periodicity = "OneTime";
}

watch(
  () => _formData.value.modelValue.periodicity,
  (newValue) => {
    switch (newValue) {
      case "OneTime":
        _formData.value.modelValue.oneTime = true;
        _formData.value.modelValue.everyDaysHoursSecs = false;
        _formData.value.modelValue.everySeconds = false;
        break;

      case "EveryDaysHoursMins":
        _formData.value.modelValue.oneTime = false;
        _formData.value.modelValue.everyDaysHoursSecs = true;
        _formData.value.modelValue.everySeconds = false;
        break;

      case "EverySeconds":
        _formData.value.modelValue.oneTime = false;
        _formData.value.modelValue.everyDaysHoursSecs = false;
        _formData.value.modelValue.everySeconds = true;
        break;
    }
  }
);
</script>
