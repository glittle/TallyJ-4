import axios from "axios";
import { getAppConfig } from "../config/appConfig";
import { i18n } from "../locales";
import { router } from "../router/router";
import { cacheService } from "./cacheService";
import { secureTokenService } from "./secureTokenService";
import { tokenRefreshService } from "./tokenRefreshService";
import { ElMessage } from "element-plus";

// Create axios instance lazily to ensure config is available
let api: ReturnType<typeof axios.create>;

function getApiInstance() {
  if (!api) {
    const config = getAppConfig();
    api = axios.create({
      baseURL: config.apiUrl,
      withCredentials: true,
      headers: {
        "Content-Type": "application/json",
      },
    });

    // Set up interceptors only when the instance is first created
    setupInterceptors(api);
  }
  return api;
}

let redirectingFor401 = false;

function setupInterceptors(apiInstance: ReturnType<typeof axios.create>) {
  // Request interceptor for caching
  apiInstance.interceptors.request.use(async (config) => {
  // No need for Authorization header - credentials are sent via httpOnly cookies

  // Only cache GET requests
  if (config.method?.toUpperCase() === "GET") {
    const cacheKey = cacheService.generateKey(config);
    const cachedData = await cacheService.get(cacheKey);

    if (cachedData) {
      // Return cached data by creating a resolved promise
      config.adapter = () =>
        Promise.resolve({
          data: cachedData,
          status: 200,
          statusText: "OK",
          headers: {},
          config,
          request: {},
        } as any);
    }

    // Store cache key for response interceptor
    (config as any)._cacheKey = cacheKey;
  }

  return config;
});

  // Response interceptor for caching
  apiInstance.interceptors.response.use(
  async (response) => {
    // Cache successful GET responses
    if (
      response.config.method?.toUpperCase() === "GET" &&
      response.status === 200
    ) {
      const cacheKey = (response.config as any)._cacheKey;
      if (cacheKey) {
        // Cache for 5 minutes for most data, 1 minute for dynamic data
        const ttl =
          response.config.url?.includes("/results/") ||
          response.config.url?.includes("/monitor")
            ? 60000
            : 300000;
        await cacheService.set(cacheKey, response.data, ttl);
      }
    }
    return response;
  },
  async (error) => {
    // Handle 401 Unauthorized - try to refresh token once, then redirect to login
    if (error.response?.status === 401 && !redirectingFor401) {
      // Don't try to refresh if this is already the refresh endpoint
      if (!error.config?.url?.includes("/refreshToken")) {
        try {
          // Try to refresh token (uses shared promise to prevent concurrent refreshes)
          const refreshed = await tokenRefreshService.refreshToken();

          if (refreshed) {
            // Retry the original request
            return getApiInstance().request(error.config);
          }
        } catch (refreshError) {
          console.error("Token refresh failed:", refreshError);
        }
      }

      // If refresh failed or this is the refresh endpoint, redirect to login
      redirectingFor401 = true;
      tokenRefreshService.stopAutoRefresh();
      secureTokenService.clearAuthData();
      const { t } = i18n.global;
      ElMessage({
        message: t("error.sessionExpired"),
        type: "warning",
        duration: 0,
        showClose: true,
      });
      router.push("/");
      setTimeout(() => {
        redirectingFor401 = false;
      }, 2000);
      return Promise.reject(error);
    }

    if (error.config?.method?.toUpperCase() === "GET") {
      const cacheKey = (error.config as any)._cacheKey;
      if (cacheKey) {
        const cachedData = await cacheService.get(cacheKey);
        if (cachedData) {
          return {
            data: cachedData,
            status: 200,
            statusText: "OK (cached)",
            headers: {},
            config: error.config,
            request: error.request,
          };
        }
      }
    }
    return Promise.reject(error);
  },
  );
}

// Export a proxy that lazily initializes the API instance
export default new Proxy({} as ReturnType<typeof axios.create>, {
  get(_target, prop) {
    const instance = getApiInstance();
    return (instance as any)[prop];
  },
});
