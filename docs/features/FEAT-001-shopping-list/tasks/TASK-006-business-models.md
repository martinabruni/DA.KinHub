---
id: TASK-006
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-007
  - TASK-008
  - TASK-010
---

# TASK-006 — Business Models (Request / Response)

## Descrizione

Creare tutti i modelli request/response nel progetto `Kin.KinHub.Core.Business`, cartella `RecipeFeature/Models/`.

## File da creare

| File | Proprietà |
|---|---|
| `CreateShoppingListRequest.cs` | `Name` (string) |
| `UpdateShoppingListRequest.cs` | `Name` (string) |
| `ShoppingListResponse.cs` | `Id` (Guid), `Name` (string), `FamilyId` (Guid), `ItemCount` (int), `CheckedCount` (int) |
| `CreateShoppingListItemRequest.cs` | `Name` (string), `ShoppingListId` (Guid) |
| `BulkAddShoppingListItemsRequest.cs` | `Names` (IReadOnlyList\<string\>), `ShoppingListId` (Guid) |
| `BulkAddShoppingListItemsResponse.cs` | `AddedCount` (int) |
| `ShoppingListItemResponse.cs` | `Id` (Guid), `Name` (string), `IsChecked` (bool), `CreatedAt` (DateTime) |

## Note

- `ShoppingListResponse` ha sia `ItemCount` che `CheckedCount` per visualizzare "X/Y" nelle card.
- `BulkAddShoppingListItemsResponse.AddedCount` conta solo gli item effettivamente inseriti (non i duplicati scartati).
- Seguire il pattern `FridgeResponse` / `FridgeIngredientResponse` come riferimento.

## File impattati

- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListRequest.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/UpdateShoppingListRequest.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListResponse.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/CreateShoppingListItemRequest.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsRequest.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/BulkAddShoppingListItemsResponse.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Models/ShoppingListItemResponse.cs` (nuovo)
