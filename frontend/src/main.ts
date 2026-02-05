import { createApp } from "vue";
import { createPinia } from "pinia";

import ElementPlus from "element-plus";
import "element-plus/dist/index.css";
import "./style.css";

import App from "./App.vue";
import { router } from "./router/router";
import "./api/config";
import { i18n } from "./locales";

// Sentry error tracking and performance monitoring
import * as Sentry from "@sentry/vue";

const pinia = createPinia();

const app = createApp(App);

app.use(ElementPlus);
app.use(pinia);
app.use(router);
app.use(i18n);

// Initialize Sentry for error tracking and performance monitoring
Sentry.init({
  app,
  dsn:
    import.meta.env.VITE_SENTRY_DSN ||
    "https://placeholder@example.ingest.sentry.io/placeholder",
  environment: import.meta.env.MODE || "development",
  integrations: [
    Sentry.browserTracingIntegration({ router }),
    Sentry.replayIntegration(),
  ],
  // Performance Monitoring
  tracesSampleRate: import.meta.env.PROD ? 0.1 : 1.0, // 10% in production, 100% in development
  // Session Replay
  replaysSessionSampleRate: import.meta.env.PROD ? 0.1 : 1.0,
  replaysOnErrorSampleRate: 1.0,
});

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
// if ("serviceWorker" in navigator) {
//   window.addEventListener("load", () => {
//     navigator.serviceWorker
//       .register("/sw.js")
//       .then((registration) => {
//         console.log(
//           "Service Worker registered successfully:",
//           registration.scope,
//         );
//       })
//       .catch((error) => {
//         console.log("Service Worker registration failed:", error);
//       });
//   });
// }

// router.isReady().then(() =>
app.mount("#app");
