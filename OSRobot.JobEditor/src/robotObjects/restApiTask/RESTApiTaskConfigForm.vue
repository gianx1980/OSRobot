<template>
  <div class="q-pa-md">
    <PluginGeneralConfigForm
      v-model="_propsRef.modelValue"
      @nodeNeedsUpdate="_emit('nodeNeedsUpdate', $event)"
    />
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("apiCall") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col-11">
            <q-input
              filled
              class="q-pr-xs"
              v-model="_propsRef.modelValue.url"
              :label="_$t('url')"
              lazy-rules
              dense
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
          <div class="col-1">
            <BtnDynamicDataBrowser
              v-model="_propsRef.modelValue"
              :folderItems="_propsRef.containingFolderItems"
              modelValueKey="url"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-select
              v-model="_propsRef.modelValue.method"
              :options="_methods"
              :label="_$t('method')"
              dense
              lazy-rules
              map-options
              emit-value
              :rules="[(val) => !!val || _$t('thisFieldIsMandatory')]"
            />
          </div>
        </div>
        <div class="row">
          <div class="col">
            <q-card class="q-mt-sm">
              <q-card-section>
                <div class="text-subtitle1">
                  {{ _$t("headers") }}
                </div>
              </q-card-section>
              <q-card-section>
                <div class="row">
                  <div class="col">
                    <q-table
                      :columns="_headersDef"
                      :rows="_headersList"
                      :visible-columns="_headersColumnsVisibility"
                      :no-data-label="_$t('thereAreNoHeadersToShow')"
                      row-key="id"
                      dense
                    >
                      <template v-slot:body-cell-actions="props">
                        <q-td :props="props">
                          <q-btn
                            square
                            class="q-ml-xs"
                            size="xs"
                            icon="mode_edit"
                            color="primary"
                          >
                            <q-menu>
                              <q-list style="min-width: 100px">
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_headerEditItemClick(props.row)"
                                    >{{ _$t("edit") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_headerDeleteItemClick(props.row)"
                                    >{{ _$t("delete") }}</q-item-section
                                  >
                                </q-item>
                                <q-separator />
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_headerItemMoveUpClick(props.row)"
                                    >{{ _$t("moveUp") }}</q-item-section
                                  >
                                </q-item>
                                <q-item clickable v-close-popup>
                                  <q-item-section
                                    @click="_headerItemMoveDownClick(props.row)"
                                    >{{ _$t("moveDown") }}</q-item-section
                                  >
                                </q-item>
                              </q-list>
                            </q-menu>
                          </q-btn>
                        </q-td>
                      </template>
                    </q-table>
                  </div>
                </div>
                <div class="row">
                  <div class="col">
                    <q-btn
                      color="primary"
                      icon="add"
                      :label="_$t('add')"
                      class="q-mt-sm"
                      size="md"
                      @click="_headerAddItemClick"
                    />
                  </div>
                </div>
                <div class="row q-mt-md">
                  <div class="col-11">
                    <q-input
                      type="textarea"
                      class="q-pr-xs"
                      filled
                      v-model="_propsRef.modelValue.parameters"
                      :label="_$t('parametersJSON')"
                      lazy-rules
                      dense
                    />
                  </div>
                  <div class="col-1">
                    <BtnDynamicDataBrowser
                      v-model="_propsRef.modelValue"
                      :folderItems="_propsRef.containingFolderItems"
                      modelValueKey="parameters"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
        <div class="row q-mt-md">
          <div class="col">
            <q-toggle
              v-model="_propsRef.modelValue.returnsRecordset"
              :label="_$t('returnsRecordset')"
              left-label
              dense
            />
          </div>
        </div>
      </q-card-section>
    </q-card>
    <PluginIterationConfigForm v-model="_propsRef.modelValue" />
  </div>
  <q-dialog v-model="_headerDialogVisibility" persistent>
    <div
      class="q-pa-md q-gutter-sm"
      style="width: 650px; max-width: 80vw; height: 650px"
    >
      <q-card style="width: 600px" class="q-px-sm q-pb-md">
        <q-card-section>
          <div class="text-h6">{{ _headerDialogTitle }}</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="_headerDialogFormSubmit">
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_headerDialogFormData.name"
                  :label="_$t('name')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  v-model="_headerDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="headerTitle"
                />
              </div>
            </div>
            <div class="row">
              <div class="col-11">
                <q-input
                  filled
                  v-model="_headerDialogFormData.value"
                  :label="_$t('value')"
                  dense
                  lazy-rules
                />
              </div>
              <div class="col-1">
                <BtnDynamicDataBrowser
                  v-model="_headerDialogFormData"
                  :folderItems="_propsRef.containingFolderItems"
                  modelValueKey="value"
                />
              </div>
            </div>
            <div class="column items-end">
              <div class="col">
                <q-btn
                  flat
                  :label="_$t('cancel')"
                  color="primary"
                  v-close-popup
                />
                <q-btn flat type="submit" :label="_$t('ok')" color="primary" />
              </div>
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </div>
  </q-dialog>
</template>
<script setup>
import { ref, watch, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuasar } from "quasar";
import PluginGeneralConfigForm from "src/components/PluginGeneralConfigForm.vue";
import PluginIterationConfigForm from "src/components/PluginIterationConfigForm.vue";
import BtnDynamicDataBrowser from "src/components/BtnDynamicDataBrowser.vue";

const _props = defineProps(["modelValue", "containingFolderItems"]);
const _propsRef = ref(_props);

const _emit = defineEmits(["nodeNeedsUpdate"]);

const _$q = useQuasar();

const _i18n = useI18n();
const _$t = _i18n.t;

const _methods = [
  { label: _$t("get"), value: "Get" },
  { label: _$t("post"), value: "Post" },
  { label: _$t("put"), value: "Put" },
  { label: _$t("delete"), value: "Delete" },
];

const _headersDef = [
  {
    name: "name",
    label: _$t("name"),
    align: "left",
    field: "name",
  },
  {
    name: "value",
    label: _$t("value"),
    align: "left",
    field: "value",
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _headersColumnsVisibility = ref(["name", "value", "actions"]);
const _headersList = ref(_propsRef.value.modelValue.headers);

function _headerAddItemClick() {
  const list = _propsRef.value.modelValue.headers;
  const idList = list.map((v) => v.id);
  const maxId = (idList.length === 0 ? 0 : Math.max(...idList)) + 1;

  _headerDialogFormData.value = {
    id: maxId,
    name: "",
    value: "",
    isNew: true,
  };

  _headerDialogTitle.value = _$t("addHeader");
  _headerDialogVisibility.value = true;
}

function _headerEditItemClick(row) {
  _headerDialogFormData.value = Object.assign({}, row);
  _headerDialogTitle.value = _$t("editHeader");
  _headerDialogVisibility.value = true;
}

function _headerDeleteItemClick(row) {
  const index = _propsRef.value.modelValue.headers.findIndex(
    (i) => i.id === row.id
  );

  _$q
    .dialog({
      title: _$t("osRobot"),
      message: _$t("doYouWantToDeleteItem"),
      cancel: true,
      persistent: true,
    })
    .onOk(() => {
      if (index >= 0) {
        _propsRef.value.modelValue.headers.splice(index, 1);
      }
    });
}

function _headerItemMoveUpClick(row) {
  const list = _propsRef.value.modelValue.headers;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex > 0) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex - 1, 0, item);
  }
}

function _headerItemMoveDownClick(row) {
  const list = _propsRef.value.modelValue.headers;
  const itemIndex = list.findIndex((r) => r.id == row.id);

  if (itemIndex !== -1 && itemIndex < list.length - 1) {
    const item = list.splice(itemIndex, 1)[0];
    list.splice(itemIndex + 1, 0, item);
  }
}

// Dialog management
const _headerDialogVisibility = ref(false);
const _headerDialogFormData = ref({});
const _headerDialogTitle = ref(_$t("addHeader"));

function _headerDialogFormSubmit() {
  if (_headerDialogFormData.value.isNew) {
    _headerDialogFormData.value.isNew = false;
    _propsRef.value.modelValue.headers.push(_headerDialogFormData.value);
  } else {
    const index = _propsRef.value.modelValue.headers.findIndex(
      (i) => i.id === _headerDialogFormData.value.id
    );

    _propsRef.value.modelValue.headers[index] = _headerDialogFormData.value;
  }

  _headerDialogVisibility.value = false;
}
</script>
