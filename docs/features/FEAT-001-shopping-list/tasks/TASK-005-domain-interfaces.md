---
id: TASK-005
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-004
  - TASK-009
---

# TASK-005 — Domain Interfaces (Repository)

## Descrizione

Creare i contratti repository nel progetto `Kin.KinHub.Core.Domain`, cartella `RecipeFeature/Interfaces/`.

## File da creare

### IShoppingListRepository.cs

```
src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListRepository.cs
```

Metodi:
- `GetAllByFamilyIdAsync(Guid familyId, CancellationToken ct)` → `IReadOnlyList<ShoppingList>`
- `GetByIdAsync(Guid id, CancellationToken ct)` → `ShoppingList?`
- `AddAsync(ShoppingList list, CancellationToken ct)` → `ShoppingList`
- `UpdateAsync(ShoppingList list, CancellationToken ct)` → `ShoppingList`
- `DeleteAsync(Guid id, CancellationToken ct)` → `void`

### IShoppingListItemRepository.cs

```
src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListItemRepository.cs
```

Metodi:
- `GetAllByListIdAsync(Guid listId, CancellationToken ct)` → `IReadOnlyList<ShoppingListItem>`
- `AddAsync(ShoppingListItem item, CancellationToken ct)` → `ShoppingListItem`
- `AddBulkAsync(IEnumerable<ShoppingListItem> items, CancellationToken ct)` → `int` (count aggiunto)
- `ToggleCheckedAsync(Guid id, CancellationToken ct)` → `ShoppingListItem`
- `DeleteAsync(Guid id, CancellationToken ct)` → `void`
- `DeleteCheckedByListIdAsync(Guid listId, CancellationToken ct)` → `void`
- `ExistsByNameAsync(Guid listId, string name, CancellationToken ct)` → `bool`

## Note

- Tutte le interfacce devono avere `/// <summary>` per ogni metodo.
- Seguire lo stesso pattern di `IFridgeRepository` / `IFridgeIngredientRepository`.

## File impattati

- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListRepository.cs` (nuovo)
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Interfaces/IShoppingListItemRepository.cs` (nuovo)
