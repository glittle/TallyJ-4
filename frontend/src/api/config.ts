import { client } from './gen/configService/client.gen';
import { secureTokenService } from '../services/secureTokenService';
import { tokenRefreshService } from '../services/tokenRefreshService';
import { router } from '../router/router';
import { i18n } from '../locales';
import { ElMessage } from 'element-plus';

client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  throwOnError: true,
  credentials: 'include',
});

let redirectingFor401 = false;
let isRefreshingToken = false;

client.interceptors.response.use(async (response) => {
  if (response.status === 401 && !redirectingFor401) {
    // Don't try to refresh if this is already the refresh endpoint
    if (!response.url?.includes('/refreshToken')) {
      // Try to refresh token once
      if (!isRefreshingToken) {
        isRefreshingToken = true;
        
        try {
          const refreshed = await tokenRefreshService.refreshToken();
          isRefreshingToken = false;
          
          if (refreshed) {
            // Retry the original request - would need to store and replay the request
            // For now, just let the app handle the 401
            console.log('Token refreshed, please retry your request');
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
  }
  return response;
});

export { client };
