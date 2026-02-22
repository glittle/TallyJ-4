import axios from 'axios';
import { cacheService } from './cacheService';
import { secureTokenService } from './secureTokenService';
import { tokenRefreshService } from './tokenRefreshService';
import { router } from '../router/router';
import { i18n } from '../locales';
import { ElMessage } from 'element-plus';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json'
  }
});

let redirectingFor401 = false;
let isRefreshingToken = false;

// Request interceptor for caching
api.interceptors.request.use(async (config) => {
  // No need for Authorization header - credentials are sent via httpOnly cookies

  // Only cache GET requests
  if (config.method?.toUpperCase() === 'GET') {
    const cacheKey = cacheService.generateKey(config);
    const cachedData = await cacheService.get(cacheKey);

    if (cachedData) {
      // Return cached data by creating a resolved promise
      config.adapter = () => Promise.resolve({
        data: cachedData,
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
        request: {}
      } as any);
    }

    // Store cache key for response interceptor
    (config as any)._cacheKey = cacheKey;
  }

  return config;
});

// Response interceptor for caching
api.interceptors.response.use(
  async (response) => {
    // Cache successful GET responses
    if (response.config.method?.toUpperCase() === 'GET' && response.status === 200) {
      const cacheKey = (response.config as any)._cacheKey;
      if (cacheKey) {
        // Cache for 5 minutes for most data, 1 minute for dynamic data
        const ttl = response.config.url?.includes('/results/') ||
                   response.config.url?.includes('/monitor') ? 60000 : 300000;
        await cacheService.set(cacheKey, response.data, ttl);
      }
    }
    return response;
  },
  async (error) => {
    // Handle 401 Unauthorized - try to refresh token once, then redirect to login
    if (error.response?.status === 401 && !redirectingFor401) {
      // Don't try to refresh if this is already the refresh endpoint
      if (!error.config?.url?.includes('/refreshToken')) {
        // Try to refresh token once
        if (!isRefreshingToken) {
          isRefreshingToken = true;
          
          try {
            const refreshed = await tokenRefreshService.refreshToken();
            isRefreshingToken = false;
            
            if (refreshed) {
              // Retry the original request
              return api.request(error.config);
            }
          } catch (refreshError) {
            isRefreshingToken = false;
            console.error('Token refresh failed:', refreshError);
          }
        }
      }
      
      // If refresh failed or this is the refresh endpoint, redirect to login
      redirectingFor401 = true;
      tokenRefreshService.stopAutoRefresh();
      secureTokenService.clearAuthData();
      const { t } = i18n.global;
      ElMessage({ message: t('error.sessionExpired'), type: 'warning', duration: 0, showClose: true });
      router.push('/login');
      setTimeout(() => { redirectingFor401 = false; }, 2000);
      return Promise.reject(error);
    }

    if (error.config?.method?.toUpperCase() === 'GET') {
      const cacheKey = (error.config as any)._cacheKey;
      if (cacheKey) {
        const cachedData = await cacheService.get(cacheKey);
        if (cachedData) {
          return {
            data: cachedData,
            status: 200,
            statusText: 'OK (cached)',
            headers: {},
            config: error.config,
            request: error.request
          };
        }
      }
    }
    return Promise.reject(error);
  }
);

export default api;
