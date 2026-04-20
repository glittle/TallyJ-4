export interface AppConfig {
  apiUrl: string;
  env: string;
  sentryDsn: string;
  googleClientId?: string;
}

export function createAppConfig(overrides: Partial<AppConfig> = {}): AppConfig {
  return {
    apiUrl: import.meta.env.VITE_API_URL || "http://localhost:5016",
    env: import.meta.env.VITE_ENV || "development",
    sentryDsn: import.meta.env.VITE_SENTRY_DSN || "",
    googleClientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || undefined,
    ...overrides,
  };
}

declare global {
  interface Window {
    __APP_CONFIG__: AppConfig;
  }
}

export async function loadAppConfig(): Promise<AppConfig> {
  if (import.meta.env.DEV) {
    return createAppConfig();
  }

  const response = await fetch("/config.json");
  return (await response.json()) as AppConfig;
}

export function setAppConfig(config: AppConfig): void {
  window.__APP_CONFIG__ = config;
}

export function getAppConfig(): AppConfig {
  return window.__APP_CONFIG__;
}
