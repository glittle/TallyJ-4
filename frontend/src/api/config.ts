import { client } from './gen/configService/client.gen';
import { secureTokenService } from '../services/secureTokenService';
import { router } from '../router/router';
import { i18n } from '../locales';
import { ElMessage } from 'element-plus';

client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  throwOnError: true,
  credentials: 'include',
});

let redirectingFor401 = false;

client.interceptors.response.use((response) => {
  if (response.status === 401 && !redirectingFor401) {
    redirectingFor401 = true;
    secureTokenService.clearAuthData();
    const { t } = i18n.global;
    ElMessage({ message: t('error.sessionExpired'), type: 'warning', duration: 0, showClose: true });
    router.push('/login');
    setTimeout(() => { redirectingFor401 = false; }, 2000);
  }
  return response;
});

export { client };
