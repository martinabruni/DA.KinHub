import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from "axios";
import { toast } from "sonner";
import i18next from "i18next";
import { redirectToIdentityLogin } from "@/config/appLinks";
import { getStatusAwareErrorMessage } from "@/lib/errors";

const getEnvUrl = (value: unknown, fallback: string) => {
  return typeof value === "string" && value.trim() ? value.trim() : fallback;
};

const KINRECIPE_API_URL = getEnvUrl(
  import.meta.env.VITE_KINRECIPE_API_URL,
  "http://localhost:5000",
);
const IDENTITY_API_URL = getEnvUrl(
  import.meta.env.VITE_IDENTITY_API_URL,
  KINRECIPE_API_URL,
);

export const apiClient = axios.create({
  baseURL: KINRECIPE_API_URL,
  headers: { "Content-Type": "application/json" },
});

export const identityApiClient = axios.create({
  baseURL: IDENTITY_API_URL,
  headers: { "Content-Type": "application/json" },
});

const getAccessToken = () => localStorage.getItem("accessToken");
const getRefreshToken = () => localStorage.getItem("refreshToken");
const clearTokens = () => {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  sessionStorage.removeItem("activeMember");
};

const attachAccessToken = (config: InternalAxiosRequestConfig) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
};

apiClient.interceptors.request.use(attachAccessToken);
identityApiClient.interceptors.request.use(attachAccessToken);

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((p) => (token ? p.resolve(token) : p.reject(error)));
  failedQueue = [];
};

const isRefreshableRequest = (url?: string) => {
  if (!url) {
    return false;
  }

  return ![
    "/api/auth/login",
    "/api/auth/logout",
    "/api/auth/refresh",
    "/api/auth/register",
  ].some((authPath) => url.endsWith(authPath));
};

const attachRefreshInterceptor = (client: AxiosInstance) => {
  client.interceptors.response.use(
    (res) => res,
    async (error) => {
      const original = error.config as AxiosRequestConfig & { _retry?: boolean };

      if (
        error.response?.status !== 401 ||
        original._retry ||
        !isRefreshableRequest(original.url)
      ) {
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
              if (original.headers) {
                original.headers["Authorization"] = `Bearer ${token}`;
              }
              resolve(client(original));
            },
            reject,
          });
        });
      }

      original._retry = true;
      isRefreshing = true;

      try {
        const refreshToken = getRefreshToken();
        const { data } = await axios.post(
          `${IDENTITY_API_URL}/api/auth/refresh`,
          {
            refreshToken,
          },
        );
        localStorage.setItem("accessToken", data.accessToken);
        localStorage.setItem("refreshToken", data.refreshToken);
        apiClient.defaults.headers.common["Authorization"] =
          `Bearer ${data.accessToken}`;
        identityApiClient.defaults.headers.common["Authorization"] =
          `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);
        return client(original);
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
};

attachRefreshInterceptor(apiClient);
attachRefreshInterceptor(identityApiClient);
