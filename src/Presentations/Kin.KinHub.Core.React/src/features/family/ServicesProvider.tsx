import type { ReactNode } from "react";
import { createContext, useCallback, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { kinHubApiClient } from "@/api/apiClient";
import { isServiceToggleable, normalizeServiceState } from "@/config/serviceConfig";
import { useAuth } from "@/features/auth/AuthProvider";
import type { Family, Service } from "@/types";

interface ServicesContextValue {
  services: Service[];
  isLoading: boolean;
  toggleService: (serviceId: number, enabled: boolean) => Promise<void>;
}

const ServicesContext = createContext<ServicesContextValue | null>(null);

export function ServicesProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAuth();
  const queryClient = useQueryClient();

  const { data: family, isLoading: isLoadingFamily } = useQuery({
    queryKey: ["family"],
    queryFn: async () => {
      const { data } = await kinHubApiClient.get<Family>("/api/families");
      return data;
    },
    enabled: isAuthenticated,
    retry: false,
  });

  const qKey = ["services", "family", family?.id];

  const { data: services = [], isLoading: isLoadingServices } = useQuery({
    queryKey: qKey,
    queryFn: async () => {
      const { data } = await kinHubApiClient.get<Service[]>(
        `/api/services/family/${family!.id}`,
      );
      return data.map(normalizeServiceState);
    },
    enabled: !!family?.id,
  });

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: qKey });
  }, [queryClient, qKey]);

  const toggleMutation = useMutation({
    mutationFn: async ({
      serviceId,
      enabled,
    }: {
      serviceId: number;
      enabled: boolean;
    }) => {
      await kinHubApiClient.post(`/api/services/family/${family!.id}/toggle`, {
        serviceId,
        isActive: enabled,
      });
    },
    onMutate: async ({ serviceId, enabled }) => {
      await queryClient.cancelQueries({ queryKey: qKey });
      const previous = queryClient.getQueryData<Service[]>(qKey);
      queryClient.setQueryData<Service[]>(qKey, (old = []) =>
        old.map((s) => (s.id === serviceId ? { ...s, isEnabled: enabled } : s)),
      );
      return { previous };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) queryClient.setQueryData(qKey, ctx.previous);
      toast.error(t("services.toggleError"));
    },
    onSuccess: () => {
      toast.success(t("services.updated"));
      invalidate();
    },
  });

  return (
    <ServicesContext.Provider
      value={{
        services,
        isLoading: isLoadingFamily || isLoadingServices,
        toggleService: (serviceId, enabled) => {
          const service = services.find((item) => item.id === serviceId);

          if (service && !isServiceToggleable(service.name)) {
            toast.error(t("services.fixedServiceError"));
            return Promise.resolve();
          }

          return toggleMutation.mutateAsync({ serviceId, enabled });
        },
      }}
    >
      {children}
    </ServicesContext.Provider>
  );
}

export function useServices() {
  const ctx = useContext(ServicesContext);
  if (!ctx) throw new Error("useServices must be used within ServicesProvider");
  return ctx;
}
