---
id: TASK-018
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-016
  - TASK-017
---

# TASK-018 — Frontend Routes

## Descrizione

Aggiungere le rotte per le nuove pagine in `routes.tsx`, all'interno del blocco `ServiceGuard serviceName="KinRecipe"`.

## Rotte da aggiungere

```tsx
{ path: '/shopping-lists', element: <ShoppingListsPage /> },
{ path: '/shopping-lists/:id', element: <ShoppingListDetailPage /> },
```

## Note

- Importare `ShoppingListsPage` e `ShoppingListDetailPage` in cima al file.
- Le rotte devono essere dentro il `ServiceGuard` con `serviceName="KinRecipe"` (stesso blocco di `fridges`, `recipe-books`, `ai-assistant`).

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/router/routes.tsx`
