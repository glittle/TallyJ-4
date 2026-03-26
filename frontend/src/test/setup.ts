import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
import { setAppConfig } from "../config/appConfig";

setAppConfig({
  apiUrl: "http://localhost:5016",
  env: "development",
  sentryDsn: "",
});

const testPinia = createPinia();
const testRouter = createRouter({
  history: createWebHistory(),
  routes: [],
});

export { testPinia as pinia, testRouter as router };
