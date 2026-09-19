import { route } from "quasar/wrappers";
import {
  createRouter,
  createMemoryHistory,
  createWebHistory,
  createWebHashHistory,
} from "vue-router";
import routes from "./routes";
import { useAppStore } from "../stores/appStore";

/*
 * If not building with SSR mode, you can
 * directly export the Router instantiation;
 *
 * The function below can be async too; either use
 * async/await or return a Promise which resolves
 * with the Router instance.
 */

export default route(function (/* { store, ssrContext } */) {
  const createHistory = process.env.SERVER
    ? createMemoryHistory
    : process.env.VUE_ROUTER_MODE === "history"
    ? createWebHistory
    : createWebHashHistory;

  const Router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    routes,

    // Leave this as is and make changes in quasar.conf.js instead!
    // quasar.conf.js -> build -> vueRouterMode
    // quasar.conf.js -> build -> publicPath
    history: createHistory(process.env.VUE_ROUTER_BASE),
  });

  Router.beforeEach(async (to, from) => {
    const appStore = useAppStore();
    const user = appStore.getLoggedUser();

    // Only login page can be accessed without authentication
    if (user === null && to.name !== "Login") return { name: "Login" };

    // A user carrying a forced/default password must change it before reaching anything else -
    // the server enforces this too (see MustChangePasswordFilter on the backend); this is just
    // what keeps the UI from bouncing off a 403 to get there.
    if (
      user !== null &&
      user.mustChangePassword &&
      to.name !== "ChangePassword" &&
      to.name !== "Logout"
    )
      return { name: "ChangePassword" };

    // Nothing to do on the change-password page once it's no longer required.
    if (user !== null && !user.mustChangePassword && to.name === "ChangePassword")
      return { name: "Home" };
  });

  return Router;
});
