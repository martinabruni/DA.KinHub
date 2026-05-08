---
id: TASK-016
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-013
  - TASK-014
  - TASK-015
  - TASK-017
  - TASK-018
---

# TASK-016 — Frontend Pages

## Descrizione

Creare le due pagine principali in `src/features/shopping-lists/pages/`.

---

## ShoppingListsPage.tsx

### Comportamento

- Grid di card come `FridgesPage`
- Icona: `ShoppingCart` da `lucide-react`
- Ordinamento: `UpdatedAt DESC` (già gestito dal backend)
- Ogni card mostra: `Name` + count `"CheckedCount/ItemCount"`
- MoreHorizontal menu per ogni card:
  - **Rinomina** → Dialog con input (come FridgesPage)
  - **Elimina** → AlertDialog di conferma prima di eliminare
- Pulsante "+ Nuova lista" → Dialog con form
- Pulsante Refresh manuale → `queryClient.invalidateQueries`
- Stato empty: messaggio + CTA

---

## ShoppingListDetailPage.tsx

### Comportamento

- Titolo lista + pulsante Refresh manuale
- Form inline per aggiungere un nuovo item
- Lista item ordinata: **unchecked prima** (CreatedAt ASC), **checked in fondo** (CreatedAt ASC)
  - Item checked: `line-through opacity-50`
- Ogni item ha:
  - Checkbox per toggle `IsChecked`
  - Pulsante elimina (icona `Trash2`)
- Pulsante "Cancella completati" → AlertDialog di conferma → `DELETE /items/checked`
- Stato empty: messaggio

---

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListsPage.tsx` (nuovo)
- `src/Presentations/Kin.KinHub.Core.React/src/features/shopping-lists/pages/ShoppingListDetailPage.tsx` (nuovo)
