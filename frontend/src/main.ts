import { createApp } from "vue";
import { createPinia } from "pinia";

import ElementPlus from "element-plus";
import "./style.css";

import App from "./App.vue";
import { router } from "./router/router";

const pinia = createPinia();

const app = createApp(App);
app.use(ElementPlus);
app.use(pinia);
app.use(router);

app.mount("#app");
