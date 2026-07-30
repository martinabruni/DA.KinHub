---
status: In review
---

# FEAT-015 - Raggiungere i servizi attivi della famiglia

- **Codice**: `catalogo-servizi-familiari`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 4
- **Risultato**: la Home mostra KinList come servizio attivo della famiglia e nessuno può raggiungerlo senza sessione, famiglia e disponibilità autorizzata.

## Contesto autonomo

La Home attuale mostra card hardcoded per KinList e Release Notes. Il modello condiviso possiede famiglia e membership, ma non conosce i servizi disponibili per una famiglia. Questa feature introduce il primo catalogo KinHub completo: dati localizzati, disponibilità familiare, backfill delle famiglie esistenti, Home dinamica e controllo autorevole dell'accesso diretto a KinList. L'amministrazione futura è preparata dai dati, non dalla UI o da nuovi permessi.

## Scope

### Incluso

- Catalogo persistito nello schema shared con KinList, route e localizzazioni `it`/`en`.
- Disponibilità unica per coppia famiglia-servizio, con KinList attivo per tutte le famiglie esistenti e per ogni nuova famiglia nello stesso commit della creazione.
- Lettura autorizzata dei servizi attivi e contratto tipizzato per la Home.
- Home `/` con login per visitatori, onboarding per utenti autenticati senza famiglia, servizi dinamici per membri e stati loading, vuoto, errore, offline e accesso negato distinti.
- Rimozione dalla Home delle card hardcoded KinList e Release Notes; route, menu Informazioni e notifica Release Notes restano invariati.
- Guard di KinList che applica, anche per URL diretto, autenticazione, famiglia attiva e disponibilità del servizio.
- Migration, rollback, OpenAPI, telemetria redatta, test, help/guide `it`/`en` e change fragment.

### Escluso

- UI/API per creare, modificare o amministrare KinService, localizzazioni o disponibilità familiari.
- Attivazione/disattivazione manuale e ruoli amministrativi.
- Nuovi KinService oltre a KinList.
- Rimozione di Release Notes dalle altre superfici o modifiche al ciclo di notifica aggiornamenti.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-001, FLOW-009, FLOW-015, FLOW-016 | Home dinamica, creazione con disponibilità e accesso diretto sicuro |
| Requisiti | FR-056-FR-064; NFR-005, NFR-008, NFR-009, NFR-011 | Catalogo, disponibilità, localizzazione, UX e autorizzazione |
| Regole/decisioni | BR-002, BR-019, BR-021, BR-042-BR-048; DEC-036-DEC-040 | Perimetro famiglia, privacy, fallback e comportamenti Home |
| Architettura | ADR-002, ADR-003, ADR-009, ADR-010, ADR-011, ADR-018-ADR-020; sezioni 6.1 e 6.1-bis | Schema shared, policy Family, API e guard |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-002 - Creare la propria famiglia | hard | La disponibilità viene assegnata alla famiglia esistente e durante la sua creazione atomica | Famiglia, membership e caso d'uso di creazione integrati | Inizio dopo integrazione FEAT-002 |
| FEAT-014 - Usare un design system condiviso in tutta KinHub | hard | La Home deve riusare stati, card e navigazione approvati | Primitive UI, shell e regole di riuso frontend | Inizio dopo integrazione FEAT-014 |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| TECH-009 | open, non bloccante | Fissa dettagli tecnici di schema, API e codici senza cambiare comportamento approvato | Migration/rollback eseguibili, vincoli verificati, OpenAPI-route paritari e test dei contratti |
| ASM-001 | accepted | KinList conserva la route `/kinlist` | Test di route e accesso diretto aggiornati |

### Parallelismo consentito

Con FEAT-003 dopo CP-001 ampliato. Prima di modificare Home, client API, migration shared o gate KinList, congelare catalogo, disponibilità, forma della risposta, `familyId` e responsabilità del guard. Nessun'altra feature con migration shared procede senza coordinamento.

## Contratto di consegna

### Comportamento

- La migration inserisce KinList e le localizzazioni italiana e inglese, quindi assegna una disponibilità attiva a ogni famiglia esistente senza duplicati.
- La creazione di una famiglia assegna i KinService preconfigurati attivi nella stessa transazione di famiglia e membership; retry e concorrenza non lasciano disponibilità parziali.
- La Home non autenticata mostra solo `Accedi`; la Home autenticata senza famiglia mostra onboarding; il membro riceve esclusivamente servizi attivi della sua famiglia nella lingua selezionata, con fallback tecnico inglese.
- Senza servizi attivi la Home mostra uno stato vuoto; un errore recuperabile mostra Riprova; offline non mostra dati familiari conservati.
- KinList non è raggiungibile da card o URL diretto senza sessione, membership attiva e disponibilità attiva; il rifiuto non rivela altri servizi o famiglie.

### Touchpoint previsti

