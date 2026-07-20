# Azure Deployment Plan — KinHub

> **Status:** Validated

Generated: 2026-07-16

---

## 1. Project Overview

**Goal:** creare da repository vuoto il bootstrap completo, compilabile, testabile e distribuibile di KinHub secondo `docs/bootstrap.prompt.md`.

**Path:** New Project.

**Deployment scope:** preparazione di codice, Bicep e workflow; nessun provisioning o deploy live durante il bootstrap.

## 2. Requirements

| Attribute | Value |
|---|---|
| Classification | Development |
| Scale | Small, meno di 1.000 utenti iniziali |
| Budget | Cost-Optimized |
| Subscription | `MPN-BM` (`a148a62f-0509-4dd5-a61f-0043b182d5f1`), confermata dall'utente |
| Resource group | `rg-kinhub-dev`, esistente e vuoto |
| Application location | `italynorth`, confermata dall'utente |
| Static Web App location | `westeurope`, fissa per requisito |
| Compliance | Nessun requisito ulteriore dichiarato; localizzazione dati europea |
| Policy constraints | Nessun policy assignment rilevato a livello subscription il 2026-07-16 |

Vincoli applicativi principali:

- backend .NET 10, Azure Functions 4.x Isolated Worker Linux su Flex Consumption;
- frontend React, TypeScript, Vite, shadcn/ui, PWA e Azure Static Web Apps;
- PostgreSQL con EF Core, autenticazione Microsoft Entra External ID;
- italiano predefinito e inglese supportato;
- infrastruttura Bicep e CI/CD GitHub Actions con OIDC;
- secret esclusivamente tramite configurazione non versionata, GitHub Secrets o Key Vault;
- nessun codice di produzione caricato dinamicamente dalle skill.

## 3. Components

Il repository applicativo era vuoto all'analisi iniziale; i componenti seguenti sono da creare.

| Component | Type | Technology | Path |
|---|---|---|---|
| KinHub Domain | Domain library | .NET 10 | `src/backend/domains/DA.KinHub.Domain` |
| KinHub Infrastructure | Infrastructure library | EF Core + Npgsql | `src/backend/infrastructure/DA.KinHub.Infrastructure` |
| KinHub Business | Application/business library | .NET 10 | `src/backend/business/DA.KinHub.Business` |
| KinHub API | Serverless REST API | Azure Functions Isolated .NET 10 | `src/backend/applications/DA.KinHub.Functions` |
| KinHub Web | SPA/PWA | React + TypeScript + Vite | `src/frontend` |
| Skill harness | Repository tool | Node.js + TypeScript | `tools/skill-harness` |
| Documentation sync | Repository tool | Node.js + TypeScript | `tools/docs-sync` |
| Release notes | Repository tool | Node.js + TypeScript | `tools/release-notes` |
| Azure infrastructure | IaC | Bicep | `infra` |
| CI/CD | Automation | GitHub Actions | `.github/workflows` |

### Dependencies

| Component | Depends on | Contract |
|---|---|---|
| Web | API | HTTPS/JSON + Entra access token |
| API | Business, Infrastructure | Project references + DI |
| Infrastructure | Domain, PostgreSQL | EF Core/Npgsql |
| Business | Domain | Domain interfaces and models |
| Build | Skill/docs/release tools | Deterministic validation/generation |

## 4. Recipe Selection

**Selected:** direct Bicep + Azure CLI/GitHub Actions.

**Rationale:** il prompt prescrive Bicep modulare, workflow GitHub dettagliati, pacchettizzazione separata e One Deploy. Non viene aggiunto `azd` perché introdurrebbe un secondo percorso di configurazione e deployment non richiesto.

## 5. Architecture

**Stack:** Serverless.

| Component | Azure Service | Planned SKU/configuration |
|---|---|---|
| REST API | Azure Functions Flex Consumption | `FC1`, Linux, `dotnet-isolated`, .NET 10, 2.048 MB, max 20 istanze, 0 always-ready |
| SPA/PWA | Azure Static Web Apps | Free per dev, location `westeurope` |
| Relational data | Azure Database for PostgreSQL Flexible Server | Burstable `Standard_B1ms`, 32 GiB, HA disabilitata in dev |
| Host/deployment storage | Azure Storage | Standard LRS, private deployment container, managed identity |
| Secrets | Azure Key Vault | Standard, RBAC authorization, purge protection parametrica |
| Telemetry | Application Insights | Workspace-based |
| Logs | Log Analytics Workspace | Pay-as-you-go, retention economica parametrica |
| Identity | System-assigned managed identity | Least-privilege RBAC per Function App |

Decisioni pragmatiche:

- niente VNet integration, private endpoint o always-ready per default dev;
- concorrenza HTTP lasciata al comportamento della piattaforma;
- migration automatica solo in locale/dev con feature flag; produzione tramite bundle/pipeline;
- One Deploy usa un container Blob privato e managed identity;
- Static Web Apps resta in `westeurope` come richiesto anche se le altre risorse sono in `italynorth`.

