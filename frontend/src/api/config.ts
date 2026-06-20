import { getAppConfig } from "../config/appConfig";
import { i18n } from "../locales";
import { router } from "../router/router";
import { secureTokenService } from "../services/secureTokenService";
import { tokenRefreshService } from "../services/tokenRefreshService";
import { client } from "./gen/configService/client.gen";

import { useNotifications } from "../composables/useNotifications";

let redirectingFor401 = false;
let showSessionExpiredWarning: ((message: string) => void) | null = null;
const AUTH_RETRY_HEADER = "X-TallyJ-Auth-Retry";

function handleSessionExpired(): void {
  redirectingFor401 = true;
  tokenRefreshService.stopAutoRefresh();
  secureTokenService.clearAuthData();
  const { t } = i18n.global;
  showSessionExpiredWarning?.(t("error.sessionExpired"));
  router.push("/");
  setTimeout(() => {
    redirectingFor401 = false;
  }, 2000);
}

export function initApiClient() {
  const { showWarningMessage } = useNotifications();
  showSessionExpiredWarning = showWarningMessage;
  const config = getAppConfig();

  client.setConfig({
    baseUrl: config.apiUrl,
    throwOnError: true,
    credentials: "include",
  });

  client.interceptors.response.use(async (response, request) => {
    if (response.status !== 401 || redirectingFor401) {
      return response;
    }

    if (
      response.url?.includes("/refreshToken") ||
      request.headers.get(AUTH_RETRY_HEADER) === "1"
    ) {
      handleSessionExpired();
      return response;
    }

    try {
      const refreshed = await tokenRefreshService.refreshToken();

      if (refreshed) {
        const retryHeaders = new Headers(request.headers);
        retryHeaders.set(AUTH_RETRY_HEADER, "1");
        const retryRequest = new Request(request, { headers: retryHeaders });
        const retryResponse = await fetch(retryRequest);

        if (retryResponse.status !== 401) {
          console.log(
            "[API Config] Token refreshed and original request retried successfully.",
          );
          return retryResponse;
        }
      }
    } catch (refreshError) {
      console.error("[API Config] Token refresh failed:", refreshError);
    }

    handleSessionExpired();
    return response;
  });
}

export { client };
