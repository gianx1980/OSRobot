<template>
  <q-layout view="hHh lpR fFf">

    <q-header elevated class="bg-primary text-white" height-hint="98">
      <q-toolbar>
        <q-btn dense flat round icon="menu" @click="toggleLeftDrawer" />

        <q-toolbar-title>
          OpenRobot Job Editor
        </q-toolbar-title>
      </q-toolbar>
    </q-header>

    <q-drawer show-if-above v-model="leftDrawerOpen" side="left" bordered class="bg-grey-3" :width="200">
      <q-scroll-area class="fit">
        <q-list>
          <template v-for="(menuItem, index) in menuList" :key="index">
            <q-item
              clickable
              :active="menuItem.label === 'Outbox'"
              v-ripple
              @click="menuClicked(menuItem.id, menuItem.url)"
            >
              <q-item-section avatar>
                <q-icon :name="menuItem.icon" />
              </q-item-section>
              <q-item-section>
                {{ menuItem.label }}
              </q-item-section>
            </q-item>
            <q-separator :key="'sep' + index" v-if="menuItem.separator" />
          </template>
        </q-list>
      </q-scroll-area>
    </q-drawer>

    <q-page-container>
      <div class="row justify-center">
        <div class="col-2">First column</div>
        <div class="col-8">Second column</div>
        <div class="col-2">Third column</div>
      </div>
      <!--<router-view />-->
    </q-page-container>

    <q-footer elevated class="bg-grey-8 text-white">
      <q-toolbar>
        <q-toolbar-title>
          <div>Title</div>
        </q-toolbar-title>
      </q-toolbar>
    </q-footer>

  </q-layout>
</template>

<script setup>
import { ref } from 'vue'
//import ButtonCounter from 'https://localhost:7179/Plugin/GetComponent'
const leftDrawerOpen = ref(false)
const splitterModel = ref(50)

const menuList = [
  {
    id: 'mnuWorkspace',
    icon: 'workspaces',
    label: 'Workspace',
    url: '/workspace',
    separator: true,
  },
  {
    id: 'mnuLogout',
    icon: 'logout',
    label: 'Logout',
    url: '/logout',
    separator: true,
  },
  {
    id: 'mnuHelp',
    icon: 'help',
    iconColor: 'primary',
    label: 'Help',
    url: '/help',
    separator: false,
  },
];

function toggleLeftDrawer() {
  leftDrawerOpen.value = !leftDrawerOpen.value
}
</script>
