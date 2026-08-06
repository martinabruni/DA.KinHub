---
status: In review
---

# FEAT-016 - Riallineare infrastruttura e delivery dell'ambiente dev

- **Codice**: `riallineamento-infrastruttura-dev`
- **Tipo**: `operational`
- **Readiness**: `blocked`
- **Wave**: 5
- **Risultato**: il repository adotta un provisioning dev esplicito, incrementale e verificabile con pipeline CI, infrastructure e release coerenti, senza discovery euristica o deploy distruttivi.

## Contesto autonomo

Il repository aveva un entry point Bicep generativo, workflow di deploy separati e riferimenti distribuiti che non riflettevano piu i nomi Azure approvati per `dev`, la strategia build-once deploy-many e i controlli di sicurezza richiesti per un repository pubblico. Il refactor approvato consolida i moduli Bicep, sostituisce le pipeline GitHub Actions con tre workflow stabili, rende espliciti i nomi delle risorse esistenti e aggiorna harness e documentazione. La modifica e gia stata implementata nel repository, ma la validazione live su Azure resta bloccata finche la subscription approvata non e accessibile dalla pipeline OIDC e dalla sessione autorizzata per inventory e what-if.

## Scope

### Incluso

- Sostituzione di `infra/app.bicep` con `infra/main.bicep` e di `infra/main.dev.bicepparam` con `infra/environments/dev.bicepparam`.
- Consolidamento dei moduli Bicep in `monitoring.bicep`, `data-security.bicep`, `functions.bicep` e `static-web-app.bicep`, con nomi dev espliciti e senza `uniqueString` o `namingPrefix`.
- Adozione sicura delle risorse esistenti, what-if bloccante su delete/replacement e deployment ARM stabile `kinhub-dev-infrastructure` in modalita `incremental`.
- Sostituzione dei workflow con `ci.yml`, `infrastructure.yml` e `release.yml`, action esterne fissate a SHA completi, `CODEOWNERS`, OIDC, concurrency non cancellabile e artifact retention coerente.
- Packaging backend riusabile senza seconda compilazione, migration bundle e release build-once deploy-many.
- Skill `infrastructure`, aggiornamento della skill `implementation`, validazioni harness estese e rigenerazione del registry.
- Aggiornamento di prompt, README, documentazione operativa e riferimenti repository-wide ai nuovi file autorevoli.
- Conservazione del piano operativo in `feature.plan.md` e delle linee guida infrastrutturali nella cartella della feature come fonte backlog dedicata.

### Escluso

- Nuove feature di prodotto, nuovi KinService o modifiche allo scope funzionale approvato di KinHub/KinList.
- Introduzione di nuovi ambienti oltre `dev`, nuove risorse Azure non richieste dal piano o bootstrap automatico ripetuto a ogni deploy.
- Merge della PR, completamento automatico della feature o bypass delle verifiche live mancanti.

## Tracciabilita

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | Nessuno | Refactor operativo senza nuovi flussi utente |
| Requisiti | FR-027-FR-030, FR-056-FR-064; NFR-004-NFR-007, NFR-011-NFR-014 | Preserva deploy, accesso, osservabilita, PWA e disponibilita del sistema condiviso |
| Regole/decisioni | `docs/backlog/features/riallineamento-infrastruttura-dev/feature.plan.md` sezioni 1-11; `docs/backlog/features/riallineamento-infrastruttura-dev/infra-guidelines.md` sezioni 1-15; `AGENTS.md` sezioni Azure Functions, CI/CD, skill harness e Definition of Done | Traduce il refactor approvato in contratti eseguibili di Bicep, workflow, packaging e documentazione |
| Architettura | `docs/brainstorming/architecture.md` sezioni 11-12; ADR-001, ADR-004, ADR-018, ADR-019, ADR-020 | Allinea hosting condiviso, managed identity, catalogo servizi e responsabilita di deploy |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-001 - Entrare nel percorso corretto dopo il login | hard | Il refactor deve preservare i contratti condivisi di auth, policy, Problem Details e Function App esistenti | Baseline backend/shared valida e documentata | Inizio dopo l'integrazione dei contratti condivisi |
| FEAT-015 - Raggiungere i servizi attivi della famiglia | hard | I workflow di release e i controlli smoke devono preservare Home, `/api` e accesso diretto ai KinService attivi | Contratti di route, bootstrap e disponibilita familiare gia congelati | Inizio dopo la stabilizzazione dei touchpoint applicativi esposti |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-003 | open | Senza accesso alla subscription `a148a62f-0509-4dd5-a61f-0043b182d5f1` non si possono completare inventory live, validate, what-if e smoke test Azure | Sessione o pipeline OIDC con accesso alla subscription, artifact what-if del workflow `infrastructure.yml`, verifica live di runtime, `health/live`, `/api/version` e telemetria |

