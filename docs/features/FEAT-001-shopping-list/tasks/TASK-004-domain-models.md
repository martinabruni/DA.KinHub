---
id: TASK-004
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-005
  - TASK-006
---

# TASK-004 — Domain Models

## Descrizione

Creare i modelli di dominio nel progetto `Kin.KinHub.Core.Domain`, cartella `RecipeFeature/Models/`.

## File da creare

### ShoppingList.cs

```
src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingList.cs
```

Eredita da `BaseEntity<Guid>`.  
Proprietà: `Name` (string), `FamilyId` (Guid)

### ShoppingListItem.cs

```
src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingListItem.cs
```

Eredita da `BaseEntity<Guid>`.  
Proprietà: `Name` (string), `IsChecked` (bool), `ShoppingListId` (Guid)

## Note

- Modelli anemici (solo proprietà, nessuna logica).
- Seguire il pattern `Fridge` / `FridgeIngredient` nella stessa cartella.
- I nomi delle proprietà devono corrispondere esattamente all'entity Infrastructure per il mapping Mapster zero-config.

## File impattati

- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingList.cs` (nuovo)
- `src/Domains/Kin.KinHub.Core.Domain/RecipeFeature/Models/ShoppingListItem.cs` (nuovo)
