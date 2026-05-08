---
id: FEAT-001
type: feature
status: planned
priority: high
created_at: 2026-05-08
related: []
---

# FEAT-001 — Lista della Spesa Condivisa (KinRecipe)

## Descrizione

Aggiunta di una sezione "Shopping List" alla feature KinRecipe. Ogni famiglia può avere più liste della spesa condivise. Gli item hanno solo `Name` e `IsChecked`. La feature si integra con il calcolo degli ingredienti mancanti (via `IRecipeMissingIngredientsService`), permettendo di aggiungere in un clic gli ingredienti mancanti a una lista direttamente da `RecipeDetailPage` e dalla `SuggestTab` dell'AI Assistant.

## Decisioni architetturali

| Decisione | Scelta |
|---|---|
| Più liste per famiglia | Sì (come Fridge) |
| Campi item | Solo Name + IsChecked |
| Soft/Hard delete | Hard delete (`BaseEntity<Guid>`, no `BaseDeletableEntity`) |
| FK ShoppingListItem → ShoppingList | ON DELETE CASCADE |
| Dedup item | Case-insensitive per Name, all'interno della stessa lista |
| Real-time sync | No — pulsante Refresh manuale |
| Ordinamento liste | UpdatedAt DESC |
| Ordinamento item | Unchecked (CreatedAt ASC) poi Checked (CreatedAt ASC) |
| Format count su card | "X/Y" (CheckedCount/ItemCount) |
| Item checked nella detail page | In fondo, con line-through + opacity-50 |
| Rinomina lista | Via MoreHorizontal menu (Dialog) |
| Elimina lista | AlertDialog di conferma |
| AdaptTab | Nessuna integrazione (rimossa) |
| Bulk response | `{ addedCount: N }` |
| Toast | "N ingredienti aggiunti alla lista" |
| MaxLength Name lista | 200 |
| MaxLength Name item | 200 |
| Route conflict DELETE | `[HttpDelete("checked")]` prima di `[HttpDelete("{itemId:guid}")]` |

## API Endpoints

```
GET    /api/shopping-lists
POST   /api/shopping-lists
PUT    /api/shopping-lists/{id}
DELETE /api/shopping-lists/{id}

GET    /api/shopping-lists/{listId}/items
POST   /api/shopping-lists/{listId}/items
POST   /api/shopping-lists/{listId}/items/bulk
PATCH  /api/shopping-lists/{listId}/items/{itemId}/toggle
DELETE /api/shopping-lists/{listId}/items/{itemId}
DELETE /api/shopping-lists/{listId}/items/checked
```

## Pattern di riferimento

- `Fridge` / `FridgeIngredient` (entity, repository, service, controller, provider, pages)
- `KinRecipeServiceLayout` per la nav
- `FridgesPage` per grid + MoreHorizontal menu + AlertDialog

## Dipendenze esterne

- `IRecipeMissingIngredientsService` (già esistente) per calcolo mancanti
- `IFamilyMemberService` per ricavare `FamilyId` dall'utente loggato

## Rischi

- Route ambiguity `DELETE /items/checked` vs `DELETE /items/{itemId:guid}` → risolto con ordine dichiarazione controller
- `UpdatedAt` della lista deve essere aggiornato lato repository/service ogni volta che un item viene modificato

## Acceptance Criteria

- [ ] Famiglia può creare/rinominare/eliminare liste
- [ ] Item aggiungibile singolarmente o in bulk (dedup case-insensitive)
- [ ] Item toggleable (checked/unchecked)
- [ ] Bulk delete dei checked
- [ ] Card lista mostra "X/Y"
- [ ] RecipeDetailPage mostra Select lista + Aggiungi per gli ingredienti mancanti
- [ ] SuggestTab mostra Select lista + Aggiungi per recipe con mancanti > 0
- [ ] Toast conferma con conteggio item aggiunti
