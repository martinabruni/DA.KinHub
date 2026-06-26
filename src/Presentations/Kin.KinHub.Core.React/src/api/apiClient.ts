import axios, { type AxiosRequestConfig } from "axios";
import { toast } from "sonner";
import i18next from "i18next";
import { redirectToIdentityLogin } from "@/config/appLinks";
import { getStatusAwareErrorMessage } from "@/lib/errors";

const getEnvUrl = (value: unknown, fallback: string) => {
  return typeof value === "string" && value.trim() ? value.trim() : fallback;
};

const BASE_URL = getEnvUrl(
  import.meta.env.VITE_IDENTITY_API_URL,
  "http://localhost:5000",
);

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

const getAccessToken = () => localStorage.getItem("accessToken");
const getRefreshToken = () => localStorage.getItem("refreshToken");
const clearTokens = () => {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  sessionStorage.removeItem("activeMember");
};

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((p) => (token ? p.resolve(token) : p.reject(error)));
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config as AxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || original._retry) {
      const status = error.response?.status;
      if (status !== undefined && status >= 400) {
        toast.error(getStatusAwareErrorMessage(error, status));
      }
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token) => {
            if (original.headers)
              original.headers["Authorization"] = `Bearer ${token}`;
            resolve(apiClient(original));
          },
          reject,
        });
      });
    }

    original._retry = true;
    isRefreshing = true;

    try {
      const refreshToken = getRefreshToken();
      const { data } = await axios.post(`${BASE_URL}/api/auth/refresh`, {
        refreshToken,
      });
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("refreshToken", data.refreshToken);
      apiClient.defaults.headers.common["Authorization"] =
        `Bearer ${data.accessToken}`;
      processQueue(null, data.accessToken);
      return apiClient(original);
    } catch (err) {
      processQueue(err, null);
      clearTokens();
      toast.error(i18next.t("errors.sessionExpired"));
      redirectToIdentityLogin();
      return Promise.reject(err);
    } finally {
      isRefreshing = false;
    }
  },
);
