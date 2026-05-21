---
id: TASK-001
feature: FEAT-005
type: task
status: planned
priority: high
created_at: 2026-05-21
related:
  - RFC-001
  - TASK-002
---

# TASK-001 - Estendere il catalogo tool KinAi per letture recipe

## Obiettivo

Allineare il catalogo tool dichiarato a OpenAI con i casi d'uso read-only richiesti per ricette e shopping list.

## Attivita'

- aggiungere o rifinire gli schema tool per:
  - `list_recipe_books`
  - `list_recipes`
  - `list_shopping_list_items`
  - `get_recipe_missing_ingredients`
- definire descrizioni e argomenti minimi necessari per identificare recipe book, recipe e shopping list;
- verificare che ogni tool dichiarato abbia un path esplicito nell'executor.

## Output atteso

- catalogo tool coerente tra `OpenAiChatService` e `KinHubChatToolExecutor`;
- nessun tool dichiarato ma non implementato.

