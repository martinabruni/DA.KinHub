---
id: TASK-015
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-013
  - TASK-016
---

# TASK-015 — Frontend Provider

## Descrizione

Creare `ShoppingListProvider.tsx` in `src/features/shopping-lists/`.

## Responsabilità

- Context + hooks per operazioni CRUD sulle liste e sugli item
- Seguire il pattern `FridgeProvider.tsx`
- Usare React Query (`useQuery`, `useMutation`, `queryClient.invalidateQueries`)

## API calls da wrappare

### Liste

- `GET /api/shopping-lists` — `useShoppingLists()`
- `POST /api/shopping-lists` — `useCreateShoppingList()`
- `PUT /api/shopping-lists/{id}` — `useUpdateShoppingList()`
- `DELETE /api/shopping-lists/{id}` — `useDeleteShoppingList()`

### Item

- `GET /api/shopping-lists/{listId}/items` — `useShoppingListItems(listId)`
- `POST /api/shopping-lists/{listId}/items` — `useAddShoppingListItem()`
- `POST /api/shopping-lists/{listId}/items/bulk` — `useBulkAddShoppingListItems()`
- `PATCH /api/shopping-lists/{listId}/items/{itemId}/toggle` — `useToggleShoppingListItem()`
- `DELETE /api/shopping-lists/{listId}/items/{itemId}` — `useDeleteShoppingListItem()`
- `DELETE /api/shopping-lists/{listId}/items/checked` — `useDeleteCheckedShoppingListItems()`

## Note

- `FridgeProvider` è usato localmente per pagina, non hoistato in `Layout.tsx` — seguire lo stesso approccio.
- Per `RecipeDetailPage` e `AIAssistantPage`, le shopping lists vengono fetchate indipendentemente (non tramite provider) con `useQuery`.
- Il refresh manuale viene gestito tramite `queryClient.invalidateQueries` con il query key corrispondente.

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/ShoppingListProvider.tsx` (nuovo)
