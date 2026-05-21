---
id: TASK-002
feature: FEAT-003
type: task
status: validated
priority: medium
created_at: 2026-05-20
related:
  - BUG-001
  - BUG-002
---

# TASK-002 — Migliorare metadata messaggi e preview dei tool

## Obiettivo

Rendere la UI della conversazione comprensibile per l'utente finale, mostrando mittente e orario in modo leggibile e sostituendo preview tecniche poco utili come `{}`.

## Implementazione prevista

- mostrare etichette localizzate per utente / assistente / tool
- confermare che `createdAt` venga renderizzato e adattare il formato per evidenziare l'ora del messaggio
- introdurre un formatter per i tool senza argomenti, così `list_shopping_lists` e tool analoghi mostrino una descrizione umana invece di `{}` 
- aggiornare le traduzioni KinAi dove serve

## File da toccare

- `src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx`
- `src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/it.json`
- `src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/en.json`
