const routes = [
  {
    name: "Login",
    path: "/login",
    component: () => import("pages/LoginPage.vue"),
  },

  {
    name: "Logout",
    path: "/logout",
    component: () => import("pages/LogoutPage.vue"),
  },

  {
    path: "/",
    component: () => import("layouts/MainLayout.vue"),
    children: [{ path: "", component: () => import("pages/IndexPage.vue") }],
  },

  // Always leave this as last one,
  // but you can also remove it
  {
    path: "/:catchAll(.*)*",
    component: () => import("pages/ErrorNotFound.vue"),
  },
];

export default routes;