### Parallelismo consentito

Nessuno. La feature modifica file autorevoli condivisi in `infra/`, `.github/workflows/`, `tools/skill-harness/`, script di packaging e documentazione operativa; non e sicuro procedere in parallelo con altre feature che toccano gli stessi contratti.

## Contratto di consegna

### Comportamento

- L'infrastruttura dev usa nomi espliciti delle risorse adottate, moduli consolidati e deployment `incremental` a scope resource group, senza creazione o modifica del resource group.
- `ci.yml` esegue quality gate completi sulle pull request senza secret Azure e senza produrre artifact autorizzati al deployment.
- `infrastructure.yml` esegue validate e what-if immediatamente prima del deploy, conserva l'output non sensibile, blocca cambi distruttivi e applica un deployment ARM con nome stabile.
- `release.yml` ricompila il commit trusted una sola volta, usa gli output ARM invece della discovery euristica, applica migration prima di One Deploy e pubblica il frontend gia compilato.
- Skill, registry e documentazione descrivono solo i workflow ammessi, i vincoli SHA pinning, la modalita incremental, il linked backend `/api` e l'adozione sicura delle risorse esistenti.

### Touchpoint previsti

- **Dominio/business**: Non pertinente salvo preservare i contratti applicativi gia esposti dal backend condiviso.
- **Persistenza/migration**: `src/backend/infrastructure/DA.KinHub.Infrastructure/Persistence`, migration bundle, `docs/operations/database-migrations.md` e grant PostgreSQL identity-based usati dalla release.
- **API/integrazioni**: `src/backend/applications/DA.KinHub.Functions/OpenApi/OpenApiDocumentProvider.cs`, `openapi.yaml`, smoke test `health/live` e `/api/version`, collegamento Static Web Apps `/api`.
- **Frontend/UX**: `src/frontend/public/staticwebapp.config.json`, shell pubblicata da `release.yml`, nessuna nuova UX di prodotto.
- **Infrastruttura/configurazione**: `infra/main.bicep`, `infra/environments/dev.bicepparam`, `infra/modules/*`, `.github/workflows/ci.yml`, `.github/workflows/infrastructure.yml`, `.github/workflows/release.yml`, `.github/CODEOWNERS`, `scripts/package-backend.*`, `tools/skill-harness/index.mjs`.
- **Documentazione/operazioni**: `AGENTS.md`, `README.md`, `infra/README.md`, `docs/bootstrap.prompt.md`, `.azure/deployment-plan.md`, `docs/operations/azure-deployment.md`, `tools/skill-harness/README.md`, change fragment bilingue e registry skill generato.

### Errori, sicurezza e osservabilita

- Le pull request non devono usare `pull_request_target`, secret Azure o action non fissate a SHA completo.
- Il provisioning blocca `Delete`, replacement e cambi distruttivi su PostgreSQL o rete e non espone valori sensibili negli output Bicep.
- Release e workflow loggano solo metadati tecnici a bassa cardinalita; non registrano token, secret, PII, codici o hostname scoperti euristicamente.
- Le verifiche finali devono distinguere esplicitamente cio che e stato validato localmente da cio che resta bloccato in assenza di accesso Azure.

## Criteri di accettazione

### AC-091 - Struttura Bicep esplicita e incrementale

- **Dato** il repository con il nuovo refactor infrastrutturale
- **Quando** si leggono entry point, parameter file e moduli Bicep di `dev`
- **Allora** esistono `infra/main.bicep`, `infra/environments/dev.bicepparam` e i moduli consolidati approvati, con nomi risorsa espliciti, deployment `incremental` e nessun uso di `uniqueString` o `namingPrefix`
- **Fonte**: piano sezioni 1-3, linee guida sezioni 5-6

