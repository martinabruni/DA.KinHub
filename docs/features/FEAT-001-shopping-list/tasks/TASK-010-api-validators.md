---
id: TASK-010
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-006
  - TASK-011
---

# TASK-010 — API Validators

## Descrizione

Creare i validator FluentValidation nel progetto `Kin.KinHub.Shared.Api`, cartella `RecipeFeature/Validators/`.

## File da creare

### CreateShoppingListRequestValidator.cs

- `Name`: NotEmpty, MaxLength(200)

### UpdateShoppingListRequestValidator.cs

- `Name`: NotEmpty, MaxLength(200)

### CreateShoppingListItemRequestValidator.cs

- `Name`: NotEmpty, MaxLength(200)
- `ShoppingListId`: NotEmpty (deve essere un Guid valido)

### BulkAddShoppingListItemsRequestValidator.cs

- `Names`: NotEmpty (lista non vuota)
- Ogni elemento di `Names`: NotEmpty, MaxLength(200)
- `ShoppingListId`: NotEmpty

## Note

- Seguire il pattern dei validator esistenti per le altre feature (es. `CreateFridgeRequestValidator`).
- I validator vengono registrati automaticamente tramite `AddFluentValidation` se già configurato; altrimenti registrarli manualmente in DI.

## File impattati

- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListRequestValidator.cs` (nuovo)
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/UpdateShoppingListRequestValidator.cs` (nuovo)
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/CreateShoppingListItemRequestValidator.cs` (nuovo)
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Validators/BulkAddShoppingListItemsRequestValidator.cs` (nuovo)
