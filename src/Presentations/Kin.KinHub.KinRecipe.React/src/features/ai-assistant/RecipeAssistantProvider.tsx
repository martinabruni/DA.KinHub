import type { ReactNode } from 'react'
import { createContext, useContext } from 'react'
import { useMutation } from '@tanstack/react-query'
import { apiClient } from '@/api/apiClient'
import type { AIAdaptedRecipe, AIParsedRecipe, SuggestRecipesResult } from '@/types'

interface RecipeAssistantContextValue {
  suggestRecipes: (fridgeId: string) => Promise<SuggestRecipesResult>
  parseRecipe: (rawText: string) => Promise<AIParsedRecipe>
  adaptRecipe: (recipeId: string, constraints: string[]) => Promise<AIAdaptedRecipe>
  isSuggesting: boolean
  isParsing: boolean
  isAdapting: boolean
}

const RecipeAssistantContext = createContext<RecipeAssistantContextValue | null>(null)

export function RecipeAssistantProvider({ children }: { children: ReactNode }) {
  const suggestMutation = useMutation({
    mutationFn: async (fridgeId: string) => {
      const { data } = await apiClient.post<SuggestRecipesResult>('/api/recipe-assistant/suggest', { fridgeId })
      return data
    },
  })

  const parseMutation = useMutation({
    mutationFn: async (rawText: string) => {
      const { data } = await apiClient.post<AIParsedRecipe>('/api/recipe-assistant/parse', { rawText })
      return data
    },
  })

  const adaptMutation = useMutation({
    mutationFn: async ({ recipeId, constraints }: { recipeId: string; constraints: string[] }) => {
      const { data } = await apiClient.post<AIAdaptedRecipe>('/api/recipe-assistant/adapt', {
        recipeId,
        constraints,
      })
      return data
    },
  })

  return (
    <RecipeAssistantContext.Provider
      value={{
        suggestRecipes: (fridgeId) => suggestMutation.mutateAsync(fridgeId),
        parseRecipe: (rawText) => parseMutation.mutateAsync(rawText),
        adaptRecipe: (recipeId, constraints) => adaptMutation.mutateAsync({ recipeId, constraints }),
        isSuggesting: suggestMutation.isPending,
        isParsing: parseMutation.isPending,
        isAdapting: adaptMutation.isPending,
      }}
    >
      {children}
    </RecipeAssistantContext.Provider>
  )
}

export function useRecipeAssistant() {
  const ctx = useContext(RecipeAssistantContext)
  if (!ctx) throw new Error('useRecipeAssistant must be used within RecipeAssistantProvider')
  return ctx
}
