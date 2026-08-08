---
status: In review
---

# BUG-FEAT-016-002 - Correggere il falso fallimento dello smoke test release

- **Feature interessata**: FEAT-016 `riallineamento-infrastruttura-dev`
- **Tipo**: correzione delivery, smoke test GitHub Actions
- **Readiness**: `ready`
- **Stato**: `In review`
- **Breaking change prodotto**: no
- **Risultato**: la pipeline di release riconosce correttamente un sito funzionante e fallisce soltanto quando una verifica realmente necessaria non è soddisfatta.

## Diagnosi e correzione

Il run `31256624552` ha completato migration, One Deploy e pubblicazione Static Web App. Root e `/api/version` hanno risposto `200`, mentre l'hostname diretto della Function ha restituito `401 Unauthorized` con `WWW-Authenticate: Bearer`: l'autenticazione front-door della Function App precede il worker e rende insufficiente una richiesta anonima, anche se `/health/live` è anonimo nel contratto applicativo.

`release.yml` ora acquisisce un token temporaneo per l'audience API dal login OIDC e lo usa solo per la health Function. Il nuovo helper `scripts/smoke-test-release.mjs` verifica i tre endpoint separatamente, segue redirect, ritenta solo condizioni transitorie e registra esclusivamente esito, status e tentativi.

## Segnalazione

La pipeline `release.yml` fallisce nella fase `Smoke test release`, anche quando il sito è raggiungibile e funziona correttamente. Il controllo attuale usa richieste `curl --fail` verso health Function, root della Static Web App e `/api/version`; almeno una di queste verifiche può restituire un esito non accettato dal comando pur non rappresentando un guasto del sito.

La causa concreta deve essere individuata nei log e nelle risposte HTTP del run, senza assumere che il problema sia nel deployment applicativo. La correzione deve distinguere un falso negativo dello smoke test da un reale malfunzionamento della release.

## Scope

### Incluso

- Riprodurre e diagnosticare il fallimento dello smoke test sui tre endpoint attualmente verificati.
- Correggere la verifica affinché usi endpoint, hostname, status code, redirect, timing e retry coerenti con il contratto live della release.
- Mantenere verifiche separate e leggibili per Function health, root Static Web App e `/api/version`, oppure documentare una motivazione tecnica per una composizione diversa.
- Conservare il controllo di disponibilità reale del sito e rendere il fallimento diagnostico senza esporre secret o PII.
- Aggiungere validazione o test del workflow per successo, risposta non valida, timeout e indisponibilità reale.

### Escluso

- Modifiche a Bicep, risorse Azure, dominio applicativo, frontend di prodotto o contratto degli endpoint.
- Rimozione dello smoke test, aumento indefinito dei retry o ignorare errori HTTP reali.
- Bypass di health, `/api/version`, deployment incrementale, migration, One Deploy o gate live `GATE-003`.
- Introduzione di nuovi workflow o secret.

## Tracciabilità

| Tipo | Riferimenti | Contributo del bug |
|---|---|---|
| Flussi | Nessuno | Corregge esclusivamente la verifica operativa post-release |
| Requisiti | NFR-004-NFR-007, NFR-011-NFR-014 | Mantiene disponibilità, osservabilità e verifica del delivery |
| Regole/decisioni | FEAT-016 AC-094 e AC-096; `feature.plan.md` sezioni 7 e 11; `AGENTS.md` CI/CD | Rende affidabile lo smoke test senza trasformarlo in un bypass |
| Architettura | ADR-018, ADR-019, ADR-020 | Preserva separazione tra deployment e verifica live di Function/Static Web App |

## Dipendenze

### Feature prerequisite

Nessuna. Il bug riguarda il workflow già esistente e può essere sviluppato su un ramo dedicato.

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-003 | open | La riproduzione end-to-end dipende dall'accesso alla subscription Azure target e dai run della release | Run release sullo stesso SHA, log/risposte non sensibili e smoke test verde con sito funzionante |

### Parallelismo consentito

Nessuno con modifiche concorrenti a `.github/workflows/release.yml`, agli endpoint health/version o alla documentazione del contratto smoke. Prima del lavoro parallelo su delivery deve essere congelato il contratto di esito atteso per ciascun endpoint.

## Contratto di consegna

### Comportamento