## 6. Provisioning Limit Checklist

Verifiche eseguite il 2026-07-16 sulla subscription confermata. `Microsoft.Quota` non è registrato: non è stato modificato lo stato della subscription. È stato usato il fallback previsto (Azure Resource Graph, capability API/CLI e limiti Microsoft documentati). Il resource group target contiene 0 risorse.

| Resource type | New | Current in target region | Total after | Limit/capacity evidence | Result |
|---|---:|---:|---:|---|---|
| `Microsoft.Web/serverfarms` Flex Consumption | 1 | 0 | 1 | `italynorth` restituita da `az functionapp list-flexconsumption-locations`; quota regionale predefinita 250 core | Pass |
| Flex runtime capacity, 2.048 MB × 20 | 20 core max | 0 core rilevati | 20 core max | 250 core predefiniti per subscription/regione; 230 core di margine | Pass |
| `Microsoft.Web/sites` Function App | 1 | 0 | 1 | Una Function App per piano Flex; piano dedicato previsto | Pass |
| `Microsoft.Storage/storageAccounts` | 1 | 0 | 1 | Limite Microsoft documentato 250 account per regione/subscription | Pass |
| `Microsoft.DBforPostgreSQL/flexibleServers` | 1 | 0 | 1 | Capability API conferma `Standard_B1ms`, 1 vCore, zone 1/2/3 e 32 GiB in `italynorth`; nessuna quota numerica esposta | Pass |
| `Microsoft.Insights/components` | 1 | 0 | 1 | Nessun limite di provisioning regionale rilevante esposto; uso dev minimo | Pass |
| `Microsoft.OperationalInsights/workspaces` | 1 | 1 | 2 | Nessun limite vicino; il workspace esistente è fuori dal resource group target | Pass |
| `Microsoft.KeyVault/vaults` | 1 | 0 | 1 | Nessun limite vicino per il singolo vault pianificato | Pass |
| `Microsoft.Web/staticSites` in `westeurope` | 1 | 1 | 2 | Servizio disponibile; singola app dev Free pianificata | Pass |
| `Microsoft.Authorization/roleAssignments` | 4–6 | non materiale | non materiale | Molto sotto il limite per subscription e scope | Pass |

**Status:** All planned resources within documented limits/capabilities.

Limite noto: per ottenere valori live dal provider Microsoft Quota occorre registrare `Microsoft.Quota`; questa operazione non è necessaria per generare o validare staticamente il bootstrap e dovrà essere autorizzata prima di un deploy reale.

## 7. Security and Configuration

- OIDC GitHub-to-Azure come percorso primario; publish profile soltanto fallback documentato.
- Managed identity system-assigned per Storage e Key Vault.
- Deployment Blob privato, shared key disabilitabile e nessun secret in Bicep versionato.
- PostgreSQL password tramite parametro sicuro/Key Vault; nessun valore reale nel repository.
- Entra tenant, client ID, audience e scope come placeholder espliciti.
- HTTPS only, TLS minimo, CORS parametrico, log senza dati sensibili.
- Validazione della configurazione all'avvio con fallimento esplicito per impostazioni critiche.

## 8. Execution Checklist

### Phase 1 — Planning

- [x] Specialized technology check: Azure Functions resta in `azure-prepare`.
- [x] Analizzare workspace: modalità New Project.
- [x] Raccogliere requisiti da `docs/bootstrap.prompt.md`.
- [x] Confermare subscription, resource group e location.
- [x] Verificare policy e inventario risorse.
- [x] Verificare disponibilità Flex Consumption e PostgreSQL.
- [x] Selezionare Bicep come recipe.
- [x] Definire architettura e profilo costi.
- [x] Approvazione esplicita dell'utente (2026-07-16).

### Phase 2 — Execution

- [x] Caricare le composition rules ufficiali per Azure Functions.
- [x] Creare struttura, solution .NET e progetti xUnit.
- [x] Implementare dominio, business, infrastruttura EF Core e Function App.
- [x] Implementare frontend React/PWA, i18n, tema, onboarding e route help.
- [x] Implementare skill harness, docs sync e release notes.
- [x] Creare documentazione bilingue, change fragment e patch notes.
- [x] Creare Bicep modulare, packaging e workflow GitHub Actions.
- [x] Eseguire hardening e verifiche funzionali locali.
- [x] Aggiornare lo stato a `Ready for Validation`.

### Phase 3 — Validation

- [x] Applicare la skill `azure-validate` senza eseguire deploy.
  - [x] Bicep compilation.
  - [x] Resource-group template validation.
  - [x] What-if preview.
  - [x] Azure CLI authentication and approved-subscription check.
  - [x] Bicep linting.
  - [x] Azure Policy assignment review.
  - [x] Static managed-identity/RBAC review.
