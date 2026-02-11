import axios from 'axios';
import { cacheService } from './cacheService';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  withCredentials: true, // Send httpOnly cookies automatically
  headers: {
    'Content-Type': 'application/json'
  }
});

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
    // If request fails and we have cached data, return it
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