- Dopo un deployment riuscito, un sito funzionante supera lo smoke test in modo deterministico entro i retry approvati.
- Un endpoint realmente indisponibile, un timeout o una risposta HTTP non accettata fanno fallire lo smoke test.
- Redirect e risposte attese sono gestiti secondo il contratto effettivo degli endpoint, non tramite assunzioni implicite del runner.
- Il log identifica quale verifica è fallita, con status e diagnostica tecnica non sensibile, senza token, secret, hostname scoperti o payload personali.
- La verifica continua a coprire health Function, root Static Web App e `/api/version` attraverso i nomi prodotti dal deployment ARM.

### Touchpoint previsti

- **Dominio/business**: Non pertinente.
- **Persistenza/migration**: Non pertinente; verificare soltanto che il test resti dopo migration e deployment Function.
- **API/integrazioni**: Contratti live di `/health/live`, `/` e `/api/version`; nessuna modifica funzionale agli endpoint salvo evidenza indispensabile.
- **Frontend/UX**: Nessuna modifica di prodotto; la root pubblicata deve restare verificabile.
- **Infrastruttura/configurazione**: `.github/workflows/release.yml` e validatori workflow in `tools/skill-harness/` se necessari.
- **Documentazione/operazioni**: `docs/operations/azure-deployment.md` e riferimenti allo smoke test se il contratto o la diagnostica cambiano.

### Errori, sicurezza e osservabilità

- Non mascherare errori reali con `|| true`, exit code ignorati o retry senza limite.
- Non stampare token, header di autenticazione, secret, payload o dati personali.
- Mantenere action esterne fissate a SHA completo, permessi minimi e correlazione tramite SHA/run ID.
- Distinguere nei log errore di rete, timeout, status HTTP inatteso e sito non raggiungibile.

## Criteri di accettazione

### AC-016-006 - Sito funzionante riconosciuto come successo

- **Dato** una release completata e un sito che risponde correttamente ai contratti live
- **Quando** `release.yml` esegue lo smoke test
- **Allora** health Function, root Static Web App e `/api/version` superano la verifica senza falso fallimento
- **Fonte**: segnalazione bug; FEAT-016 AC-096; `feature.plan.md` sezione 7

### AC-016-007 - Guasto reale rilevato

- **Dato** un endpoint non raggiungibile, in timeout o con status non accettato
- **Quando** viene eseguito lo smoke test
- **Allora** il job fallisce, indica la verifica interessata e non prosegue come se il deployment fosse valido
- **Fonte**: NFR-004-NFR-007; FEAT-016 AC-096

### AC-016-008 - Retry e redirect coerenti

- **Dato** un endpoint che richiede retry transitorio o restituisce un redirect previsto dal contratto
- **Quando** il runner esegue la verifica
- **Allora** applica solo il comportamento esplicitamente approvato e non interpreta un falso negativo del client HTTP come indisponibilità del sito
- **Fonte**: segnalazione bug; `feature.plan.md` sezioni 7 e 11

### AC-016-009 - Diagnostica sicura

- **Dato** un fallimento dello smoke test
- **Quando** si consultano i log della release
- **Allora** sono disponibili endpoint logico, causa tecnica, status e tentativi senza token, secret, PII o payload sensibili
- **Fonte**: `AGENTS.md` regole sicurezza, osservabilità e CI/CD

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Non pertinente salvo helper puro di classificazione HTTP/retry | Test del helper se introdotto, altrimenti N/A motivato |
| Integrazione | Validazione del workflow con risposte success, redirect, timeout e failure | Test/script repository o fixture documentate |
| Frontend/component | Non pertinente; verificare solo asset root già pubblicati | N/A motivato |
| End-to-end/manuale | Run release con sito funzionante e simulazione/fixture di endpoint non valido | Run GitHub Actions, smoke verde nel caso corretto e failure diagnostico nel caso reale |
| Validator repository | `npm run skills:validate`, actionlint e validatori applicabili | Esiti verdi senza nuovi workflow o secret |

## Definition of Done

- Tutti i criteri di accettazione sono verificati oppure la sola verifica Azure resta esplicitamente sotto `GATE-003`.
- Un sito funzionante non produce più il falso fallimento osservato e un guasto reale continua a bloccare la release.
- Workflow, diagnostica, test/validatori e documentazione operativa applicabili sono coerenti.
- Non sono introdotti bypass, secret, nuovi endpoint o elementi out of scope.
- Il bug resta in stato `Open` finché la responsabile umana non autorizza la transizione successiva.
