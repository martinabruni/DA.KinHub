---
id: TASK-014
feature: FEAT-001
type: task
status: implemented
priority: low
created_at: 2026-05-08
related:
  - TASK-016
  - TASK-019
  - TASK-020
---

# TASK-014 — Frontend i18n

## Descrizione

Aggiungere le chiavi di traduzione in `it.json` e `en.json`.

## Chiavi da aggiungere (namespace `shoppingLists`)

```json
{
  "shoppingLists": {
    "title": "Liste della spesa",
    "createList": "Nuova lista",
    "listName": "Nome lista",
    "rename": "Rinomina",
    "delete": "Elimina lista",
    "confirmDelete": "Sei sicuro di voler eliminare questa lista?",
    "confirmDeleteDescription": "L'azione è irreversibile. Tutti gli elementi verranno eliminati.",
    "addItem": "Aggiungi elemento",
    "itemName": "Nome elemento",
    "deleteChecked": "Cancella completati",
    "confirmDeleteChecked": "Eliminare tutti gli elementi completati?",
    "noLists": "Nessuna lista della spesa. Creane una!",
    "noItems": "Nessun elemento in questa lista.",
    "addToList": "Aggiungi alla lista",
    "selectList": "Seleziona lista",
    "itemsAdded": "{{count}} ingredienti aggiunti alla lista",
    "refresh": "Aggiorna",
    "itemCount": "{{checked}}/{{total}}"
  }
}
```

## Note

- `en.json` con traduzioni equivalenti in inglese.
- Seguire la struttura delle chiavi i18n già esistenti nel progetto.
- La chiave `itemsAdded` usa interpolazione `{{count}}` — verificare la sintassi i18n del progetto (potrebbe essere `{count}` a seconda della libreria usata).

## File impattati

- `src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/it.json`
- `src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/en.json`
