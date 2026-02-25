import { client } from "./gen/configService/client.gen";
import { secureTokenService } from "../services/secureTokenService";
import { tokenRefreshService } from "../services/tokenRefreshService";
import { router } from "../router/router";
import { i18n } from "../locales";

import { useNotifications } from "../composables/useNotifications";
const { showWarningMessage } = useNotifications();

client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL || "http://localhost:5016",
  throwOnError: true,
  credentials: "include",
});

let redirectingFor401 = false;

client.interceptors.response.use(async (response) => {
  if (response.status === 401 && !redirectingFor401) {
    // Don't try to refresh if this is already the refresh endpoint
    if (!response.url?.includes("/refreshToken")) {
      try {
        // Try to refresh token (uses shared promise to prevent concurrent refreshes)
        const refreshed = await tokenRefreshService.refreshToken();

        if (refreshed) {
          // Token refreshed successfully - the user should retry their request
          // Note: The generated client doesn't support automatic request retry,
          // so the calling code will need to handle this
          console.log(
            "[API Config] Token refreshed successfully. Please retry your request.",
          );
        }
      } catch (refreshError) {
        console.error("[API Config] Token refresh failed:", refreshError);
      }
    }

    // If refresh failed or this is the refresh endpoint, redirect to login
    redirectingFor401 = true;
    tokenRefreshService.stopAutoRefresh();
    secureTokenService.clearAuthData();
    const { t } = i18n.global;
    showWarningMessage(t("error.sessionExpired"));
    router.push("/login");
    setTimeout(() => {
      redirectingFor401 = false;
    }, 2000);
  }
  return response;
});

export { client };
