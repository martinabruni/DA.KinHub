---
id: TASK-013
feature: FEAT-001
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-015
  - TASK-016
---

# TASK-013 — Frontend Types

## Descrizione

Aggiungere le interfacce TypeScript in `src/types/index.ts`.

## Interfacce da aggiungere

```ts
export interface ShoppingList {
  id: string;
  name: string;
  familyId: string;
  itemCount: number;
  checkedCount: number;
  updatedAt: string;
}

export interface ShoppingListItem {
  id: string;
  name: string;
  isChecked: boolean;
  createdAt: string;
}

export interface BulkAddShoppingListItemsResponse {
  addedCount: number;
}
```

## Note

- `itemCount` e `checkedCount` sono entrambi necessari per il formato "X/Y" nelle card.
- `updatedAt` è necessario per ordinare le liste per `UpdatedAt DESC`.

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/types/index.ts`
