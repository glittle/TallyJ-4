export interface AppConfig {
  apiUrl: string;
  env: string;
  sentryDsn?: string;
  googleClientId?: string;
  facebookAppId?: string;
  kakaoApiJsKey?: string;
  enableTelegramLogin?: boolean;
  telegramBotUsername?: string;
}

export function createAppConfig(overrides: Partial<AppConfig> = {}): AppConfig {
  return {
    // Dev defaults to the Vite HTTPS origin so /api and /hubs are same-origin (proxied).
    apiUrl: import.meta.env.VITE_API_URL || "https://localhost:8095",
    env: import.meta.env.VITE_ENV || "development",
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

  const response = await fetch("/clientEnv.json", {
    headers: {
      "Cache-Control": "no-cache",
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to load /clientEnv.json (${response.status})`);
  }

  return (await response.json()) as AppConfig;
}

export function setAppConfig(config: AppConfig): void {
  window.__APP_CONFIG__ = config;
}

export function getAppConfig(): AppConfig {
  return window.__APP_CONFIG__;
}
