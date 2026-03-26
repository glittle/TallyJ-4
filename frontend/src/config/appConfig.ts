export interface AppConfig {
  apiUrl: string;
  env: string;
  sentryDsn: string;
  googleClientId?: string;
}

declare global {
  interface Window {
    __APP_CONFIG__: AppConfig;
  }
}

export function setAppConfig(config: AppConfig): void {
  window.__APP_CONFIG__ = config;
}

export function getAppConfig(): AppConfig {
  return window.__APP_CONFIG__;
}
