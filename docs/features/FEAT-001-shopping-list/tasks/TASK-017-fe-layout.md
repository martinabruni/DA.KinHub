---
id: TASK-017
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-018
---

# TASK-017 — Frontend Layout Nav

## Descrizione

Aggiungere il nav item "Liste della spesa" in `KinRecipeServiceLayout.tsx`.

## Modifica richiesta

Aggiungere alla lista dei nav items:

```tsx
{ to: '/shopping-lists', icon: ShoppingCart, labelKey: 'nav.shoppingLists' }
```

Importare `ShoppingCart` da `lucide-react`.

Aggiungere la chiave `nav.shoppingLists` ai file i18n (può essere fatto in TASK-014).

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/components/KinRecipeServiceLayout.tsx`
