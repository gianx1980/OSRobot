<script setup>
import { BaseEdge, EdgeLabelRenderer, getBezierPath } from "@vue-flow/core";
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const _i18n = useI18n();
const _$t = _i18n.t;

const _emit = defineEmits(["deleteEdgeClick"]);
const _props = defineProps({
  id: {
    type: String,
    required: true,
  },
  sourceX: {
    type: Number,
    required: true,
  },
  sourceY: {
    type: Number,
    required: true,
  },
  targetX: {
    type: Number,
    required: true,
  },
  targetY: {
    type: Number,
    required: true,
  },
  sourcePosition: {
    type: String,
    required: true,
  },
  targetPosition: {
    type: String,
    required: true,
  },
  markerEnd: {
    type: String,
    required: false,
  },
  style: {
    type: Object,
    required: false,
  },
});

function _delete(ev) {
  const eventData = {
    id: _props.id,
  };

  _emit("deleteEdgeClick", eventData);
}

const _path = computed(() => getBezierPath(_props));
</script>

<script>
export default {
  inheritAttrs: false,
};
</script>

<template>
  <!-- You can use the `BaseEdge` component to create your own custom edge more easily -->
  <BaseEdge :id="id" :style="style" :path="_path[0]" :marker-end="markerEnd" />

  <!-- Use the `EdgeLabelRenderer` to escape the SVG world of edges and render your own custom label in a `<div>` ctx -->
  <EdgeLabelRenderer>
    <div
      :style="{
        pointerEvents: 'all',
        position: 'absolute',
        transform: `translate(-50%, -50%) translate(${_path[1]}px,${_path[2]}px)`,
      }"
      class="nodrag nopan"
    >
      <q-btn
        color="red-14"
        flat
        size="md"
        icon="delete_forever"
        @click.stop="_delete"
      >
        <q-tooltip>{{ _$t("delete") }}</q-tooltip>
      </q-btn>
    </div>
  </EdgeLabelRenderer>
</template>

<style>
.vue-flow__edge.selected .vue-flow__edge-path,
.vue-flow__edge:focus .vue-flow__edge-path,
.vue-flow__edge:focus-visible .vue-flow__edge-path {
  stroke: darkblue;
}
</style>
