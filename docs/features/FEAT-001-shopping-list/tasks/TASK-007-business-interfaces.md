---
id: TASK-007
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-006
  - TASK-008
  - TASK-011
---

# TASK-007 — Business Interfaces (Service)

## Descrizione

Creare i contratti dei service nel progetto `Kin.KinHub.Core.Business`, cartella `RecipeFeature/Interfaces/`.

## File da creare

### IShoppingListService.cs

Metodi:
- `GetAllAsync(string userId, CancellationToken ct)` → `IReadOnlyList<ShoppingListResponse>`
- `GetByIdAsync(string userId, Guid id, CancellationToken ct)` → `ShoppingListResponse`
- `CreateAsync(string userId, CreateShoppingListRequest request, CancellationToken ct)` → `ShoppingListResponse`
- `UpdateAsync(string userId, Guid id, UpdateShoppingListRequest request, CancellationToken ct)` → `ShoppingListResponse`
- `DeleteAsync(string userId, Guid id, CancellationToken ct)` → `void`

### IShoppingListItemService.cs

Metodi:
- `GetAllByListIdAsync(string userId, Guid listId, CancellationToken ct)` → `IReadOnlyList<ShoppingListItemResponse>`
- `AddAsync(string userId, Guid listId, CreateShoppingListItemRequest request, CancellationToken ct)` → `ShoppingListItemResponse`
- `BulkAddAsync(string userId, Guid listId, BulkAddShoppingListItemsRequest request, CancellationToken ct)` → `BulkAddShoppingListItemsResponse`
- `ToggleCheckedAsync(string userId, Guid listId, Guid itemId, CancellationToken ct)` → `ShoppingListItemResponse`
- `DeleteAsync(string userId, Guid listId, Guid itemId, CancellationToken ct)` → `void`
- `DeleteCheckedAsync(string userId, Guid listId, CancellationToken ct)` → `void`

## Note

- Tutte le interfacce devono avere `/// <summary>` per ogni metodo.
- `userId` viene passato dal controller per risolvere il `FamilyId` tramite `IFamilyMemberService`.

## File impattati

- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListService.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Interfaces/IShoppingListItemService.cs` (nuovo)
