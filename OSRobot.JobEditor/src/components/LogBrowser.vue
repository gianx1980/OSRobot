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
              :pagination="_logListpagination"
              style="height: 400px"
              dense
            >
              <template v-slot:body-cell-actions="props">
                <q-td :props="props">
                  <q-btn
                    square
                    class="q-ml-xs"
                    size="xs"
                    icon="search"
                    color="primary"
                    @click="_showLogContent(props.row)"
                  />
                </q-td>
              </template>
            </q-table>
          </div>
        </div>
      </q-card-section>
      <q-card-actions align="right">
        <div>
          <q-btn
            square
            class="q-ml-xs"
            icon="refresh"
            color="primary"
            :label="_$t('refresh')"
            @click="_loadLogList(_props.folderId)"
          />
        </div>
      </q-card-actions>
    </q-card>
  </div>
</template>
<script setup>
import LogViewer from "src/components/LogViewer.vue";
import { ref, watch } from "vue";
import { useQuasar } from "quasar";
import { useAppStore } from "src/stores/appStore.js";
import { useI18n } from "vue-i18n";
import Robot from "src/infrastructure/server/Robot.js";

const _$q = useQuasar();

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

const _logListpagination = {
  rowsPerPage: 50,
  sortBy: "execDateTime",
  descending: true,
};

watch(
  () => _props.folderId,
  async (newFolderId) => {
    _loadLogList(newFolderId);
  }
);

async function _loadLogList(folderId) {
  const robot = new Robot(_user.token, _user.refreshToken);

  const folderLogsResponse = await robot.getFolderLogs(folderId);
  _logList.value = folderLogsResponse.responseObject;
}

function _showLogContent(row) {
  _$q
    .dialog({
      component: LogViewer,
      componentProps: {
        cancel: true,
        persistent: true,
        folderId: _props.folderId,
        logFileName: row.fileName,
      },
    })
    .onOk((ev) => {});
}
</script>
