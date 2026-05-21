---
id: BUG-001
feature: FEAT-003
type: bug
status: validated
priority: high
created_at: 2026-05-20
related:
  - TASK-001
  - TASK-002
---

# BUG-001 — Messaggi chat con label numeriche e layout non corretto

## Descrizione

Nella chat KinAi i messaggi mostrano identificatori tecnici (`0`, `1`) per riconoscere il mittente e i bubble possono risultare tutti allineati a sinistra.

## Comportamento atteso

- messaggi utente a destra
- messaggi bot a sinistra
- metadati leggibili per capire chi ha inviato il messaggio
- orario del messaggio visibile

## Comportamento attuale

- `ConversationDetailPage.tsx` usa `message.role === 'User'` per decidere l'allineamento
- il frontend tipizza `role` come string union, ma il backend espone enum che possono arrivare come numeri
- il titolo del bubble stampa direttamente `message.role`, quindi eventuali enum serializzati diventano `0`, `1`, `2`

## File interessati

- `src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx`
- `src/Presentations/Kin.KinHub.KinAi.React/src/types/index.ts`
- eventuali DTO / serializzazione enum lato API chat

## Riproduzione

1. Aprire una conversazione KinAi
2. Inviare un messaggio
3. Verificare che il bubble mostri `0` / `1` e che non ci sia una distinzione visiva affidabile tra utente e assistant
