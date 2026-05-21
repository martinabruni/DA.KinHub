---
id: RFC-001
feature: FEAT-003
type: research
status: validated
priority: high
created_at: 2026-05-20
related:
  - TASK-003
  - TASK-004
---

# RFC-001 — Architettura per esecuzione tool KinAi con traceability

## Obiettivo

Definire un flusso di esecuzione tool che mantenga audit trail, riusi i servizi dominio esistenti e riporti il risultato nella conversazione.

## Architettura proposta

1. `ChatController` passa sia `UserId` sia `FamilyMemberId` al manager chat per le operazioni sui tool.
2. `ChatManager` mantiene ownership e status del `ChatToolCall`, ma delega l'esecuzione a un servizio dedicato.
3. `IChatToolExecutor` riceve:
   - tool name
   - arguments JSON
   - contesto utente corrente
   - conversation / message metadata necessari
4. L'executor dispatcha il tool ai servizi applicativi esistenti (`IShoppingListService`, `IShoppingListItemService`).
5. Il risultato viene convertito in testo persistibile in chat e salvato come messaggio di sistema/tool o assistant.

## Dipendenze

- `IShoppingListService`
- `IShoppingListItemService`
- repository chat esistenti (`IChatMessageRepository`, `IChatToolCallRepository`, `IChatConversationRepository`)
- serializzazione/deserializzazione JSON affidabile per gli argomenti dei tool

## Rischi

- tool call approvato ma fallito a metà: serve definire se lasciare `Confirmed` + messaggio d'errore o introdurre stato più ricco in futuro
- tool no-args come `list_shopping_lists` hanno UX diversa da tool mutanti
- crescita futura: recipe books, recipes e fridge richiederanno un dispatcher estensibile e testabile

## Fasi implementative

1. Stabilizzare contratto e UX chat
2. Aggiungere executor e DTO di dispatch per shopping lists
3. Collegare `ConfirmToolCallAsync` all'esecuzione reale
4. Persistire risultati ed errori nella conversazione
5. Estendere lo stesso pattern agli altri tool OpenAI

## Decisioni aperte

- usare messaggi `Tool` persistiti oppure materializzare direttamente un messaggio `Assistant` già pronto per la UI
- decidere se il risultato del tool deve essere rimandato anche all'LLM per generare una risposta naturale aggiuntiva nello stesso flusso
