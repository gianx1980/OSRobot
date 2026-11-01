<!-- SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com> -->
<!-- SPDX-License-Identifier: GPL-3.0-or-later -->

<script setup>
import { Position, Handle } from "@vue-flow/core";
import { useI18n } from "vue-i18n";

const _i18n = useI18n();
const _$t = _i18n.t;

const _emit = defineEmits(["deleteNodeClick", "folderOpen", "taskExecute"]);
const _props = defineProps(["id", "data"]);

function _dblclick(ev) {
  if (_props.data.type !== "folder") return;

  const eventData = {
    id: _props.id,
    data: _props.data,
  };

  _emit("folderOpen", eventData);
}

function _taskExecute(ev) {
  const eventData = {
    id: _props.id,
    data: _props.data,
  };

  _emit("taskExecute", eventData);
}

function _delete(ev) {
  const eventData = {
    id: _props.id,
    data: _props.data,
  };

  _emit("deleteNodeClick", eventData);
}
</script>

<template>
  <div>
    <Handle
      type="target"
      :position="Position.Left"
      v-if="_props.data.type === 'task'"
    />

    <q-card
      class="my-card"
      :class="{ 'node-disabled': _props.data.enabled === false }"
      @dblclick.stop="_dblclick"
    >
      <div class="row">
        <div class="col">
          <q-card-section class="section_info" align="left">
            <i
              class="q-icon notranslate material-icons q-tree__icon q-mr-sm"
              aria-hidden="true"
              role="presentation"
              >{{ _props.data.icon }}
              <q-tooltip>{{ _props.data.pluginTitle }}</q-tooltip>
            </i>
            <i
              v-if="_props.data.enabled === false"
              class="q-icon notranslate material-icons q-tree__icon q-mr-sm"
              aria-hidden="true"
              role="presentation"
              >pause_circle
              <q-tooltip>{{ _$t("disabled") }}</q-tooltip>
            </i>
            <i
              v-if="_props.data.type !== 'folder'"
              class="q-icon notranslate material-icons q-tree__icon q-mr-sm"
              aria-hidden="true"
              role="presentation"
              >computer
              <q-tooltip
                >{{ _$t("compatibility") }}:
                <li
                  v-for="osPlatform in _props.data.supportedOSPlatformList"
                  :key="osPlatform"
                >
                  {{ osPlatform }}
                </li>
              </q-tooltip>
            </i>
          </q-card-section>
        </div>
        <div class="col">
          <q-card-actions align="right">
            <q-btn
              v-if="_props.data.type === 'task'"
              color="red-14"
              flat
              size="12px"
              icon="bolt"
              @click.stop="_taskExecute"
            >
              <q-tooltip>{{ _$t("execute") }}</q-tooltip>
            </q-btn>
            <q-btn
              color="red-14"
              flat
              size="12px"
              icon="delete_forever"
              @click.stop="_delete"
            >
              <q-tooltip>{{ _$t("delete") }}</q-tooltip>
            </q-btn>
          </q-card-actions>
        </div>
      </div>

      <q-card-section>
        <q-item-label
          :class="{ 'text-strike': _props.data.enabled === false }"
          >{{ _props.data.label }}</q-item-label
        >
      </q-card-section>
    </q-card>

    <Handle
      type="source"
      :position="Position.Right"
      v-if="_props.data.type === 'task' || _props.data.type === 'event'"
    />
  </div>
</template>

<style scoped>
.my-card .q-card__section--vert {
  padding: 2px 2px 2px 6px;
}

.my-card .q-card__actions {
  padding: 0;
}

/* Disabled: never runs (event) or is skipped by its connections (task), or a disabled folder. */
.my-card.node-disabled {
  opacity: 0.55;
  filter: grayscale(1);
}
</style>

<style>
.vue-flow__node-event,
.vue-flow__node-task,
.vue-flow__node-folder {
  font-size: 13px;
  border-radius: 3px;
  width: 200px;
  text-align: center;
  border-width: 1px;
  border-style: solid;
  color: var(--vf-node-text);
  background-color: var(--vf-node-bg);
  border-color: var(--vf-node-color);
}

.vue-flow__node.selected {
  border-color: darkblue;
  border-width: 2px;
}
</style>
