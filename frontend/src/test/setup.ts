import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
import { createI18n } from "vue-i18n";
import { createAppConfig, setAppConfig } from "../config/appConfig";

setAppConfig(
  createAppConfig({
    env: "test",
    sentryDsn: "",
  }),
);

const testPinia = createPinia();
const testRouter = createRouter({
  history: createWebHistory(),
  routes: [],
});
const testI18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {},
});

export { testPinia as pinia, testRouter as router, testI18n as i18n };
