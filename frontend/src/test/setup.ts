import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
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

export { testPinia as pinia, testRouter as router };
