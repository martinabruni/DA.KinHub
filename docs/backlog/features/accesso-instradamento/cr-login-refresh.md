# CR-FEAT-001-002 - Mantenere la sessione di accesso durante il refresh

- **Feature interessata**: FEAT-001 `accesso-instradamento`
- **Tipo**: correzione di comportamento frontend e sicurezza della sessione
- **Stato**: pianificata
- **Breaking change prodotto**: no
- **Piano**: `cr-login-refresh.plan.md`
- **Piano originario**: `feature.plan.md`

## Motivazione

La configurazione MSAL originaria di FEAT-001 usava `memoryStorage`. Un refresh ricreava la SPA, svuotava tale memoria e faceva quindi apparire l'utente disconnesso, pur senza eseguire un logout presso Entra.

Il risultato richiesto e che un utente gia autenticato resti autenticato dopo un refresh della stessa scheda e che la PWA riesegua il bootstrap autorevole. La CR sostituisce quindi il vincolo esecutivo di FEAT-001 che richiedeva i token esclusivamente in memoria.

## Comportamento attuale

- Prima della CR, `src/frontend/src/lib/auth.ts` configurava MSAL con `cacheLocation: "memoryStorage"`.
- Al reload `getActiveAccount()` non trova alcun account e `ProtectedRoute` mostra il percorso di accesso.
- `KinHubFamilyContext` conserva il solo `familyId` in memoria e `KinListAccessGate` lo cancella prima del bootstrap; questo comportamento evita dati familiari residui ed e corretto.

## Comportamento desiderato

- MSAL conserva account e token nella `sessionStorage` della singola sessione di navigazione, cosi da poter acquisire silenziosamente un token dopo un refresh della stessa scheda.
- Dopo il refresh, la route protetta non richiede di nuovo il login se la sessione Entra e ancora valida; KinList esegue nuovamente il bootstrap e ricostruisce il contesto famiglia solo dalla risposta API corrente.
- Se il token non puo essere acquisito silenziosamente, e scaduto, revocato o la sessione Entra non e piu valida, l'app continua a mostrare il flusso localizzato di nuovo accesso senza dati familiari.
- Logout esplicito, cambio account, `401`, `403`, offline e unmount continuano a rimuovere il contesto famiglia dalla memoria React; nessun `familyId`, dato familiare o risposta API viene scritto in `sessionStorage`.

## Decisione che sostituisce

Questa CR sostituisce soltanto il punto `feature.plan.md` relativo alla cache MSAL: token e account non restano piu esclusivamente in memoria del processo JavaScript, ma nella `sessionStorage` del browser limitata alla sessione della scheda. Restano invariati i divieti per `localStorage`, Cache API, IndexedDB, service worker, log, metriche, trace e dati familiari.

La formulazione generale di `docs/brainstorming/architecture.md` sulle persistenze browser e quella specifica di FEAT-001 risultano pertanto superate per il solo token/account MSAL. Il divieto di persistere dati personali applicativi e risposte delle API rimane invariato.

## Contratti invariati

- Entra External ID resta l'unico provider di identita e MSAL resta il client della SPA.
- Le API bearer restano network-only, usano token acquisiti da MSAL e non ricevono Function key.
- `ApiAccess`, `Family`, `(iss, oid)`, `familyId` in query e tutti i contratti HTTP non cambiano.
- `familyId` e dati familiari restano in memoria e sono ricostruiti dal bootstrap dopo ogni refresh.
- Le risposte autenticate restano `Cache-Control: no-store, private`.
- Token, account, claim completi, issuer, oid, `familyId`, nomi e payload non entrano in log, metriche o trace.

## Scope

- Configurazione della cache MSAL in `src/frontend/src/lib/auth.ts` per una sessione che sopravvive al refresh della scheda.
- Test del contratto di configurazione MSAL e degli stati route/bootstrap rilevanti dopo reload.
- Aggiornamento delle istruzioni operative Entra, della documentazione architetturale e del piano FEAT-001 per distinguere token/account MSAL dai dati personali applicativi.
- Change fragment bilingue e rigenerazione/validazione degli artefatti di documentazione o release interessati.

## Fuori scope

- Persistenza MSAL in `localStorage`, IndexedDB, Cache API, cookie applicativi o service worker.
- Persistenza di `familyId`, membership, dati familiari, risposte API, audio o operazioni remote.
- Login silenzioso dopo chiusura della sessione di navigazione, estensione della durata Entra, token refresh personalizzato o modifica delle policy Entra.
- Modifiche a backend, database, API, Bicep, workflow, CSP, route o policy di autorizzazione.

## Sicurezza e privacy

- `sessionStorage` e limitata all'origine e alla sessione della scheda: la CR non abilita una sessione condivisa tra schede ne una persistenza durevole richiesta al browser.
- La protezione XSS resta critica per ogni token browser-side: CSP esistente, assenza di HTML non sanitizzato e divieto di loggare token restano obbligatori.
- Il bootstrap post-refresh resta obbligatorio: il token non diventa prova di membership e non autorizza dati senza verifica server-side.
- La chiusura effettiva di una sessione browser e comportamento del browser, non un nuovo contratto applicativo; la PWA non deve copiare token o account in altri storage per forzarlo.

## Rischi

- Un cambio involontario a `localStorage` estenderebbe la durata e il perimetro della persistenza oltre quanto approvato.
- Un test che simuli soltanto lo stato React potrebbe non rilevare una regressione nella configurazione MSAL reale.
- Il ripristino di una scheda da parte del browser puo mantenere la sessione secondo le politiche del browser; la CR non deve aggiungere meccanismi applicativi per superare tali politiche.

## Rollback

- Il rollback applicativo ripristina l'asset frontend N-1, tornando al comportamento precedente `memoryStorage` e al login dopo refresh.
- Non ci sono migration, dati applicativi o contratti API da ripristinare.

## Criteri di accettazione

- La configurazione MSAL usa esclusivamente `sessionStorage`, mai `memoryStorage` o `localStorage`, per account e token della SPA.
- Con account MSAL e sessione Entra validi, il refresh della route `/kinlist` non mostra il login e riesegue il bootstrap autorevole prima di rendere il contesto famiglia.
- Dopo un refresh non viene riutilizzato alcun `familyId` o dato familiare precedente: il contesto React parte vuoto e viene valorizzato solo dal bootstrap riuscito.
- Token non acquisibile, sessione Entra non valida, `401`, `403`, cambio account, logout e offline mantengono gli stati correnti fail-closed e non espongono dati familiari.
- Nessun token/account MSAL e copiato in `localStorage`, Cache API, IndexedDB, service worker, URL, log, metriche o trace.
- Documentazione e release note distinguono esplicitamente la sessione MSAL in `sessionStorage` dalla persistenza vietata di dati personali applicativi.

## Tracciabilita

- Requisiti originari: `docs/brainstorming/functional-analysis.md`, FLOW-001, FR-001, FR-003, FR-027-FR-030 e FR-032.
- Decisione modificata: `docs/backlog/features/accesso-instradamento/feature.plan.md`, riga 19.
- Architettura modificata: `docs/brainstorming/architecture.md`, sezioni 1, 6.1, 8 e ADR-009, limitatamente alla cache MSAL.
- Vincoli applicati: `AGENTS.md`, regole frontend, PWA, sicurezza, i18n, documentazione e Definition of Done.
- Stato reale: `src/frontend/src/lib/auth.ts`, `src/frontend/src/components/ProtectedRoute.tsx`, `src/frontend/src/components/KinHubFamilyContext.tsx` e `src/frontend/src/components/KinListAccessGate.tsx`.
