---
status: Completed
---

# FEAT-001 - Entrare nel percorso corretto dopo il login

- **Codice**: `accesso-instradamento`
- **Tipo**: `enabler`
- **Readiness**: `ready`
- **Wave**: 1
- **Risultato**: dopo il login, l'utente vede KinList se ha una membership attiva oppure l'onboarding obbligatorio, senza esposizione di dati familiari.

## Contesto autonomo

KinHub dispone già di MSAL nella SPA e della policy scope `ApiAccess`, ma non ha profili applicativi, membership o policy `Family`. Questa slice crea il collegamento idempotente `(iss, oid)` -> profilo interno, restituisce lo stato familiare autorevole e instrada la PWA. Stabilisce anche i confini condivisi di autorizzazione, telemetria e comportamento offline usati dalle feature successive.

## Scope

### Incluso

- Profilo applicativo unico per `(iss, oid)`, con claim obbligatori e nessun fallback identificativo.
- Stato post-login con membership attiva oppure onboarding; una membership inattiva non concede accesso.
- Policy `ApiAccess` per bootstrap/onboarding e policy esattamente `Family` per API di famiglia, con handler scoped asincrono e `familyId` in query.
- Scope famiglia ripetuto nei casi d'uso/repository e distinzione tra sessione invalida, `403`, errore tecnico e assenza di famiglia.
- Shell PWA pubblica offline senza dati personali, operazioni remote disabilitate e messaggio localizzato.
- Baseline di metriche/tracce KinList redatte e integrazione con correlation ID/Problem Details esistenti.
- Allineamento minimo di PostgreSQL e identità gestita necessario alla persistenza sicura, con migration e piano di rollback.

### Escluso

- Creazione o join della famiglia, realizzati da FEAT-002 e FEAT-005.
- Dati item, inviti, ruoli/gruppi, cache membership o dati personali offline.
- Nuove risorse Azure o fallback a password per PostgreSQL negli ambienti approvati.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-001 | Bootstrap, verifica membership e instradamento sicuro |
| Requisiti | FR-001-FR-003, FR-027-FR-030, FR-032 | Identità, accesso, PWA, privacy e osservabilità di base |
| Regole/decisioni | BR-001, BR-002, BR-019, BR-021, BR-024, BR-036; DEC-002, DEC-010, DEC-011, DEC-015, DEC-016, DEC-027, DEC-031 | Regole fail-closed, offline e famiglia unica |
| Architettura | ADR-001-ADR-004, ADR-009-ADR-011; sezioni 6.1, 6.2, 8, 9 | Layer reali, managed identity, policy e redazione |

## Dipendenze

### Feature prerequisite

Nessuna.

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| TECH-001 | closed | Claim canonici e configurazione token confermati senza cambiare il contratto approvato | `docs/operations/entra-external-id.md`, test claim mancanti e configurazione audience/scope |
| TECH-002 | closed | Percorso infrastrutturale identity-based e principal PostgreSQL definiti e automatizzati | `docs/operations/database-migrations.md`, `docs/operations/azure-deployment.md`, workflow OIDC con principal/grant e rollback documentato |

### Parallelismo consentito

Nessuno: questa feature stabilisce i contratti iniziali di identità, membership, policy, DI e schema shared.

## Contratto di consegna

### Comportamento

- Un token valido con `iss` e `oid` crea o riusa un solo profilo; richieste concorrenti non duplicano il profilo.
- La risposta di bootstrap non include dati familiari se manca una membership attiva e la PWA mostra solo `Crea una famiglia` e `Unisciti con un codice`.
- Una richiesta `Family` usa il `familyId` della query, verifica il database e restituisce `403` senza dettagli se la membership non è attiva.
- Offline la shell si avvia, ma non mostra dati personali, non esegue richieste remote e non accoda operazioni.
- Loading, sessione scaduta, errore recuperabile, accesso negato e onboarding sono stati distinti e localizzati.

### Touchpoint previsti

