export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}

export interface User {
  id: string;
  email: string;
  familyId: string | null;
}

export interface Family {
  id: string;
  name: string;
  members: FamilyMember[];
}

export interface FamilyMember {
  id: string;
  name: string;
}

export interface Service {
  id: number;
  name: string;
  description?: string;
  icon?: string;
  isEnabled: boolean;
}

export interface RecipeBook {
  id: string;
  name: string;
  recipeCount: number;
  updatedAt: string;
}

export interface Recipe {
  id: string;
  recipeBookId: string;
  name: string;
  description?: string;
  servingSize: number;
  prepTimeMinutes: number;
  ingredients: Ingredient[];
  steps: Step[];
}

export interface Ingredient {
  id: string;
  name: string;
  quantity: number;
  unit: string;
}

export interface Step {
  id: string;
  order: number;
  description: string;
}

export interface Fridge {
  id: string;
  name: string;
  ingredientCount: number;
}

export interface FridgeIngredient {
  id: string;
  name: string;
  quantity: number;
  unit: string;
}

export interface AiIngredient {
  id?: string;
  name: string;
  quantity: number;
  measureUnit: string;
}

export interface AiStep {
  order: number;
  description: string;
}

export interface AIParsedRecipe {
  name: string;
  backstory?: string;
  finalTime: string;
  portions: number;
  ingredients: AiIngredient[];
  steps: AiStep[];
}

export interface AISuggestedRecipe {
  recipe: AIParsedRecipe;
  matchPercentage: number;
  missingIngredients: AiIngredient[];
}

export interface ExistingRecipeSuggestion {
  recipeId: string;
  name: string;
  matchPercentage: number;
  missingIngredients: AiIngredient[];
}

export interface SuggestRecipesResult {
  existingRecipes: ExistingRecipeSuggestion[];
  newRecipes: AISuggestedRecipe[];
}

export interface RecipeChange {
  type: string;
  description: string;
}

export interface AIAdaptedRecipe {
  originalRecipe: AIParsedRecipe;
  adaptedRecipe: AIParsedRecipe;
  changes: RecipeChange[];
  changedOriginalIngredientIds: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}
