import type { ReactNode } from "react";
import { createContext, useCallback, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/api/apiClient";
import type { Fridge, FridgeIngredient } from "@/types";

interface FridgeContextValue {
  fridges: Fridge[];
  isLoading: boolean;
  createFridge: (name: string) => Promise<void>;
  updateFridge: (id: string, name: string) => Promise<void>;
  deleteFridge: (id: string) => Promise<void>;
  getFridgeIngredients: (id: string) => FridgeIngredient[];
  isFridgeLoading: (id: string) => boolean;
  addIngredient: (
    fridgeId: string,
    ingredient: Omit<FridgeIngredient, "id">,
  ) => Promise<void>;
  updateIngredient: (
    fridgeId: string,
    ingredientId: string,
    ingredient: Omit<FridgeIngredient, "id">,
  ) => Promise<void>;
  deleteIngredient: (fridgeId: string, ingredientId: string) => Promise<void>;
}

const FridgeContext = createContext<FridgeContextValue | null>(null);

export function FridgeProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const qKey = ["fridges"];

  const { data: fridges = [], isLoading } = useQuery({
    queryKey: qKey,
    queryFn: async () => {
      const { data } = await apiClient.get<Fridge[]>("/api/fridges");
      return data;
    },
  });

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: qKey });
  }, [queryClient]);

  const createMutation = useMutation({
    mutationFn: (name: string) => apiClient.post("/api/fridges", { name }),
    onSuccess: () => {
      toast.success(t("fridges.created"));
      invalidate();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) =>
      apiClient.put(`/api/fridges/${id}`, { name }),
    onSuccess: () => {
      toast.success(t("fridges.updated"));
      invalidate();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/fridges/${id}`),
    onSuccess: () => {
      toast.success(t("fridges.deleted"));
      invalidate();
    },
  });

  const addIngredientMutation = useMutation({
    mutationFn: ({
      fridgeId,
      ingredient,
    }: {
      fridgeId: string;
      ingredient: Omit<FridgeIngredient, "id">;
    }) =>
      apiClient.post(`/api/fridges/${fridgeId}/ingredients`, {
        name: ingredient.name,
        quantity: ingredient.quantity,
        measureUnit: ingredient.unit,
        fridgeId,
      }),
    onSuccess: (_, { fridgeId }) => {
      toast.success(t("fridges.ingredientAdded"));
      queryClient.invalidateQueries({
        queryKey: ["fridge-ingredients", fridgeId],
      });
    },
  });

  const updateIngredientMutation = useMutation({
    mutationFn: ({
      fridgeId,
      ingredientId,
      ingredient,
    }: {
      fridgeId: string;
      ingredientId: string;
      ingredient: Omit<FridgeIngredient, "id">;
    }) =>
      apiClient.put(`/api/fridges/${fridgeId}/ingredients/${ingredientId}`, {
        name: ingredient.name,
        quantity: ingredient.quantity,
        measureUnit: ingredient.unit,
      }),
    onSuccess: (_, { fridgeId }) => {
      toast.success(t("fridges.ingredientUpdated"));
      queryClient.invalidateQueries({
        queryKey: ["fridge-ingredients", fridgeId],
      });
    },
  });

  const deleteIngredientMutation = useMutation({
    mutationFn: ({
      fridgeId,
      ingredientId,
    }: {
      fridgeId: string;
      ingredientId: string;
    }) =>
      apiClient.delete(`/api/fridges/${fridgeId}/ingredients/${ingredientId}`),
    onSuccess: (_, { fridgeId }) => {
      toast.success(t("fridges.ingredientDeleted"));
      queryClient.invalidateQueries({
        queryKey: ["fridge-ingredients", fridgeId],
      });
    },
  });

  return (
    <FridgeContext.Provider
      value={{
        fridges,
        isLoading,
        createFridge: async (name) => {
          await createMutation.mutateAsync(name);
        },
        updateFridge: async (id, name) => {
          await updateMutation.mutateAsync({ id, name });
        },
        deleteFridge: async (id) => {
          await deleteMutation.mutateAsync(id);
        },
        getFridgeIngredients: (id) => {
          const data = queryClient.getQueryData<FridgeIngredient[]>([
            "fridge-ingredients",
            id,
          ]);
          return data ?? [];
        },
        isFridgeLoading: (id) => {
          return (
            queryClient.isFetching({ queryKey: ["fridge-ingredients", id] }) > 0
          );
        },
        addIngredient: async (fridgeId, ingredient) => {
          await addIngredientMutation.mutateAsync({ fridgeId, ingredient });
        },
        updateIngredient: async (fridgeId, ingredientId, ingredient) => {
          await updateIngredientMutation.mutateAsync({
            fridgeId,
            ingredientId,
            ingredient,
          });
        },
        deleteIngredient: async (fridgeId, ingredientId) => {
          await deleteIngredientMutation.mutateAsync({
            fridgeId,
            ingredientId,
          });
        },
      }}
    >
      {children}
    </FridgeContext.Provider>
  );
}

export function useFridges() {
  const ctx = useContext(FridgeContext);
  if (!ctx) throw new Error("useFridges must be used within FridgeProvider");
  return ctx;
}
