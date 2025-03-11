<template>
  <div class="q-pa-md">
    <q-card class="q-mt-sm q-mb-sm">
      <q-card-section>
        <div class="text-h6">{{ _$t("logs") }}</div>
      </q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <q-table
              :columns="_logItemColumnsDef"
              :rows="_logList"
              :visible-columns="_logColumnVisibility"
              :no-data-label="_$t('thereAreNoItemsToShow')"
              row-key="fileName"
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
                    @click="_showLogDetail"
                  />
                </q-td>
              </template>
            </q-table>
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>
<script setup>
import { ref, onMounted, watch } from "vue";
import { useAppStore } from "src/stores/appStore.js";
import { useI18n } from "vue-i18n";
import Robot from "src/infrastructure/server/Robot.js";

const _i18n = useI18n();
const _$t = _i18n.t;

const _appStore = useAppStore();
const _user = _appStore.getLoggedUser();

const _props = defineProps(["folderId"]);

const _logItemColumnsDef = [
  {
    name: "fileName",
    field: "fileName",
  },
  {
    name: "eventId",
    label: _$t("eventId"),
    align: "left",
    field: "eventId",
    sortable: true,
  },
  {
    name: "execDateTime",
    label: _$t("startDate"),
    align: "left",
    field: "execDateTime",
    sortable: true,
  },
  {
    name: "actions",
    align: "center",
    field: "actions",
  },
];

const _logColumnVisibility = ref(["eventId", "execDateTime", "actions"]);

const _logList = ref([]);

watch(
  () => _props.folderId,
  async (newFolderId) => {
    const robot = new Robot(_user.token, _user.refreshToken);

    const folderLogs = await robot.getFolderLogs(newFolderId);
    _logList.value = folderLogs.responseObject;
  }
);

function _showLogDetail() {}
</script>
