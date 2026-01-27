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

// Global error handler for unhandled errors
app.config.errorHandler = (error, instance, info) => {
  console.error('Global error handler:', error, instance, info);
  // Could send to error reporting service here
};

// Handle unhandled promise rejections
window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason);
  // Prevent the default browser behavior (logging to console)
  event.preventDefault();
});

// Handle uncaught errors
window.addEventListener('error', (event) => {
  console.error('Uncaught error:', event.error);
});

app.mount("#app");
