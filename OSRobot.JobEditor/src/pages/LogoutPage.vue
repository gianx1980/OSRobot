<template>
  <h6>{{ _$t("loggingOut") }}...</h6>
</template>
<script setup>
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAppStore } from "../stores/appStore.js";

const _i18n = useI18n();
const _$t = _i18n.t;
const _appStore = useAppStore();
const _router = useRouter();

_appStore.logOut();
_router.push("/login");
</script>
