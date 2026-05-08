---
id: TASK-020
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-013
  - TASK-015
---

# TASK-020 — AIAssistantPage SuggestTab Integration

## Descrizione

Modificare `AIAssistantPage.tsx` — **SuggestTab** — per permettere l'aggiunta degli ingredienti mancanti a una lista della spesa.

## Modifica richiesta

Per ogni card nella SuggestTab (sia `existingRecipes` che `newRecipes`) dove `missingIngredients.length > 0`:

1. Aggiungere una `Select` con le shopping lists della famiglia
2. Aggiungere un pulsante "Aggiungi alla lista"
3. Al click: chiamare `POST /api/shopping-lists/{selectedListId}/items/bulk` con i nomi degli ingredienti mancanti della card
4. Al successo: mostrare un toast "N ingredienti aggiunti alla lista"

## Stato locale

- Ogni card ha il proprio stato `selectedListId` (non condiviso tra card diverse)
- Suggerito: `Map<cardIndex/recipeId, string>` o stato per-card

## Fetch delle shopping lists

Le shopping lists vengono fetchate una volta sola per l'intera pagina con `useQuery`:

```tsx
const { data: shoppingLists } = useQuery({
  queryKey: ['shopping-lists'],
  queryFn: () => fetchShoppingLists(),
});
```

## AdaptTab

Nessuna modifica alla AdaptTab.

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/features/ai-assistant/pages/AIAssistantPage.tsx`
