<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

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
        <div class="row items-center q-mb-sm">
          <div class="col-4">
            <q-radio
              v-model="_formData.modelValue.schedule"
              val="OneTime"
              :label="_$t('oneTime')"
              dense
            />
          </div>
        </div>
        <div
          v-if="_formData.modelValue.schedule !== 'Interval'"
          class="row q-mb-sm"
        >
          <div class="col">
            <date-input
              :label="_$t('atDate')"
              v-model="_formData.modelValue.atDate"
              format="DD/MM/YYYY HH:mm"
              :disable="_formData.modelValue.schedule !== 'OneTime'"
            ></date-input>
          </div>
        </div>
        <div class="row items-center q-mb-sm">
          <div class="col-4">
            <q-radio
              v-model="_formData.modelValue.schedule"
              val="EveryDayAt"
              :label="_$t('everyDayAt')"
              dense
            />
          </div>
          <div class="col-4">
            <q-input
              filled
              v-model="_formData.modelValue.dailyTime"
              :label="_$t('time')"
              mask="fulltime"
              lazy-rules
              dense
              hide-bottom-space
              :disable="_formData.modelValue.schedule !== 'EveryDayAt'"
              :rules="[(val) => _isValidTime(val) || _$t('invalidTime')]"
            >
              <template v-slot:append>
                <q-icon name="access_time" class="cursor-pointer">
                  <q-popup-proxy
                    cover
                    transition-show="scale"
                    transition-hide="scale"
                  >
                    <q-time
                      v-model="_formData.modelValue.dailyTime"
                      mask="HH:mm:ss"
                      with-seconds
                      format24h
                    >
                      <div class="row items-center justify-end">
                        <q-btn
                          v-close-popup
                          :label="_$t('close')"
                          color="primary"
                          flat
                        />
                      </div>
                    </q-time>
                  </q-popup-proxy>
                </q-icon>
              </template>
            </q-input>
          </div>
        </div>
        <div class="row items-center q-mb-sm">
          <div class="col-4">
            <q-radio
              v-model="_formData.modelValue.schedule"
              val="Interval"
              :label="_$t('every')"
              dense
            />
          </div>
        </div>
        <div class="row q-col-gutter-sm">
          <div class="col">
            <q-input
              filled
              v-model.number="_formData.modelValue.intervalHours"
              :label="_$t('hours')"
              lazy-rules
              dense
              :disable="_formData.modelValue.schedule !== 'Interval'"
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
              v-model.number="_formData.modelValue.intervalMinutes"
              :label="_$t('minutes')"
              lazy-rules
              dense
              :disable="_formData.modelValue.schedule !== 'Interval'"
              type="number"
              min="0"
              max="59"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 59) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '59']),
              ]"
            />
          </div>
          <div class="col">
            <q-input
              filled
              v-model.number="_formData.modelValue.intervalSeconds"
              :label="_$t('seconds')"
              lazy-rules
              dense
              :disable="_formData.modelValue.schedule !== 'Interval'"
              type="number"
              min="0"
              max="59"
              :rules="[
                (val) => val === 0 || !!val || _$t('thisFieldIsMandatory'),
                (val) =>
                  (val >= 0 && val <= 59) ||
                  _$t('mustBeAValueBetweenXAndY', ['0', '59']),
              ]"
            />
          </div>
        </div>
        <div
          v-if="_formData.modelValue.schedule === 'Interval' && _intervalIsZero"
          class="text-negative text-caption q-mb-sm"
        >
          {{ _$t("intervalMustBeGreaterThanZero") }}
        </div>
        <!-- The interval's first occurrence, which the following ones are aligned to: the same
             atDate as "One time", shown here instead while this schedule is selected. -->
        <div v-if="_formData.modelValue.schedule === 'Interval'" class="row">
          <div class="col">
            <date-input
              :label="_$t('startingFrom')"
              v-model="_formData.modelValue.atDate"
              format="DD/MM/YYYY HH:mm"
            ></date-input>
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
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onTuesday"
              :label="_$t('tuesday')"
              dense
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onWednesday"
              :label="_$t('wednesday')"
              dense
              :disable="_formData.modelValue.schedule === 'OneTime'"
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
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onFriday"
              :label="_$t('friday')"
              dense
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
          <div class="col">
            <q-checkbox
              right-label
              v-model="_formData.modelValue.onSaturday"
              :label="_$t('saturday')"
              dense
              :disable="_formData.modelValue.schedule === 'OneTime'"
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
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
          <div class="col-4">
            <q-checkbox
              right-label
              @click="_allClick"
              v-model="_onAllDays"
              :label="_$t('allDays')"
              dense
              :disable="_formData.modelValue.schedule === 'OneTime'"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref, computed, watch } from "vue";
import { useI18n } from "vue-i18n";
import DateInput from "src/components/DateInput.vue";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _emit = defineEmits(["nodeNeedsUpdate"]);

const _i18n = useI18n();
const _$t = _i18n.t;

const _formData = ref(_props);
const _onAllDays = ref(true);

// Schedule: "OneTime", "EveryDayAt" or "Interval" (DateTimeEventSchedule on the server)
if (!_formData.value.modelValue.schedule)
  _formData.value.modelValue.schedule = "OneTime";
if (!_formData.value.modelValue.dailyTime)
  _formData.value.modelValue.dailyTime = "00:00:00";

function _isValidTime(val) {
  return /^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$/.test(val ?? "");
}

const _intervalIsZero = computed(
  () =>
    !(
      _formData.value.modelValue.intervalHours > 0 ||
      _formData.value.modelValue.intervalMinutes > 0 ||
      _formData.value.modelValue.intervalSeconds > 0
    )
);

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
</script>
