import type { ReactNode } from "react";
import { createContext, useCallback, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { apiClient } from "@/api/apiClient";
import type { Ingredient, Recipe, Step } from "@/types";

interface RecipeContextValue {
  recipes: Recipe[];
  isLoading: boolean;
  getRecipe: (recipeId: string) => Recipe | undefined;
  createRecipe: (bookId: string, data: Partial<Recipe>) => Promise<Recipe>;
  updateRecipe: (
    bookId: string,
    recipeId: string,
    data: Partial<Recipe>,
  ) => Promise<void>;
  deleteRecipe: (bookId: string, recipeId: string) => Promise<void>;
  addIngredient: (
    bookId: string,
    recipeId: string,
    ingredient: Omit<Ingredient, "id">,
  ) => Promise<void>;
  deleteIngredient: (
    bookId: string,
    recipeId: string,
    ingredientId: string,
  ) => Promise<void>;
  addStep: (
    bookId: string,
    recipeId: string,
    step: Omit<Step, "id">,
  ) => Promise<void>;
  deleteStep: (
    bookId: string,
    recipeId: string,
    stepId: string,
  ) => Promise<void>;
}

const RecipeContext = createContext<RecipeContextValue | null>(null);

interface RecipeProviderProps {
  children: ReactNode;
  bookId: string;
}

export function RecipeProvider({ children, bookId }: RecipeProviderProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const qKey = ["recipes", bookId];

  const { data: recipes = [], isLoading } = useQuery({
    queryKey: qKey,
    queryFn: async () => {
      const { data } = await apiClient.get<Recipe[]>(
        `/api/recipe-books/${bookId}/recipes`,
      );
      return data;
    },
    enabled: !!bookId,
  });

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: qKey });
  }, [queryClient, qKey]);

  const createMutation = useMutation({
    mutationFn: async ({ bId, d }: { bId: string; d: Partial<Recipe> }) => {
      const minutes = d.prepTimeMinutes ?? 0;
      const hh = String(Math.floor(minutes / 60)).padStart(2, "0");
      const mm = String(minutes % 60).padStart(2, "0");
      const { data } = await apiClient.post<Recipe>(
        `/api/recipe-books/${bId}/recipes`,
        {
          name: d.name,
          backstory: d.description,
          finalTime: `${hh}:${mm}:00`,
          portions: d.servingSize,
          recipeBookId: d.recipeBookId ?? bId,
          ingredients: d.ingredients?.map((i) => ({
            name: i.name,
            quantity: i.quantity,
            measureUnit: i.unit,
          })),
          steps: d.steps?.map((s) => ({
            order: s.order,
            description: s.description,
          })),
        },
      );
      return data;
    },
    onSuccess: () => {
      toast.success(t("recipes.created"));
      invalidate();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      bId,
      rId,
      d,
    }: {
      bId: string;
      rId: string;
      d: Partial<Recipe>;
    }) => {
      const minutes = d.prepTimeMinutes ?? 0;
      const hh = String(Math.floor(minutes / 60)).padStart(2, "0");
      const mm = String(minutes % 60).padStart(2, "0");
      return apiClient.put(`/api/recipe-books/${bId}/recipes/${rId}`, {
        name: d.name,
        backstory: d.description,
        finalTime: `${hh}:${mm}:00`,
        portions: d.servingSize,
        ingredients: d.ingredients?.map((i) => ({
          name: i.name,
          quantity: i.quantity,
          measureUnit: i.unit,
        })),
        steps: d.steps?.map((s) => ({
          order: s.order,
          description: s.description,
        })),
      });
    },
    onSuccess: () => {
      toast.success(t("recipes.updated"));
      invalidate();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: ({ bId, rId }: { bId: string; rId: string }) =>
      apiClient.delete(`/api/recipe-books/${bId}/recipes/${rId}`),
    onSuccess: () => {
      toast.success(t("recipes.deleted"));
      invalidate();
    },
  });

  const addIngredientMutation = useMutation({
    mutationFn: ({
      bId,
      rId,
      ingredient,
    }: {
      bId: string;
      rId: string;
      ingredient: Omit<Ingredient, "id">;
    }) =>
      apiClient.post(`/api/recipe-books/${bId}/recipes/${rId}/ingredients`, {
        name: ingredient.name,
        quantity: ingredient.quantity,
        measureUnit: ingredient.unit,
        recipeId: rId,
      }),
    onSuccess: () => {
      toast.success(t("recipes.ingredientAdded"));
      invalidate();
    },
  });

  const deleteIngredientMutation = useMutation({
    mutationFn: ({
      bId,
      rId,
      iId,
    }: {
      bId: string;
      rId: string;
      iId: string;
    }) =>
      apiClient.delete(
        `/api/recipe-books/${bId}/recipes/${rId}/ingredients/${iId}`,
      ),
    onSuccess: () => {
      toast.success(t("recipes.ingredientDeleted"));
      invalidate();
    },
  });

  const addStepMutation = useMutation({
    mutationFn: ({
      bId,
      rId,
      step,
    }: {
      bId: string;
      rId: string;
      step: Omit<Step, "id">;
    }) =>
      apiClient.post(`/api/recipe-books/${bId}/recipes/${rId}/steps`, {
        order: step.order,
        description: step.description,
        recipeId: rId,
      }),
    onSuccess: () => {
      toast.success(t("recipes.stepAdded"));
      invalidate();
    },
  });

  const deleteStepMutation = useMutation({
    mutationFn: ({
      bId,
      rId,
      sId,
    }: {
      bId: string;
      rId: string;
      sId: string;
    }) =>
      apiClient.delete(`/api/recipe-books/${bId}/recipes/${rId}/steps/${sId}`),
    onSuccess: () => {
      toast.success(t("recipes.stepDeleted"));
      invalidate();
    },
  });

  return (
    <RecipeContext.Provider
      value={{
        recipes,
        isLoading,
        getRecipe: (recipeId) => recipes.find((r) => r.id === recipeId),
        createRecipe: (bId, d) => createMutation.mutateAsync({ bId, d }),
        updateRecipe: async (bId, rId, d) => {
          await updateMutation.mutateAsync({ bId, rId, d });
        },
        deleteRecipe: async (bId, rId) => {
          await deleteMutation.mutateAsync({ bId, rId });
        },
        addIngredient: async (bId, rId, ingredient) => {
          await addIngredientMutation.mutateAsync({ bId, rId, ingredient });
        },
        deleteIngredient: async (bId, rId, iId) => {
          await deleteIngredientMutation.mutateAsync({ bId, rId, iId });
        },
        addStep: async (bId, rId, step) => {
          await addStepMutation.mutateAsync({ bId, rId, step });
        },
        deleteStep: async (bId, rId, sId) => {
          await deleteStepMutation.mutateAsync({ bId, rId, sId });
        },
      }}
    >
      {children}
    </RecipeContext.Provider>
  );
}

export function useRecipes() {
  const ctx = useContext(RecipeContext);
  if (!ctx) throw new Error("useRecipes must be used within RecipeProvider");
  return ctx;
}
