<template>
  <div>
    <transition-group
      appear
      enter-active-class="animated fadeIn"
      leave-active-class="animated fadeOut"
    >
      <q-banner
        v-for="banner in _banners"
        inline-actions
        rounded
        :class="_getBannerClass(banner.type)"
        style="
          z-index: 9999999;
          width: 70%;
          margin-left: auto;
          margin-right: auto;
        "
        :key="banner"
      >
        {{ banner.message }}
        <template v-slot:action>
          <q-btn
            flat
            :color="banner.buttonColor"
            :label="_$t('dismiss')"
            @click="_dismissClick(banner.id)"
          />
        </template>
      </q-banner>
    </transition-group>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";

const _bannerDuration = 4000;
const _props = defineProps(["banners"]);
const _formData = ref(_props);
const _banners = ref([]);

const _i18n = useI18n();
const _$t = _i18n.t;

let _lastId = 0;

function _dismissClick(id) {
  const index = _banners.value.findIndex((t) => t.id === id);
  if (index === -1) return;
  _banners.value.splice(index, 1);
}

function _getBannerClass(bannerType) {
  switch (bannerType) {
    case "MessageOk":
      return "bg-green text-white fixed-top q-mt-sm";

    case "MessageError":
      return "bg-red text-white fixed-top q-mt-sm";

    case "MessageInfo":
    default:
      return "bg-primary text-white fixed-top q-mt-sm";
  }
}

function addBanner(bannerInfo) {
  bannerInfo.id = ++_lastId;
  _banners.value.push(bannerInfo);

  setTimeout(() => {
    const index = _banners.value.findIndex((t) => t.id === bannerInfo.id);
    if (index === -1) return;
    _banners.value.splice(index, 1);
  }, _bannerDuration);
}

defineExpose({ addBanner });
</script>
