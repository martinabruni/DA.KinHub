---
id: FEAT-001
type: links
---

# Links — FEAT-001

## File da creare (Backend)

- `scripts/create-postgres-schema.sql` (aggiunta tabelle)

## File generati automaticamente da EF Core Power Tools (TASK-002 — assegnato allo sviluppatore)

- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListEntity.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListItemEntity.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/Common/CoreDbContext.cs` (rigenerato)
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingList.cs`
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingListItem.cs`
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListRepository.cs`
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListItemRepository.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListRequest.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/UpdateShoppingListRequest.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListResponse.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListItemRequest.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsRequest.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsResponse.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListItemResponse.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListItemService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListItemService.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListRepository.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListItemRepository.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListController.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListItemController.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListRequestValidator.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/UpdateShoppingListRequestValidator.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListItemRequestValidator.cs`
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/BulkAddShoppingListItemsRequestValidator.cs`

## File da creare (Frontend)

- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/ShoppingListProvider.tsx`
- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListsPage.tsx`
- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListDetailPage.tsx`

## File da modificare (Backend)

- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/ServiceCollectionExtensions.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ServiceCollectionExtensions.cs`

## File da modificare (Frontend)

- `src/Presentations/Kin.KinHub.Core.React/src/types/index.ts`
- `src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/it.json`
- `src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/en.json`
- `src/Presentations/Kin.KinHub.Core.React/src/components/KinRecipeServiceLayout.tsx`
- `src/Presentations/Kin.KinHub.Core.React/src/router/routes.tsx`
- `src/Presentations/Kin.KinHub.Core.React/src/features/recipes/pages/RecipeDetailPage.tsx`
- `src/Presentations/Kin.KinHub.Core.React/src/features/ai-assistant/pages/AIAssistantPage.tsx`

## Tasks

- [tasks/TASK-001-sql-tables.md](tasks/TASK-001-sql-tables.md)
- [tasks/TASK-002-ef-core-power-tools.md](tasks/TASK-002-ef-core-power-tools.md) ⚠️ assegnato allo sviluppatore
- [tasks/TASK-004-domain-models.md](tasks/TASK-004-domain-models.md)
- [tasks/TASK-005-domain-interfaces.md](tasks/TASK-005-domain-interfaces.md)
- [tasks/TASK-006-business-models.md](tasks/TASK-006-business-models.md)
- [tasks/TASK-007-business-interfaces.md](tasks/TASK-007-business-interfaces.md)
- [tasks/TASK-008-business-services.md](tasks/TASK-008-business-services.md)
- [tasks/TASK-009-infra-repositories.md](tasks/TASK-009-infra-repositories.md)
- [tasks/TASK-010-api-validators.md](tasks/TASK-010-api-validators.md)
- [tasks/TASK-011-api-controllers.md](tasks/TASK-011-api-controllers.md)
- [tasks/TASK-012-di-registration.md](tasks/TASK-012-di-registration.md)
- [tasks/TASK-013-fe-types.md](tasks/TASK-013-fe-types.md)
- [tasks/TASK-014-fe-i18n.md](tasks/TASK-014-fe-i18n.md)
- [tasks/TASK-015-fe-provider.md](tasks/TASK-015-fe-provider.md)
- [tasks/TASK-016-fe-pages.md](tasks/TASK-016-fe-pages.md)
- [tasks/TASK-017-fe-layout.md](tasks/TASK-017-fe-layout.md)
- [tasks/TASK-018-fe-routes.md](tasks/TASK-018-fe-routes.md)
- [tasks/TASK-019-fe-recipe-detail.md](tasks/TASK-019-fe-recipe-detail.md)
- [tasks/TASK-020-fe-ai-suggest.md](tasks/TASK-020-fe-ai-suggest.md)
