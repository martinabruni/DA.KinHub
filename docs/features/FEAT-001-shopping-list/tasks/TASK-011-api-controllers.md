---
id: TASK-011
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-007
  - TASK-010
  - TASK-012
---

# TASK-011 — API Controllers

## Descrizione

Creare i controller nel progetto `Kin.KinHub.Shared.Api`, cartella `RecipeFeature/Controllers/`.

## File da creare

### ShoppingListController.cs

Route base: `api/shopping-lists`

| Metodo | Route | Action |
|---|---|---|
| GET | `/` | `GetAll` — lista tutte le liste della famiglia |
| POST | `/` | `Create` — crea nuova lista |
| PUT | `/{id:guid}` | `Update` — rinomina lista |
| DELETE | `/{id:guid}` | `Delete` — elimina lista (cascade) |

### ShoppingListItemController.cs

Route base: `api/shopping-lists/{listId:guid}/items`

| Metodo | Route | Action |
|---|---|---|
| GET | `/` | `GetAll` — tutti gli item della lista |
| POST | `/` | `Add` — aggiunge singolo item |
| POST | `/bulk` | `BulkAdd` — aggiunge più item (ritorna `BulkAddShoppingListItemsResponse`) |
| PATCH | `/{itemId:guid}/toggle` | `Toggle` — toggle IsChecked |
| DELETE | `/checked` | `DeleteChecked` — elimina tutti i checked |
| DELETE | `/{itemId:guid}` | `DeleteItem` — elimina item singolo |

## ⚠️ Nota critica sul routing

In `ShoppingListItemController`, l'action `[HttpDelete("checked")]` DEVE essere dichiarata **prima** di `[HttpDelete("{itemId:guid}")]` nel file, altrimenti ASP.NET Core potrebbe tentare di interpretare "checked" come un Guid.

## Note

- `userId` si ricava da `User.FindFirstValue(ClaimTypes.NameIdentifier)` o equivalente.
- Restituire `404 Not Found` se la lista non appartiene alla famiglia.
- Seguire il pattern `FridgeController` come riferimento.

## File impattati

- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListController.cs` (nuovo)
- `src/Presentations/Kin.KinHub.Shared.Api/RecipeFeature/Controllers/ShoppingListItemController.cs` (nuovo)
