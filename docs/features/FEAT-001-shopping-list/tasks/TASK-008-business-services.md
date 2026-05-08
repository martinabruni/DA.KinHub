---
id: TASK-008
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-007
  - TASK-005
  - TASK-009
---

# TASK-008 — Business Services

## Descrizione

Implementare i service nel progetto `Kin.KinHub.Core.Business`, cartella `RecipeFeature/Services/`.

## File da creare

### KinHubShoppingListService.cs

- Implementa `IShoppingListService`
- Dipendenze: `IShoppingListRepository`, `IFamilyMemberService`
- Per ogni operazione: ricava `FamilyId` da `userId` tramite `IFamilyMemberService`
- Verifica ownership: la lista deve appartenere alla famiglia dell'utente
- `GetAll`: ordina per `UpdatedAt DESC` (oppure delega al repository)
- `Delete`: hard delete con cascade (gestito dal DB)
- Mapping da `ShoppingList` → `ShoppingListResponse` tramite Mapster
- `ShoppingListResponse.ItemCount` e `CheckedCount`: calcolati aggregando gli item (o tramite query ottimizzata nel repository)

### KinHubShoppingListItemService.cs

- Implementa `IShoppingListItemService`
- Dipendenze: `IShoppingListItemRepository`, `IShoppingListRepository`, `IFamilyMemberService`
- Verifica che la lista appartenga alla famiglia dell'utente prima di ogni operazione
- `BulkAdd`: chiama `AddBulkAsync` sul repository (dedup gestito lì); restituisce `BulkAddShoppingListItemsResponse { AddedCount = N }`
- `ToggleChecked`: aggiorna `IsChecked` e aggiorna `UpdatedAt` della lista parent
- `DeleteChecked`: bulk delete + aggiorna `UpdatedAt` della lista parent
- **Importante**: dopo ogni modifica agli item, aggiornare `ShoppingList.UpdatedAt` (chiamare `UpdateAsync` sul `IShoppingListRepository`)

## File impattati

- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListService.cs` (nuovo)
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListItemService.cs` (nuovo)
