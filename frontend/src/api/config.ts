import { client } from './gen/configService/client.gen';

// Configure API client with base URL and auth
client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  throwOnError: true,
  auth: () => {
    return localStorage.getItem('auth_token') || undefined;
  },
});

export { client };
