import axios from 'axios';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import { DeviceEventEmitter } from 'react-native';

// Use EXPO_PUBLIC_API_URL or production domain https://corvuxiam.tech
const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL ||
  (process.env.NODE_ENV === 'production'
    ? 'https://corvuxiam.tech'
    : (Platform.OS === 'android' ? 'http://10.0.2.2:5052' : 'http://localhost:5052'));

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
});

let refreshPromise: Promise<string | null> | null = null;

const refreshSession = async (): Promise<string | null> => {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const refreshToken = await SecureStore.getItemAsync('refreshToken');
    const email = await SecureStore.getItemAsync('userEmail');
    if (!refreshToken || !email) return null;

    try {
      const refreshed = await axios.post(`${API_BASE_URL}/api/Auth/refresh-token`, { email, refreshToken }, { timeout: 10000 });
      const nextToken = refreshed.data?.token;
      const nextRefreshToken = refreshed.data?.refreshToken;
      if (!nextToken || !nextRefreshToken) return null;

      await SecureStore.setItemAsync('userToken', nextToken);
      await SecureStore.setItemAsync('refreshToken', nextRefreshToken);
      return nextToken;
    } catch {
      return null;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
};

api.interceptors.request.use(async (config) => {
  const token = await SecureStore.getItemAsync('userToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as (typeof error.config & { _retry?: boolean }) | undefined;
    const isAuthRequest = originalRequest?.url?.includes('/api/Auth/login') ||
      originalRequest?.url?.includes('/api/Auth/mfa-verify') ||
      originalRequest?.url?.includes('/api/Auth/refresh-token');

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthRequest) {
      originalRequest._retry = true;
      const nextToken = await refreshSession();
      if (nextToken) {
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${nextToken}`;
        return api(originalRequest);
      }

      await SecureStore.deleteItemAsync('userToken');
      await SecureStore.deleteItemAsync('refreshToken');
      await SecureStore.deleteItemAsync('userEmail');
      DeviceEventEmitter.emit('corvusiam:logout');
    }
    return Promise.reject(error);
  }
);

export default api;

