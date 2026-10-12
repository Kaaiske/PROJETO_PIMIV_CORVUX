import axios from 'axios';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

// Use EXPO_PUBLIC_API_URL or production domain https://corvuxiam.tech
const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL ||
  (process.env.NODE_ENV === 'production'
    ? 'https://corvuxiam.tech'
    : (Platform.OS === 'android' ? 'http://10.0.2.2:5052' : 'http://localhost:5052'));

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
});

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
    if (error.response?.status === 401) {
      // Handle unauthorized (e.g., redirect to login)
      await SecureStore.deleteItemAsync('userToken');
      // A full implementation would trigger a global state update to reset navigation
    }
    return Promise.reject(error);
  }
);

export default api;

