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

# FEAT-003 — KinAi chat UX e tool execution

## Descrizione

Correggere due problemi funzionali in KinAi:

1. i messaggi della chat non distinguono correttamente utente e bot, mostrano label numeriche (`0`, `1`) e non garantiscono un layout coerente sinistra/destra con metadati leggibili;
2. i tool approvati dalla chat non eseguono nessuna azione reale, quindi richieste come creare o mostrare liste della spesa restano bloccate nella sola approvazione del comando.

## Problema affrontato

| Problema | Evidenza attuale |
|---|---|
| Label `0` / `1` in chat | Il frontend si aspetta ruoli stringa (`User`, `Assistant`, `Tool`), ma i DTO API possono serializzare gli enum come valori numerici |
| Allineamento errato dei bubble | `ConversationDetailPage.tsx` allinea a destra solo `message.role === 'User'`; con enum numerici tutto resta a sinistra |
| Timestamp da verificare / migliorare | La UI prova a renderizzare `createdAt`, ma va confermato che il payload arrivi correttamente e che l'orario sia mostrato in modo leggibile |
| Preview tool poco chiara | I tool senza argomenti mostrano `{}`
| Conferma tool senza effetto | `ConfirmToolCallAsync` aggiorna solo lo status `Pending -> Confirmed` e non invoca nessun servizio applicativo |
| Nessun risultato tool nella conversazione | Dopo la conferma non viene persistito nessun messaggio `Tool`/`Assistant` con esito o dati restituiti |

## Obiettivi

- Mostrare i messaggi dell'utente a destra e quelli del bot a sinistra in modo affidabile
- Sostituire label tecniche o numeriche con metadati leggibili
- Mostrare l'orario di invio del messaggio in chat
- Rendere la card di approvazione tool comprensibile anche per tool senza argomenti
- Eseguire davvero i tool shopping list dopo la conferma utente
- Restituire in chat l'esito dell'azione o i dati letti (es. elenco liste della spesa)

## Acceptance Criteria

- [ ] Nessun messaggio mostra label `0`, `1` o altri enum numerici
- [ ] I messaggi utente sono allineati a destra, quelli assistant/tool a sinistra
- [ ] Ogni messaggio mostra un orario leggibile e coerente con il locale del browser
- [ ] Le richieste `list_shopping_lists` non mostrano più soltanto `{}`
- [ ] La conferma di `create_shopping_list` crea davvero la lista e gli elementi richiesti
- [ ] La conferma di `list_shopping_lists` restituisce in chat l'elenco delle liste disponibili
- [ ] Lo stato dei tool resta tracciato (`Pending`, `Confirmed`, `Rejected`) senza perdere l'audit trail

## Moduli / file impattati

- `src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx`
- `src/Presentations/Kin.KinHub.KinAi.React/src/types/index.ts`
- `src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/it.json`
- `src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/en.json`
- `src/Presentations/Kin.KinHub.Shared.Api/ChatFeature/Controllers/ChatController.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Interfaces/IChatManager.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/ChatManager.cs`
- nuovi componenti/servizi ChatFeature per dispatch ed esecuzione tool
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Services/KinHubShoppingListItemService.cs`

## Stato finale
- ruolo messaggi stabilizzato tra backend e frontend
- bubble utente allineati a destra, assistant/tool a sinistra
- preview tool senza argomenti resa leggibile
- conferma tool shopping list collegata ai servizi dominio reali
- risultati tool persistiti come messaggi assistant nella conversazione
