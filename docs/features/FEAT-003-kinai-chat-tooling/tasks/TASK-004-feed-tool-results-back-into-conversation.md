---
id: TASK-004
feature: FEAT-003
type: task
status: validated
priority: high
created_at: 2026-05-20
related:
  - BUG-002
  - TASK-003
---

# TASK-004 — Persistire il risultato del tool nella conversazione

## Obiettivo

Dopo l'esecuzione di un tool confermato, salvare e mostrare nella conversazione un risultato utile invece di lasciare la chat senza feedback.

## Implementazione prevista

- definire il formato del risultato tool (messaggio `Tool`, messaggio `Assistant`, o entrambi)
- salvare almeno un output testuale ricostruibile per audit e UX
- per `list_shopping_lists`, mostrare i nomi delle liste trovate
- per `create_shopping_list`, mostrare nome lista creata e numero elementi aggiunti
- invalidare/rileggere la conversazione frontend in modo che il nuovo output compaia subito

## Dipendenze

- richiede TASK-003 completato per avere un executor funzionante