### AC-092 - Workflow ammessi e protetti

- **Dato** `.github/workflows/`
- **Quando** si verificano i file autorevoli di delivery
- **Allora** esistono solo `ci.yml`, `infrastructure.yml` e `release.yml`, le action esterne sono fissate a SHA completi, `.github/CODEOWNERS` protegge workflow e `infra/**`, e la CI delle pull request non usa secret Azure
- **Fonte**: piano sezioni 4-8, linee guida sezione 8

### AC-093 - Provisioning sicuro delle risorse esistenti

- **Dato** l'ambiente `dev` adottato dal nuovo IaC
- **Quando** il workflow infrastrutturale esegue validate e what-if
- **Allora** conserva il what-if come artifact, usa concurrency dedicata non cancellabile, blocca delete/replacement e usa il deployment stabile `kinhub-dev-infrastructure` per esporre gli output a `release.yml`
- **Fonte**: piano sezioni 2, 6 e 11; linee guida sezioni 9 e 11

### AC-094 - Release build-once deploy-many

- **Dato** un commit trusted su `main`
- **Quando** `release.yml` costruisce e distribuisce gli artifact
- **Allora** ZIP Function, migration bundle, frontend `dist` e metadata vengono prodotti una sola volta, riutilizzati per il deploy, verificati con checksum e pubblicati senza discovery euristica o ricompilazione aggiuntiva
- **Fonte**: piano sezione 7; linee guida sezioni 10 e 12

### AC-095 - Harness e documentazione coerenti

- **Dato** la nuova skill `infrastructure`, il harness skill e la documentazione operativa
- **Quando** si verificano reference, README e validatori repository
- **Allora** descrivono i soli workflow ammessi, la validazione su SHA pinning, `pull_request_target`, what-if, incremental, concurrency, SKU Static Web Apps Standard e i nuovi path Bicep/workflow, con registry rigenerato
- **Fonte**: piano sezioni 9-10; linee guida sezioni 4, 8 e 14

### AC-096 - Gate live esplicito prima del completamento

- **Dato** la feature implementata e portata in review
- **Quando** si valuta la chiusura della feature
- **Allora** inventory live, validate Azure, what-if, smoke test `health/live` e `/api/version` via Static Web Apps e verifica telemetria devono risultare eseguiti con credenziali corrette; in assenza di accesso alla subscription la feature resta `blocked` e non puo essere marcata `Completed`
- **Fonte**: piano sezione 11; linee guida sezioni 11 e 14; regole backlog sugli stati

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Validatori harness su workflow ammessi, SHA pinning e divieti `uniqueString`/`pull_request_target` | `npm run skills:validate` |
| Integrazione | Build Bicep, build/test backend, migration bundle e packaging | `az bicep build/build-params`, `dotnet build/test`, bundle EF, ZIP Function |
| Frontend/component | Build shell e static assets gia integrati nel workflow release | `npm run --prefix src/frontend build` oppure N/A motivato per UI invariata |
| End-to-end/manuale | Validate/what-if/deploy live, smoke test Function e Static Web Apps, verifica telemetria | Artifact GitHub Actions e controlli live Azure |
| Validator repository | `skills`, `docs`, `release`, actionlint in CI, Bicep lint/format/build e workflow checks | Esiti registrati senza dichiarazioni non verificate |

## Definition of Done

- Tutti i criteri di accettazione sono verificati o, se bloccati da GATE-003, la feature resta esplicitamente `blocked` e `In review`.
- I workflow dichiarati sono gli unici presenti sotto `.github/workflows/` e i consumer dei file rinominati sono aggiornati repository-wide.
- Bicep, packaging, harness, registry, documentazione operativa e change fragment bilingue sono aggiornati e coerenti con i file autorevoli del refactor.
- Build, test, bundle migration, package backend, validatori repository e verifiche Bicep applicabili sono eseguiti e riportati.
- Non sono introdotti nuovi elementi out of scope, secret, risorse Azure non approvate o dipendenze architetturali vietate.
- La feature non puo essere marcata `Completed` senza una verifica live su Azure della subscription target e senza comando esplicito della responsabile umana.
