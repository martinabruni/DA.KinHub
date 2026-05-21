---
id: TASK-002
feature: FEAT-005
type: task
status: planned
priority: high
created_at: 2026-05-21
related:
  - TASK-001
  - TASK-003
---

# TASK-002 - Implementare i tool read-only per ricette e liste

## Obiettivo

Rendere eseguibili dal backend i nuovi tool KinAi che leggono recipe books, recipes, shopping list items e ingredienti mancanti.

## Attivita'

- iniettare nell'executor i servizi business necessari;
- leggere i dati usando i boundary applicativi gia' presenti invece di richiamare i controller HTTP;
- formattare output leggibili, sintetici e stabili per la chat;
- applicare validazioni esplicite sugli identificativi richiesti dai tool.

## Output atteso

- KinAi restituisce dati reali per libri ricette, ricette, elementi lista spesa e ingredienti mancanti;
- gli errori di input o autorizzazione restano espliciti e coerenti con il sistema di result esistente.

