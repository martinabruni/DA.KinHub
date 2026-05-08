---
id: TASK-019
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-013
  - TASK-015
---

# TASK-019 — RecipeDetailPage Integration

## Descrizione

Modificare `RecipeDetailPage.tsx` per permettere l'aggiunta degli ingredienti mancanti a una lista della spesa.

## Modifica richiesta

Dopo il blocco che mostra `missingIngredients` (quando `missingIngredients !== null && missingIngredients.length > 0`):

1. Aggiungere una `Select` con le shopping lists della famiglia
2. Aggiungere un pulsante "Aggiungi alla lista"
3. Al click: chiamare `POST /api/shopping-lists/{selectedListId}/items/bulk` con i nomi degli ingredienti mancanti
4. Al successo: mostrare un toast "N ingredienti aggiunti alla lista"

## Fetch delle shopping lists

Le shopping lists vengono fetchate indipendentemente con `useQuery` direttamente nella pagina (non tramite provider):

```tsx
const { data: shoppingLists } = useQuery({
  queryKey: ['shopping-lists'],
  queryFn: () => fetchShoppingLists(),
});
```

## Stato locale

- `selectedListId: string | null` — lista selezionata nella Select

## Note

- Disabilitare il pulsante se `selectedListId` è null o se la mutation è in corso.
- Seguire il pattern toast già usato nelle altre pagine del progetto.

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/features/recipes/pages/RecipeDetailPage.tsx`
