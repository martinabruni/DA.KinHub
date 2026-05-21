---
id: BUG-002
feature: FEAT-003
type: bug
status: validated
priority: high
created_at: 2026-05-20
related:
  - TASK-003
  - TASK-004
---

# BUG-002 — I tool confermati non eseguono nessuna azione reale

## Descrizione

Quando l'utente approva un tool della chat, l'azione non viene realmente eseguita. Per esempio `create_shopping_list` non crea nessuna lista e `list_shopping_lists` non restituisce i dati richiesti.

## Comportamento atteso

- la conferma di un tool dispatcha l'azione applicativa corrispondente
- l'azione usa i servizi KinHub esistenti
- il risultato viene salvato nella conversazione e mostrato all'utente

## Comportamento attuale

- `ChatManager.UpdateToolCallStatusAsync(...)` cambia soltanto lo status del record `ChatToolCall`
- nessun servizio applicativo viene invocato dopo la conferma
- nessun messaggio di risultato viene scritto in cronologia
- i tool senza argomenti mostrano solo `{}` nella UI di approvazione

## File interessati

- `src/Presentations/Kin.KinHub.Shared.Api/ChatFeature/Controllers/ChatController.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Interfaces/IChatManager.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/ChatManager.cs`
- nuovi servizi ChatFeature per esecuzione tool
- servizi shopping list già esistenti in `RecipeFeature`

## Riproduzione

1. Chiedere a KinAi di creare una shopping list con alcuni prodotti
2. Approvare il tool mostrato dalla UI
3. Verificare che nessuna nuova lista sia presente nel dominio
4. Chiedere di mostrare tutte le liste della spesa
5. Verificare che la richiesta si fermi su una preview del comando senza restituire il contenuto delle liste
