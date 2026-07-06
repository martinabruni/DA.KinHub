export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}

export interface User {
  id: string;
  email: string;
  displayName?: string | null;
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

export interface KinListSummary {
  id: string
  title: string
  etag: string
  totalItems: number
  completedItems: number
  isCompleted: boolean
  lastModifiedAt: string
}

export interface KinListItem {
  id: string
  text: string
  etag: string
  isCompleted: boolean
  createdAt: string
  updatedAt: string
}

export interface KinListDetail {
  id: string
  title: string
  etag: string
  totalItems: number
  completedItems: number
  isCompleted: boolean
  lastModifiedAt: string
  items: KinListItem[]
}

export interface KinListDraftFromAudioResponse {
  title: string
  items: string[]
  detectedLanguage?: string | null
  promptVersion?: string | null
}

export interface KinListItemDraftProposal {
  text: string
  isSelectedByDefault: boolean
  duplicateOfItemId?: string | null
}

export interface KinListExistingDuplicate {
  itemId: string
  text: string
  isCompleted: boolean
}

export interface KinListItemDraftsFromAudioResponse {
  items: KinListItemDraftProposal[]
  existingDuplicates: KinListExistingDuplicate[]
  detectedLanguage?: string | null
  promptVersion?: string | null
}

export type AudioOperationType = 'NewList' | 'AppendItems'
export type AudioOperationStatus = 'AwaitingUpload' | 'Queued' | 'Processing' | 'Succeeded' | 'Failed' | 'Expired' | 'Cancelled'

export interface CreateAudioOperationResponse {
  id: string
  uploadUrl: string
  uploadExpiresAt: string
  blobName: string
  retryAfterSeconds: number
}

export interface AudioOperationResponse {
  id: string
  type: AudioOperationType
  status: AudioOperationStatus
  listId?: string | null
  title?: string | null
  items: string[]
  itemProposals: KinListItemDraftProposal[]
  existingDuplicates: KinListExistingDuplicate[]
  detectedLanguage?: string | null
  promptVersion?: string | null
  errorCode?: string | null
  errorMessage?: string | null
  retryAfterSeconds: number
  expiresAt: string
}

export interface ProblemDetailsError {
  type?: string
  title?: string
  status?: number
  detail?: string
  code?: string
  correlationId?: string
  errors?: Record<string, string[]>
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

export interface ShoppingList {
  id: string;
  name: string;
  familyId: string;
  itemCount: number;
  checkedCount: number;
}

export interface ShoppingListItem {
  id: string;
  name: string;
  isChecked: boolean;
  createdAt: string;
}

export interface BulkAddShoppingListItemsResponse {
  addedCount: number;
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
