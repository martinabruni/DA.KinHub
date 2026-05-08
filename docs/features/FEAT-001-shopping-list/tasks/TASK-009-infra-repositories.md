---
id: TASK-009
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-002
  - TASK-005
---

# TASK-009 — Infrastructure Repositories

## Descrizione

Implementare i repository nel progetto `Kin.KinHub.Core.PostgreSql`, cartella `RecipeFeature/Repositories/`.

## File da creare

### ShoppingListRepository.cs

- Estende `PostgreSqlRepository<ShoppingListEntity, ShoppingList, Guid>`
- Implementa `IShoppingListRepository`
- `GetAllByFamilyIdAsync`: filtra per `FamilyId`, ordina per `UpdatedAt DESC`
- `GetByIdAsync`: include proiezione per calcolare `ItemCount` e `CheckedCount` (o separare in query distinta)
- Mapping entity ↔ domain via Mapster

### ShoppingListItemRepository.cs

- Estende `PostgreSqlRepository<ShoppingListItemEntity, ShoppingListItem, Guid>`
- Implementa `IShoppingListItemRepository`
- `AddBulkAsync`: 
  - Carica i nomi esistenti per la lista (case-insensitive)
  - Filtra i nuovi item non duplicati
  - Inserisce in bulk
  - Restituisce il count degli item effettivamente inseriti
- `ToggleCheckedAsync`: aggiorna `IsChecked = !IsChecked` e `UpdatedAt = now()`
- `DeleteCheckedByListIdAsync`: `DELETE WHERE ShoppingListId = ? AND IsChecked = true`
- `ExistsByNameAsync`: `SELECT EXISTS WHERE ShoppingListId = ? AND lower(Name) = lower(?)`
- `GetAllByListIdAsync`: ordina unchecked (CreatedAt ASC) poi checked (CreatedAt ASC)

## Note

- Seguire il pattern `FridgeRepository` / `FridgeIngredientRepository` come riferimento.
- Il sorting degli item (unchecked prima, checked dopo) può essere fatto lato repository con `ORDER BY "IsChecked" ASC, "CreatedAt" ASC` o lato service — preferire repository per efficienza.

## File impattati

- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListRepository.cs` (nuovo)
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Repositories/ShoppingListItemRepository.cs` (nuovo)
