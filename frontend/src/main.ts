import { createPinia } from "pinia";
import { createApp } from "vue";

import ElementPlus from "element-plus";
import "element-plus/dist/index.css";
import "./style.css";

import App from "./App.vue";
import { loadAppConfig, setAppConfig } from "./config/appConfig";
import { TOKEN_REFRESH_CONFIG } from "./config/tokenRefreshConfig";
import { i18n } from "./locales";
import { router } from "./router/router";
import { secureTokenService } from "./services/secureTokenService";
import { tokenRefreshService } from "./services/tokenRefreshService";

// Sentry error tracking and performance monitoring
import * as Sentry from "@sentry/vue";

async function init() {
  const config = await loadAppConfig();
  setAppConfig(config);

  const { initApiClient } = await import("./api/config");
  initApiClient();

  const pinia = createPinia();

  const app = createApp(App);

  app.use(ElementPlus);
  app.use(pinia);
  app.use(router);
  app.use(i18n);

  Sentry.init({
    app,
    dsn:
      config.sentryDsn ||
      "https://placeholder@example.ingest.sentry.io/placeholder",
    environment: config.env || "production",
    integrations: [
      Sentry.browserTracingIntegration({ router }),
      Sentry.replayIntegration(),
    ],
    tracesSampleRate: config.env === "development" ? 1 : 0.1,
    replaysSessionSampleRate: config.env === "development" ? 1 : 0.1,
    replaysOnErrorSampleRate: 1,
  });

  app.config.errorHandler = (error, instance, info) => {
    console.error("Global error handler:", error, instance, info);
  };

  globalThis.addEventListener("unhandledrejection", (event) => {
    console.error("Unhandled promise rejection:", event.reason);
    event.preventDefault();
  });

  globalThis.addEventListener("error", (event) => {
    if (event.message?.startsWith("ResizeObserver loop")) {
      return;
    }
    console.error("Uncaught error:", event.error);
  });

  if (secureTokenService.isAuthenticated()) {
    tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);
  }

  app.mount("#app");
}

init(); // NOSONAR
