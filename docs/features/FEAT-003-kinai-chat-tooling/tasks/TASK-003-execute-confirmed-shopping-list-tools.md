---
id: TASK-003
feature: FEAT-003
type: task
status: validated
priority: high
created_at: 2026-05-20
related:
  - BUG-002
  - RFC-001
---

# TASK-003 — Eseguire davvero i tool shopping list dopo la conferma

## Obiettivo

Introdurre una pipeline backend che dispatchi i tool confermati verso i servizi KinHub esistenti per liste della spesa.

## Implementazione prevista

- estendere il flusso `ConfirmToolCallAsync` per ricevere anche il contesto utente necessario ai servizi dominio (`UserId` oltre a `FamilyMemberId`)
- introdurre un orchestratore dedicato (es. `IChatToolExecutor`) in ChatFeature
- mappare almeno questi tool:
  - `list_shopping_lists`
  - `create_shopping_list`
  - `add_shopping_list_item`
- parsare `argumentsJson` in request validate prima della dispatch
- riusare `IShoppingListService` e `IShoppingListItemService` invece di duplicare logica dominio

## Rischi tecnici

- mismatch tra naming JSON dei tool (`shopping_list_id`, `item_name`) e request .NET
- necessità di propagare correttamente autorizzazione e ownership tra member chat e user dominio
- gestione errori tool senza perdere lo stato del record `ChatToolCall`
