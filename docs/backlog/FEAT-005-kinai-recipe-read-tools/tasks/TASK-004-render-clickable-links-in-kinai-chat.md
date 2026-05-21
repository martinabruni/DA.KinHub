---
id: TASK-004
feature: FEAT-005
type: task
status: planned
priority: high
created_at: 2026-05-21
related:
  - TASK-003
  - CR-001
---

# TASK-004 - Rendere cliccabili i link nella chat KinAi

## Obiettivo

Fare in modo che i link ricetta presenti nei messaggi assistant siano azionabili dall'utente nella UI KinAi.

## Attivita'

- aggiornare `ConversationDetailPage.tsx` per trasformare gli URL presenti nel testo in anchor cliccabili;
- mantenere il rendering leggibile per testo multi-linea e messaggi senza link;
- configurare il deploy KinAi con la variabile ambiente `VITE_CORE_URL`.

## Output atteso

- il messaggio KinAi mostra link cliccabili senza rompere il layout attuale della conversazione;
- l'ambiente di deploy fornisce la base URL usata dai deep link.