- [x] Backend restore/build/test/publish.
- [x] Packaging ZIP e verifica struttura/checksum/manifest.
- [x] Frontend install/lint/typecheck/build.
- [x] Validazioni i18n, route docs, skill, fragments e Bicep.
- [x] Avvio Functions locale e smoke test se l'ambiente lo consente.
- [x] Registrare prove e limiti nella sezione seguente.

### Phase 4 — Deployment

- [ ] Fuori scope: nessun deploy live senza richiesta e approvazione separate.

## 9. Validation Proof

Validazione completata il 2026-07-16 senza provisioning o deploy.

| Area | Command/evidence | Result |
|---|---|---|
| Azure account | `az account show` | Pass: `MPN-BM`, subscription approvata, stato `Enabled` |
| Bicep compile/lint | `az bicep lint`, `az bicep build`, `az bicep build-params` | Pass, nessun errore; disponibile solo un aggiornamento opzionale del CLI Bicep |
| ARM validation | `az deployment group validate` su `rg-kinhub-dev` | Pass: `Succeeded` |
| ARM preview | `az deployment group what-if --result-format ResourceIdOnly` | Pass: 17 cambiamenti previsti, nessun deployment eseguito |
| Azure Policy | `az policy assignment list` a scope subscription/inherited | Pass: 0 assignment visibili |
| Backend | `dotnet restore`, `dotnet build -c Release`, `dotnet test` | Pass: build 0 warning/0 error; 7 test superati |
| Dependency audit | `dotnet list package --vulnerable --include-transitive` | Pass: nessun pacchetto vulnerabile noto |
| Publish/package | `scripts/package-backend.ps1 -Environment Development` | Pass: ZIP, manifest e checksum generati; 129 entry, `host.json` e assembly alla radice, 0 file locali proibiti |
| Bash packaging | Git for Windows `bash -n scripts/package-backend.sh` | Pass: sintassi valida; esecuzione completa non necessaria perché il pacchetto equivalente PowerShell è stato prodotto |
| Frontend | `npm run typecheck`, `npm run lint`, `npm run build` | Pass: PWA generata, chunk sotto 500 KiB |
| npm audit | `npm audit --audit-level=high` | Pass: 0 vulnerabilità |
| Repository checks | docs, i18n, route, skill e change-fragment validation | Pass: 6 guide × 2 lingue, 4 namespace, 8 route, 5 skill, 1 fragment |
| Local smoke | Functions Core Tools su porta 7072 | Pass: live, version, status e OpenAPI 3.0.3 con bearer/OAuth2; readiness non completata perché PostgreSQL locale non era in esecuzione |

### Role Assignment Verification

- **Status:** Verified tramite revisione statica dei moduli Bicep.
- **Identity:** managed identity system-assigned della Function App.
- **Storage Blob Data Owner**, scope singolo storage account: host/deployment Blob e adapter documentale read/write; nessun trigger Queue/Blob richiede ruoli dati aggiuntivi.
- **Key Vault Secrets User**, scope singolo Key Vault: lettura della connection string PostgreSQL tramite Key Vault reference.
- **Monitoring Metrics Publisher**, scope singola risorsa Application Insights: telemetria autenticata con Microsoft Entra.
- L'identità GitHub OIDC richiede `Contributor` e `User Access Administrator` (o ruolo custom equivalente) sul resource group, documentati come prerequisito manuale.
- Nessun ruolo generico a subscription scope è creato dal template.

## 10. Files to Generate

| Area | Main artifacts | Status |
|---|---|---|
| Governance | `AGENTS.md`, `README.md`, `VERSION`, `CHANGELOG.md` | Generated |
| Backend | solution, 4 progetti .NET, configurazioni e migration | Generated |
| Tests | Domain, Business, Integration xUnit | Generated |
| Frontend | React/Vite/PWA, shadcn-style UI, MSAL, i18n | Generated |
| Documentation | architecture, development, operations, guide e patch note bilingui | Generated |
| Skills | 5 skill locali, cataloghi e registry | Generated |
| Tools | skill harness, docs sync, release notes | Generated |
| Infrastructure | `infra/app.bicep`, moduli e `main.dev.bicepparam` | Generated |
| Packaging | script PowerShell e Bash | Generated |
| CI/CD | PR, deploy tag e deploy main | Generated |

## 11. Manual Prerequisites

- configurare app registration Entra External ID e relativi redirect/scope;
- configurare federated credential GitHub OIDC;
- valorizzare secret e variable GitHub documentati;
- scegliere username/password amministratore PostgreSQL al deploy senza versionarli;
- registrare i resource provider mancanti soltanto previa autorizzazione;
- confermare i nomi globalmente univoci prodotti dal naming Bicep.

## 12. Next Step

Eseguire il bootstrap approvato; nessun deploy live è autorizzato.
