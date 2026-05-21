---
id: RFC-001
feature: FEAT-005
type: research
status: planned
priority: high
created_at: 2026-05-21
related:
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
---

# RFC-001 - Output dei nuovi tool read-only KinAi

## Osservazioni iniziali

1. `OpenAiChatService` espone gia' il catalogo tool ma oggi copre solo una parte del dominio recipe/shopping.
2. `KinHubChatToolExecutor` restituisce testo semplice tramite `MessageContent`.
3. `ConversationDetailPage.tsx` visualizza testo multi-linea ma non rende gli URL come link cliccabili.

## Decisione target

Mantenere il canale di risposta attuale basato su testo assistant, ma standardizzare il formato per liste e deep link:

- intestazione breve;
- elenco puntato per elementi multipli;
- URL assoluto esplicito quando una ricetta e' navigabile.

## Conseguenze

- niente nuovo payload strutturato tra backend chat e frontend KinAi;
- implementazione piu' rapida e coerente con il contratto esistente;
- il frontend deve aggiungere solo rendering link-safe, non nuove DTO o card dedicate.

