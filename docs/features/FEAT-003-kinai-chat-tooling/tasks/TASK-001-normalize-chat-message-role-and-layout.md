---
id: TASK-001
feature: FEAT-003
type: task
status: validated
priority: high
created_at: 2026-05-20
related:
  - BUG-001
---

# TASK-001 — Normalizzare il ruolo messaggio e correggere l'allineamento chat

## Obiettivo

Rendere affidabile il mapping del mittente (`user`, `assistant`, `tool`) così che il frontend possa decidere correttamente label, stile e allineamento del bubble.

## Implementazione prevista

- verificare la serializzazione degli enum `ChatMessageRole` e `ChatToolCallStatus` nell'API
- decidere se correggere il contratto lato backend (enum come stringa) o normalizzare il payload lato frontend prima del render
- introdurre una funzione/mapper riusabile che trasformi valori numerici o stringa in un ruolo UI stabile
- aggiornare `ConversationDetailPage.tsx` per usare il ruolo normalizzato anziché `message.role` raw

## Note

La correzione preferita è stabilizzare il contratto API o il mapper di lettura, non fare affidamento su confronti stringa sparsi nel componente.
