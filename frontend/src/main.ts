import { createApp } from "vue";
import { createPinia } from "pinia";

import ElementPlus from "element-plus";
import "./style.css";

import App from "./App.vue";
import { router } from "./router/router";
import { i18n } from "./locales";

const pinia = createPinia();

const app = createApp(App);
app.use(ElementPlus);
app.use(pinia);
app.use(router);
app.use(i18n);

app.mount("#app");
