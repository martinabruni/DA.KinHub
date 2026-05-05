import type { ReactNode } from "react";
import { createContext, useCallback, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/api/apiClient";
import { useAuthContext } from "@/store/authContext";
import { getApiErrorMessage } from "@/lib/errors";
import type { Family } from "@/types";

interface FamilyContextValue {
  family: Family | undefined;
  isLoading: boolean;
  updateName: (name: string) => Promise<void>;
  addMember: (name: string) => Promise<void>;
  updateMember: (memberId: string, name: string) => Promise<void>;
  removeMember: (memberId: string) => Promise<void>;
  createFamily: (payload: {
    familyName: string;
    ownerProfileName: string;
  }) => Promise<void>;
  leaveFamily: () => Promise<void>;
  deleteFamily: () => Promise<void>;
}

const FamilyContext = createContext<FamilyContextValue | null>(null);

export function FamilyProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { activeMember, isAuthenticated } = useAuthContext();
  const queryClient = useQueryClient();

  const { data: family, isLoading } = useQuery({
    queryKey: ["family"],
    queryFn: async () => {
      const { data } = await apiClient.get<Family>("/api/families");
      return data;
    },
    enabled: isAuthenticated,
    retry: false,
  });

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["family"] });
  }, [queryClient]);

  const updateNameMutation = useMutation({
    mutationFn: (name: string) => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      return apiClient.patch(`/api/families/${currentFamily.id}`, { name });
    },
    onSuccess: () => {
      toast.success(t("family.updated"));
      invalidate();
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const addMemberMutation = useMutation({
    mutationFn: (name: string) => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      return apiClient.post(`/api/families/${currentFamily.id}/members`, { name });
    },
    onSuccess: () => invalidate(),
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const updateMemberMutation = useMutation({
    mutationFn: ({ memberId, name }: { memberId: string; name: string }) => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      return apiClient.put(`/api/families/${currentFamily.id}/members/${memberId}`, {
        name,
      });
    },
    onSuccess: () => invalidate(),
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const removeMemberMutation = useMutation({
    mutationFn: (memberId: string) => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      return apiClient.delete(`/api/families/${currentFamily.id}/members/${memberId}`);
    },
    onSuccess: () => {
      toast.success(t("family.memberRemoved"));
      invalidate();
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const createFamilyMutation = useMutation({
    mutationFn: (payload: {
      familyName: string;
      ownerProfileName: string;
    }) => apiClient.post("/api/families", payload),
    onSuccess: () => {
      invalidate();
      queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const leaveFamilyMutation = useMutation({
    mutationFn: () => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      if (!activeMember?.id) throw new Error("Active member not set");
      return apiClient.delete(`/api/families/${currentFamily.id}/members/${activeMember.id}`);
    },
    onSuccess: () => {
      toast.success(t("family.left"));
      invalidate();
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  const deleteFamilyMutation = useMutation({
    mutationFn: () => {
      const currentFamily = queryClient.getQueryData<Family>(["family"]);
      if (!currentFamily?.id) throw new Error("Family not loaded");
      return apiClient.delete(`/api/families/${currentFamily.id}`);
    },
    onSuccess: () => {
      toast.success(t("family.deleted"));
      invalidate();
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err));
    },
  });

  return (
    <FamilyContext.Provider
      value={{
        family,
        isLoading,
        updateName: async (name) => {
          await updateNameMutation.mutateAsync(name);
        },
        addMember: async (name) => {
          await addMemberMutation.mutateAsync(name);
        },
        updateMember: async (memberId, name) => {
          await updateMemberMutation.mutateAsync({ memberId, name });
        },
        removeMember: async (memberId) => {
          await removeMemberMutation.mutateAsync(memberId);
        },
        createFamily: async (payload) => {
          await createFamilyMutation.mutateAsync(payload);
        },
        leaveFamily: async () => {
          await leaveFamilyMutation.mutateAsync();
        },
        deleteFamily: async () => {
          await deleteFamilyMutation.mutateAsync();
        },
      }}
    >
      {children}
    </FamilyContext.Provider>
  );
}

export function useFamily() {
  const ctx = useContext(FamilyContext);
  if (!ctx) throw new Error("useFamily must be used within FamilyProvider");
  return ctx;
}
