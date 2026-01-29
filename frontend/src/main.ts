import { createApp } from "vue";
import { createPinia } from "pinia";

import ElementPlus from "element-plus";
import "element-plus/dist/index.css";
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
  console.error("Global error handler:", error, instance, info);
  // Could send to error reporting service here
};

// Handle unhandled promise rejections
globalThis.addEventListener("unhandledrejection", (event) => {
  console.error("Unhandled promise rejection:", event.reason);
  // Prevent the default browser behavior (logging to console)
  event.preventDefault();
});

// Handle uncaught errors
globalThis.addEventListener("error", (event) => {
  console.error("Uncaught error:", event.error);
});

// Register service worker for offline support
if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker
      .register("/sw.js")
      .then((registration) => {
        console.log(
          "Service Worker registered successfully:",
          registration.scope,
        );
      })
      .catch((error) => {
        console.log("Service Worker registration failed:", error);
      });
  });
}

app.mount("#app");
