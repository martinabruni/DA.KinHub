# Changelog

## 2026-05-08

### FEAT

- Implemented FEAT-001: Lista della Spesa Condivisa (Shared Shopping List)

#### Database

- Added SQL DDL for `ShoppingListEntity` and `ShoppingListItemEntity` tables
  - files:
    - scripts/create-postgres-schema.sql

#### Infrastructure

- Added EF entity models and CoreDbContext configuration
  - files:
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/ShoppingListEntity.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/ShoppingListItemEntity.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListEntity.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListItemEntity.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/CoreDbContext.cs
- Added infrastructure repositories
  - files:
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListRepository.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListItemRepository.cs
    - src/Infrastructures/Kin.KinHub.Core.PostgreSql/ServiceCollectionExtensions.cs

#### Domain

- Added domain models and repository interfaces
  - files:
    - src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingList.cs
    - src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingListItem.cs
    - src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListRepository.cs
    - src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListItemRepository.cs

#### Business

- Added business models, service interfaces, and services
  - files:
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListRequest.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/UpdateShoppingListRequest.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListResponse.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListItemRequest.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsRequest.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsResponse.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListItemResponse.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListService.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListItemService.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListService.cs
    - src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListItemService.cs
    - src/Businesses/Kin.KinHub.Core.Business/ServiceCollectionExtensions.cs

#### Presentation (API)

- Added validators and controllers for shopping list endpoints
  - files:
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListRequestValidator.cs
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/UpdateShoppingListRequestValidator.cs
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListItemRequestValidator.cs
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/BulkAddShoppingListItemsRequestValidator.cs
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListController.cs
    - src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListItemController.cs

#### Frontend (React)

- Added TypeScript types, i18n strings, provider, pages, and navigation
  - files:
    - src/Presentations/Kin.KinHub.Core.React/src/types/index.ts
    - src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/it.json
    - src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/en.json
    - src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/ShoppingListProvider.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListsPage.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListDetailPage.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/components/KinRecipeServiceLayout.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/router/routes.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/features/recipes/pages/RecipeDetailPage.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/features/ai-assistant/pages/AIAssistantPage.tsx