- **Dominio/business**: `src/backend/domains/DA.KinHub.Domain`, `src/backend/business/DA.KinHub.Business` per catalogo shared, disponibilità, localizzazioni e orchestrazione scoped.
- **Persistenza/migration**: `src/backend/infrastructure/DA.KinHub.Infrastructure/Persistence`, `KinHubDbContext`, configurazioni EF, migration e `docs/operations/database-migrations.md` per schema `shared`, vincoli, seed, verifica e rollback.
- **API/integrazioni**: `src/backend/applications/DA.KinHub.Functions/Http/ApiRoutes.cs`, OpenAPI, Functions, policy `Family`, Problem Details e operazioni telemetriche centralizzate.
- **Frontend/UX**: `src/frontend/src/pages/HomePage.tsx`, `src/frontend/src/components/KinListAccessGate.tsx`, `src/frontend/src/lib/api.ts`, `KinPatterns.tsx`, i18n/help `it`/`en`, guide Home/KinList e route registry esistente.
- **Infrastruttura/configurazione**: nessuna nuova risorsa Azure; migration bundle e grant sullo schema shared restano conformi alle procedure esistenti.
- **Documentazione/operazioni**: `docs/user-guide/it`, `docs/user-guide/en`, contenuti help, `docs/operations/database-migrations.md`, change fragment e artefatti generati dalle fonti autorevoli.

### Errori, sicurezza e osservabilità

- Gli endpoint di catalogo e verifica usano `Family`, `familyId` in query, Problem Details con `code` e `traceId`; richieste non autenticate ricevono `401`, membership o disponibilità non valide `403` senza dettagli.
- Identità e famiglia arrivano solo da token verificato e policy; il client non decide disponibilità né invia ID utente autorevole.
- API autenticate sono `no-store` e network-only; localizzazioni e disponibilità non sono conservate nel browser.
- Trace e metriche misurano durata ed esito di caricamento catalogo e controllo accesso con dimensioni a bassa cardinalità, senza token, familyId, testi localizzati o payload.

## Criteri di accettazione

### AC-084 - Catalogo e backfill KinList

- **Dato** il database con famiglie esistenti e una nuova migration
- **Quando** la migration viene applicata
- **Allora** KinList e le localizzazioni `it`/`en` esistono una sola volta e ogni famiglia esistente ha KinList attivo
- **Fonte**: FR-056-FR-059, BR-042, BR-043, DEC-036-DEC-038

### AC-085 - Creazione atomica con disponibilità

- **Dato** un utente autenticato senza famiglia
- **Quando** crea una famiglia o ripete/concorre nella richiesta
- **Allora** famiglia, membership e disponibilità KinList sono coerenti nello stesso esito, senza duplicati o record parziali
- **Fonte**: FR-059, BR-043, NFR-006

### AC-086 - Home per visitatore e onboarding

- **Dato** la Home `/`
- **Quando** la apre un visitatore oppure un utente autenticato senza famiglia
- **Allora** il visitatore vede `Accedi`, l'utente vede onboarding, nessuno dei due vede KinService familiari o un pulsante login duplicato
- **Fonte**: FR-062, FR-063, BR-045, FLOW-015

### AC-087 - Home catalogo localizzato

- **Dato** un membro con KinList attivo
- **Quando** apre o ricarica la Home nella lingua selezionata
- **Allora** vede la card KinList ottenuta dall'API con nome e descrizione localizzati, senza card hardcoded KinList o Release Notes
- **Fonte**: FR-057, FR-060, FR-061, BR-044, BR-048

### AC-088 - Stati Home sicuri e accessibili

- **Dato** caricamento, nessun servizio attivo, errore recuperabile, offline o accesso negato
- **Quando** la Home risolve il catalogo
- **Allora** mostra stati distinti localizzati, accessibili e compatibili con tema; non mostra dati familiari stale o conservati offline
- **Fonte**: FLOW-015, BR-019, BR-044, NFR-008, NFR-009

### AC-089 - Accesso diretto protetto

- **Dato** la route `/kinlist`
- **Quando** viene aperta senza sessione, senza famiglia, con membership inattiva o con KinList non attivo per la famiglia
- **Allora** applica rispettivamente login, onboarding o accesso negato senza rendere contenuti KinList
- **Fonte**: FR-064, BR-046, BR-047, DEC-040

### AC-090 - Contratti e telemetria redatti

- **Dato** lettura catalogo o verifica di accesso al servizio
- **Quando** riesce, è negata o fallisce tecnicamente
- **Allora** OpenAPI, route e Problem Details restano coerenti, risposta e errori sono `no-store`, e telemetria contiene solo esiti e durate a bassa cardinalità
- **Fonte**: NFR-005, NFR-011, ADR-018-ADR-020

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Invarianti catalogo, localizzazione, disponibilità e fallback | Test dominio/business |
| Integrazione | Vincoli, seed, backfill, creazione atomica e autorizzazione | Test PostgreSQL reale e endpoint |
| Frontend/component | Home login/onboarding/catalogo/stati e guard KinList | Test componenti, accessibilità e i18n |
| End-to-end/manuale | Visitatore, membro, famiglia esistente e URL diretto | Nessun bypass; KinList visibile solo alla famiglia autorizzata |
| Validator repository | Backend, frontend, i18n, docs, route, migration e change fragment | Esiti registrati |

## Definition of Done

- Tutti i criteri di accettazione sono verificati e le dipendenze hard sono integrate.
- Il contratto CP-001 è congelato con FEAT-003 prima di modifiche concorrenti a migration shared, client API o gate KinList.
- Migration, verifica e rollback documentati seguono `docs/operations/database-migrations.md`; seed e backfill sono idempotenti.
- Test, OpenAPI, i18n, help/guide, accessibilità, telemetria, change fragment e artefatti generati applicabili sono aggiornati.
- I comandi richiesti da `AGENTS.md`, inclusi validatori frontend e documentazione applicabili, sono eseguiti e riportati.
- Nessuna UI/API amministrativa, ruolo, nuovo KinService, secret, dato familiare offline o dipendenza architetturale vietata è introdotta.
