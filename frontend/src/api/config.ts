import { client } from './gen/configService/client.gen';

// Configure API client with base URL and credentials
// Credentials: 'include' ensures httpOnly cookies are sent with every request
client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5016',
  throwOnError: true,
  credentials: 'include', // Send cookies with every request
});

export { client };
