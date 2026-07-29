# Piano di implementazione - CR-FEAT-001-002

## Obiettivo

Fare sopravvivere il login MSAL al refresh della stessa scheda senza persistere contesto famiglia o dati personali applicativi e senza modificare i contratti backend esistenti.

## Prerequisiti e readiness

- Nessuna feature prerequisite: FEAT-001 e la sua pipeline HTTP sono gia integrate.
- Nessun gate bloccante: la responsabile ha richiesto esplicitamente il nuovo risultato e questa CR limita la persistenza a `sessionStorage`.
- Verifica tecnica locale: confermare, con test contro la configurazione reale di MSAL e prova browser, che la versione installata supporti `sessionStorage` e che un refresh ripristini l'account prima del bootstrap.
- Dettagli delegabili: organizzazione del test di integrazione/frontend e scelta della prova manuale browser, senza introdurre storage aggiuntivo.

## Fonti e vincoli

| Fonte | Ruolo |
|---|---|
| `docs/brainstorming/functional-analysis.md` | FLOW-001 e requisiti di accesso, routing, PWA, privacy e osservabilita da preservare. |
| `docs/brainstorming/architecture.md` | MSAL/Entra, bootstrap server-side, API network-only e divieto di dati applicativi offline; la CR circoscrive l'eccezione per token/account MSAL. |
| `docs/backlog/features/accesso-instradamento/feature.md` | Contratto FEAT-001 gia consegnato e stati distinti di sessione/onboarding/offline. |
| `docs/backlog/features/accesso-instradamento/feature.plan.md` | Decisione `memoryStorage` da sostituire in modo esplicito. |
| `AGENTS.md` | Requisiti frontend, PWA, sicurezza, documentazione, change fragment e verifiche. |
| `src/frontend/src/lib/auth.ts` | Unico punto di configurazione MSAL da modificare. |

## Sequenza di consegna

1. Aggiungere un test mirato alla configurazione MSAL che distingua `sessionStorage` da `memoryStorage` e `localStorage`; non inserire token reali nei test.
2. Sostituire la cache MSAL in `src/frontend/src/lib/auth.ts` con `sessionStorage`, mantenendo authority, redirect URI, scope, popup e broker invariati.
3. Verificare che `ProtectedRoute` riconosca l'account ripristinato e che `KinListAccessGate` parta senza `familyId`, acquisisca il token silenziosamente e richiami il bootstrap. Non memorizzare il risultato in browser storage.
4. Aggiungere o estendere test componenti per il percorso account ripristinato, bootstrap riuscito e stati fail-closed `401`/`403`/offline; aggiornare i test esistenti che esprimono il vincolo "solo in memoria" affinche distinguano contesto famiglia e cache MSAL.
5. Aggiornare `docs/operations/entra-external-id.md`, `docs/brainstorming/architecture.md` e `feature.plan.md` con la nuova regola; aggiornare questo indice backlog per il collegamento della CR.
6. Creare il change fragment bilingue, rigenerare gli artefatti derivati e completare le verifiche frontend e documentali.

## Contratto di implementazione

### Comportamento atteso

- Il refresh conserva esclusivamente lo stato MSAL necessario a trovare l'account e richiedere token per la sessione della scheda.
- La PWA non considera il ripristino MSAL equivalente a un'autorizzazione: su `/kinlist` chiama sempre il bootstrap e mostra loading senza dati familiari finche non riceve l'esito.
- Un token non rinnovabile o risposta server non autorizzata segue gli stati esistenti di nuovo accesso/accesso negato e pulisce il contesto memoria.
- Il logout continua a delegare a MSAL il cleanup della cache della sessione e non deve lasciare `familyId` nella UI.

### Touchpoint previsti

- **Dominio/business**: Non pertinente.
- **Persistenza/migration**: Non pertinente.
- **API/integrazioni**: `src/frontend/src/lib/auth.ts`, `src/frontend/src/lib/api.ts` soltanto se necessario per testare l'acquisizione silenziosa; nessuna modifica di contratto API.
- **Frontend/UX**: `src/frontend/src/components/ProtectedRoute.tsx`, `src/frontend/src/components/KinHubFamilyContext.tsx`, `src/frontend/src/components/KinListAccessGate.tsx` e i test mirati corrispondenti.
- **Infrastruttura/configurazione**: Nessuna.
- **Documentazione/operazioni**: `docs/operations/entra-external-id.md`, `docs/brainstorming/architecture.md`, `docs/backlog/README.md`, piano FEAT-001, change fragment e artefatti generati dal repository.

### Errori, sicurezza e osservabilita

- Non modificare mapping Problem Details, policy, autorizzazione o cache HTTP.
- Non trattare un account in `sessionStorage` come identita o membership verificata; il token e il bootstrap rimangono gli unici ingressi previsti.
- Non aggiungere log diagnostici di token, account, claim o chiavi browser; le metriche esistenti restano aggregate e redatte.
- Preservare l'invalidazione del contesto famiglia su logout, cambio account, `401`, `403`, offline e unmount.

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Configurazione MSAL | Test che vincola `sessionStorage` ed esclude `memoryStorage` e `localStorage`. |
| Frontend/component | Route e bootstrap | Account disponibile dopo inizializzazione, loading senza contesto residuo, bootstrap family/onboarding e stati `401`/`403`/offline. |
| Sicurezza browser | Storage e logout | Ispezione automatizzata o manuale: nessun dato familiare in browser storage; logout rimuove la cache MSAL della sessione. |
| End-to-end/manuale | Chrome desktop, Chrome Android/PWA ed Edge | Login, refresh di `/kinlist`, bootstrap autorevole, logout, sessione Entra scaduta e offline senza leak. |
| Validator repository | Frontend e strumenti | `npm run test`, `npm run lint`, `npm run typecheck`, `npm run i18n:validate`, `npm run routes:validate`, `npm run design-system:validate`, `npm run build`, `npm run docs:validate`, `npm run docs:sync`, `npm run skills:validate`, `npm run skills:build` e `npm run release:validate`. |

## Rollout e rollback

- Il rilascio e un asset frontend compatibile con le API esistenti; non richiede migration o aggiornamenti infrastrutturali.
- Dopo il deploy verificare manualmente l'assenza di logout al refresh e gli stati fail-closed indicati.
- In caso di regressione, distribuire l'asset frontend N-1. Il rollback riporta il requisito di login dopo refresh, senza effetti su dati o server.

## Definition of Done

- Tutti i criteri della CR sono verificati con test e prova browser sui target prioritari.
- La cache MSAL e limitata a `sessionStorage`; nessun nuovo storage contiene token/account o dati familiari oltre il perimetro esplicitamente approvato.
- Bootstrap, invalidazione del contesto famiglia, offline, logout, `401` e `403` restano fail-closed e coperti da test pertinenti.
- Documentazione architetturale e operativa, piano FEAT-001, indice backlog e change fragment bilingue sono aggiornati; gli artefatti derivati sono rigenerati, non modificati manualmente.
- I comandi di qualita applicabili di `AGENTS.md` completano con successo e non sono introdotti secret, modifiche backend/infrastruttura o elementi fuori scope.
