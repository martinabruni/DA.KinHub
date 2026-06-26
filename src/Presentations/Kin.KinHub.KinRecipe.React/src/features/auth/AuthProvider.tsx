import type { ReactNode } from "react";
import { createContext, useCallback, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/api/apiClient";
import { useAuthContext } from "@/store/authContext";
import type { AuthTokens, LoginRequest, RegisterRequest, User } from "@/types";

interface AuthProviderValue {
  user: User | null;
  isAuthenticated: boolean;
  isLoadingUser: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthProviderContext = createContext<AuthProviderValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { setUser, setTokens, clearAuth, isAuthenticated, user } = useAuthContext();

  const { isLoading: isLoadingUser } = useQuery({
    queryKey: ["auth", "me"],
    queryFn: async () => {
      const { data } = await apiClient.get<User>("/api/auth/me");
      setUser(data);
      return data;
    },
    enabled: isAuthenticated,
    retry: false,
  });

  const loginMutation = useMutation({
    mutationFn: async (payload: LoginRequest) => {
      const { data } = await apiClient.post<AuthTokens>(
        "/api/auth/login",
        payload,
      );
      return data;
    },
    onSuccess: async (tokens) => {
      setTokens(tokens.accessToken, tokens.refreshToken);
      const { data } = await apiClient.get<User>("/api/auth/me");
      setUser(data);
      await queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
  });

  const registerMutation = useMutation({
    mutationFn: async (payload: RegisterRequest) => {
      await apiClient.post("/api/auth/register", payload);
    },
    onSuccess: () => {
      toast.success(t("auth.accountCreated"));
    },
  });

  const logoutMutation = useMutation({
    mutationFn: async () => {
      const refreshToken = localStorage.getItem("refreshToken") ?? "";
      await apiClient.post("/api/auth/logout", { refreshToken });
    },
    onSettled: () => {
      clearAuth();
      queryClient.clear();
      toast.success(t("auth.loggedOut"));
    },
  });

  const login = useCallback(
    async (data: LoginRequest) => {
      await loginMutation.mutateAsync(data);
    },
    [loginMutation],
  );

  const register = useCallback(
    async (data: RegisterRequest) => {
      await registerMutation.mutateAsync(data);
    },
    [registerMutation],
  );

  const logout = useCallback(async () => {
    await logoutMutation.mutateAsync();
  }, [logoutMutation]);

  return (
    <AuthProviderContext.Provider
      value={{ user, isAuthenticated, isLoadingUser, login, register, logout }}
    >
      {children}
    </AuthProviderContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthProviderContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
