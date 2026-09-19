import { boot } from 'quasar/wrappers'
import axios from 'axios'
import { useAppStore } from 'src/stores/appStore.js'
import { Account } from 'src/infrastructure/server/Account.js'

// Be careful when using SSR for cross-request state pollution
// due to creating a Singleton instance here;
// If any client changes this (global) instance, it might be a
// good idea to move this instance creation inside of the
// "export default () => {}" function below (which runs individually
// for each client)
const api = axios.create({ baseURL: AppConfig.apiUrl })

export default boot(({ app, router }) => {
  // for use inside Vue files (Options API) through this.$axios and this.$api

  app.config.globalProperties.$axios = axios
  // ^ ^ ^ this will allow you to use this.$axios (for Vue Options API form)
  //       so you won't necessarily have to import axios in each vue file

  app.config.globalProperties.$api = api
  // ^ ^ ^ this will allow you to use this.$api (for Vue Options API form)
  //       so you can easily perform requests against your app's API

  // Defense in depth: LoginPage/the router guard already redirect a user with
  // mustChangePassword straight to the change-password page, but if the client's local state is
  // ever out of sync with the server (e.g. a stale tab), any *other* API call still gets refused
  // with 403 MustChangePassword by the backend (see MustChangePasswordFilter). Catch that here,
  // in one place, rather than in every service class.
  api.interceptors.response.use(
    (response) => response,
    (error) => {
      const responseCode = error.response?.data?.responseCode
      if (error.response?.status === 403 && responseCode === Account.MustChangePassword) {
        const appStore = useAppStore()
        const user = appStore.getLoggedUser()
        if (user) {
          user.mustChangePassword = true
          appStore.setLoggedUser(user)
        }
        if (router.currentRoute.value.name !== 'ChangePassword') {
          router.push({ name: 'ChangePassword' })
        }
      }

      return Promise.reject(error)
    }
  )
})

export { api }
