import { getAppConfig } from "../config/appConfig";
import { i18n } from "../locales";
import { router } from "../router/router";
import { secureTokenService } from "../services/secureTokenService";
import { tokenRefreshService } from "../services/tokenRefreshService";
import { client } from "./gen/configService/client.gen";

import { useNotifications } from "../composables/useNotifications";

let redirectingFor401 = false;

export function initApiClient() {
  const { showWarningMessage } = useNotifications();
  const config = getAppConfig();

  client.setConfig({
    baseUrl: config.apiUrl,
    throwOnError: true,
    credentials: "include",
  });

  client.interceptors.response.use(async (response) => {
    if (response.status === 401 && !redirectingFor401) {
      if (!response.url?.includes("/refreshToken")) {
        try {
          const refreshed = await tokenRefreshService.refreshToken();

          if (refreshed) {
            console.log(
              "[API Config] Token refreshed successfully. Please retry your request.",
            );
          }
        } catch (refreshError) {
          console.error("[API Config] Token refresh failed:", refreshError);
        }
      }

      redirectingFor401 = true;
      tokenRefreshService.stopAutoRefresh();
      secureTokenService.clearAuthData();
      const t = (i18n.global as any).t as (key: string) => string;
      showWarningMessage(t("error.sessionExpired"));
      router.push("/");
      setTimeout(() => {
        redirectingFor401 = false;
      }, 2000);
    }
    return response;
  });
}

export { client };