- **Dominio/business**: `src/backend/domains/DA.KinHub.Domain`, `src/backend/business/DA.KinHub.Business` per identità applicativa, membership e risoluzione del contesto.
- **Persistenza/migration**: `src/backend/infrastructure/DA.KinHub.Infrastructure/Persistence`, schema shared, vincoli univoci e migration con verifica/rollback in `docs/operations/database-migrations.md`.
- **API/integrazioni**: `src/backend/applications/DA.KinHub.Functions/Program.cs`, configurazione auth, endpoint bootstrap e Problem Details.
- **Frontend/UX**: `src/frontend/src/App.tsx`, `components/ProtectedRoute.tsx`, `lib/auth.ts`, `lib/api.ts`, route e risorse `it`/`en`.
- **Infrastruttura/configurazione**: `infra/modules/postgres.bicep`, composition Bicep/workflow per Entra auth e provisioning identity-based già approvato.
- **Documentazione/operazioni**: guida onboarding bilingue, help route, runbook autenticazione/DB e change fragment.

### Errori, sicurezza e osservabilità

- Claim mancanti, token non valido e repository non disponibile falliscono chiusi senza fallback su nome/email.
- `401`, `403`, onboarding e guasto tecnico restano distinguibili; nessun nome famiglia o claim completo nei log.
- Tracce collegano bootstrap, policy e repository; metriche aggregano esiti/durate e `403` separati dai guasti.

## Criteri di accettazione

### AC-001 - Profilo idempotente

- **Dato** un token validato con `iss` e `oid`
- **Quando** lo stesso utente accede una o più volte, anche con richieste concorrenti
- **Allora** esiste un solo profilo interno collegato alla coppia e viene riusato
- **Fonte**: FR-001, FR-002, BR-001

### AC-002 - Claim mancanti fail-closed

- **Dato** un token senza `iss` o `oid`
- **Quando** viene richiesto il bootstrap
- **Allora** l'accesso fallisce senza usare nome o email e senza creare profili
- **Fonte**: FR-001, DEC-031, NFR-005

### AC-003 - Instradamento autorevole

- **Dato** un profilo con membership attiva, inattiva oppure assente
- **Quando** termina la verifica post-login
- **Allora** solo la membership attiva apre KinList; gli altri casi mostrano onboarding senza dati familiari residui
- **Fonte**: FR-003, FR-032, BR-002, DEC-016

### AC-004 - Policy Family riservata

- **Dato** un utente autenticato senza membership attiva per il `familyId` richiesto
- **Quando** invoca un'API protetta da `Family`
- **Allora** riceve `403` Problem Details senza dettagli della famiglia e nessun dato viene letto o modificato fuori scope
- **Fonte**: FR-003, BR-036, DEC-027, ADR-011

### AC-005 - Shell offline senza dati personali

- **Dato** un browser supportato oppure una PWA installata, online o senza rete
- **Quando** l'utente installa, apre o naviga KinHub
- **Allora** manifest e shell restano utilizzabili; offline vede solo la shell pubblica e un feedback breve, mentre dati e API autenticate non sono in cache né accodati
- **Fonte**: FR-027, FR-029, BR-019, ADR-009

### AC-006 - Esperienza e telemetria conformi

- **Dato** ciascuno stato di bootstrap nei temi e nelle lingue supportate
- **Quando** viene usato da tastiera, touch o tecnologia assistiva
- **Allora** focus, contrasto, testo e stato sono comprensibili e la telemetria registra solo esito/durata/categoria redatti
- **Fonte**: FR-028, FR-030, NFR-004, NFR-007-NFR-010

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Risoluzione claim, stati membership, mapping errori | Test dominio/business fail-closed e idempotenza |
| Integrazione | Vincoli `(iss, oid)`, membership attiva, policy/DI, managed identity | Test PostgreSQL e Function `401`/`403`/errore |
| Frontend/component | Routing, loading/error/onboarding/offline e accessibilità | Test di stato e audit accessibilità |
| End-to-end/manuale | Login associato/non associato, refresh e offline PWA | Evidenza Chrome/Edge desktop e Android |
| Validator repository | Build/test backend; lint, typecheck, build, i18n/routes/docs/release; Bicep/package | Tutti i comandi applicabili completati |

## Definition of Done

- Tutti i criteri di accettazione sono verificati e TECH-001/TECH-002 hanno evidenza.
- Contratti identità, policy `Family`, errori e telemetria sono documentati per le feature dipendenti.
- Migration include verifica e rollback; configurazione identity-based non introduce secret o fallback non approvati.
- Traduzioni, help/guida, route registry se applicabile, accessibilità, PWA e change fragment sono aggiornati.
- Sono eseguiti i comandi di qualità applicabili definiti in `AGENTS.md`, incluso publish/package se cambia il backend.
- Non sono introdotti ruoli, cache membership, dati personali offline o elementi out of scope.
