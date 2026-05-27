---
id: FEAT-003
type: feature
status: validated
priority: high
created_at: 2026-05-20
related:
  - BUG-001
  - BUG-002
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
  - RFC-001
---

# FEAT-003 - Transizione verso MCP server

## Descrizione

Questa feature documenta la dismissione dell'assistente conversazionale legacy e la pulizia completa delle sue superfici applicative.

## Risultato finale

- rimosso il frontend dedicato non piu' necessario;
- rimossi endpoint, servizi, persistenza e workflow correlati;
- riallineata la documentazione per spiegare che l'evoluzione futura passa da un MCP server.

## Motivazione

La soluzione precedente e' stata ritirata per semplificare l'architettura e concentrare l'integrazione futura su un modello basato su MCP server.
