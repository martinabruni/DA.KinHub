# KinHub

KinHub è una piattaforma web calma e intuitiva per la famiglia, pensata per riunire servizi come KinRecipe e KinList con poco rumore visivo. Il bootstrap offre una base full-stack reale, localizzata, osservabile e distribuibile su Azure.

Versione corrente: `0.1.0`. Lingua predefinita: italiano; lingua supportata e fallback: inglese.

## Funzionalità iniziali

- Dashboard e area Progetti con creazione/elenco.
- Microsoft Entra External ID con MSAL e API JWT bearer.
- Help contestuale obbligatorio e guide Markdown visibili nel sito.
- Tutorial iniziale versionato, riavviabile e accessibile.
- Temi light/dark/system senza flash iniziale.
- PWA installabile con update controllato.
- Versione, build metadata, change fragment e patch note bilingui.
- Skill di progetto e tool deterministici di validazione.

## Stack

Backend .NET 10, Azure Functions 4.x Isolated Worker Linux Flex Consumption, EF Core e PostgreSQL. Frontend React 19, TypeScript strict, Vite, shadcn/ui/Radix, i18next e MSAL. Infrastruttura Bicep; pipeline GitHub Actions; Azure Static Web Apps, Storage, Key Vault, Application Insights e Log Analytics.

## Architettura e repository

Il backend separa Domain, Business, Infrastructure e Applications. Il dominio non dipende da framework. La SPA chiama l'API tramite client tipizzato e token delegato.

```text
src/backend/{domains,business,infrastructure,applications}
src/frontend
tests
docs/{architecture,development,operations,user-guide,patch-notes,FP,CR}
skills
tools/{skill-harness,docs-sync,release-notes}
infra/modules
scripts
.github/workflows
```

Consulta [AGENTS.md](AGENTS.md) prima di modificare il repository e [l'overview](docs/architecture/overview.md) per le decisioni.

## Prerequisiti

- .NET 10 SDK
- Node.js 22 e npm 10+
- PostgreSQL 16+
- Azure Functions Core Tools 4
- Azurite per `AzureWebJobsStorage` locale
- Azure CLI con Bicep CLI
- GitHub CLI per configurare repository/environment

## Avvio locale

### Database

Crea database e utente locali `kinhub`; la password di esempio è esclusivamente locale. Copia il file impostazioni:

```powershell
Copy-Item src/backend/applications/DA.KinHub.Functions/local.settings.json.example src/backend/applications/DA.KinHub.Functions/local.settings.json
```

Avvia Azurite e PostgreSQL, quindi applica le migration:

```bash
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef database update --project src/backend/infrastructure/DA.KinHub.Infrastructure --startup-project src/backend/applications/DA.KinHub.Functions
```

Per creare una migration:

```bash
dotnet ef migrations add <Name> --project src/backend/infrastructure/DA.KinHub.Infrastructure --startup-project src/backend/applications/DA.KinHub.Functions
```

In ambienti condivisi usa il migration bundle descritto in [database-migrations.md](docs/operations/database-migrations.md); non abilitare migration al cold start.

### Backend

```bash
dotnet restore KinHub.slnx
dotnet build KinHub.slnx
cd src/backend/applications/DA.KinHub.Functions
func start
```

Endpoint: `GET /health/live`, `GET /health/ready`, `GET /api/version`, `GET /api/status`, `GET /api/openapi.json`, `GET /api/kinlist/bootstrap`, `GET /api/kinlist/family-context?familyId=<uuid>`.

### Frontend

```bash
cd src/frontend
npm install
npm run dev
```

Vite gira su `http://localhost:5173` e inoltra `/api` e `/health` a Core Tools su `7071`.

## Build, test e validazioni

```bash
dotnet restore KinHub.slnx
dotnet build KinHub.slnx --configuration Release --no-restore
dotnet test KinHub.slnx --configuration Release --no-build

npm run skills:validate
npm run docs:validate
npm run release:validate

cd src/frontend
npm ci
npm run lint
npm run typecheck
npm run i18n:validate
npm run routes:validate
npm run build

az bicep build --file infra/app.bicep
```

## Publish e packaging Function App

Gli script puliscono l'output, eseguono restore/build/publish Release, iniettano versione/SHA/data/ambiente, verificano `host.json` e assembly nella root, escludono secret e creano manifest/checksum.

```powershell
./scripts/package-backend.ps1 -Environment Development
```

```bash
./scripts/package-backend.sh Development
```

Output: `artifacts/backend/kinhub-backend-<version>-<sha>.zip`, relativo `.sha256` e `build-manifest.json`.

Per pubblicare manualmente su Flex Consumption usa preferibilmente l'action ufficiale configurata nei workflow. One Deploy carica il pacchetto nel container privato indicato da `functionAppConfig.deployment.storage`; non distribuire il codice tramite Bicep.

## Frontend, i18n e documentazione

Tutti i testi React usano i18next. Italiano è default, inglese fallback. I file sono organizzati per namespace in `src/frontend/src/locales/{locale}`. I validator controllano parità delle chiavi e copertura route.

Ogni pagina usa `PageScaffold`: titolo e `PageHelpAccordion` precedono il contenuto. Il registry route richiede help it/en e slug guida. Le guide in `docs/user-guide/{it,en}` sono l'unica fonte Markdown:

```bash
npm run docs:validate
npm run docs:sync
```

## Tutorial, tema e PWA

Il tutorial usa target `data-tour`, persistenza versionata, skip/back/restart, focus management e fallback senza target. Lingua e tema persistono in localStorage. Lo script nel `<head>` evita il flash chiaro/scuro.

La PWA usa un service worker Workbox, manifest KinHub, icona SVG placeholder e cache network-first per version metadata. Desktop/Android espongono normalmente Installa; iOS richiede Condividi → Aggiungi alla schermata Home. API e login richiedono rete. Sostituire l'icona SVG con asset PNG 192/512 prima di una pubblicazione store-like.

## Skill harness

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:validate
npm run skills:build
npm run skills:watch
```

Per promuovere un componente UI o servizio business: implementazione nel layer corretto, test, esempio, documentazione, item nel catalogo, aggiornamento `SKILL.md`, registry, fragment e guide/traduzioni applicabili.

## Versioning e patch note

`VERSION` è l'unica fonte SemVer. Build backend/frontend ricevono commit, data e ambiente senza duplicare la versione. Ogni modifica significativa aggiunge un fragment:

```bash
npm run release:validate
npm run release:generate
npm run release:prepare
```

`generate` produce patch note it/en e `src/frontend/public/release-notes.json`; `release` aggiorna anche `CHANGELOG.md`.

## Microsoft Entra External ID

La configurazione completa è in [entra-external-id.md](docs/operations/entra-external-id.md). Servono due app registration:

1. API che espone lo scope delegato `access_as_user`.
2. SPA con redirect `http://localhost:5173` e URL Static Web Apps.
3. Permesso delegato SPA → API e consenso appropriato.

Il frontend usa popup con selezione account; il backend convalida JWT e scope. Nessun client secret è richiesto alla SPA o all'API.

## Infrastruttura Azure

`infra/app.bicep` usa scope resource group e moduli per:

- piano `FC1/FlexConsumption` dedicato e Function App Linux .NET 10 isolated;
- Storage LRS e container One Deploy privato;
- PostgreSQL Flexible Server Burstable `Standard_B1ms`, 32 GiB;
- Key Vault RBAC, Application Insights e Log Analytics;
- Static Web Apps Free con location fissata nel modulo a `westeurope`.

Parametri dev: `location=italynorth`, `instanceMemoryMB=2048`, `maximumInstanceCount=20`, `alwaysReadyInstanceCount=0`, concorrenza HTTP piattaforma, VNet disabilitata. Memoria/scala/always-ready restano esclusivamente in Bicep/bicepparam.

Validazione/deploy manuale:

```bash
az bicep build --file infra/app.bicep
az deployment group validate --resource-group rg-kinhub-dev --template-file infra/app.bicep --parameters infra/main.dev.bicepparam --parameters postgresAdminPassword='<VALUE>'
az deployment group create --resource-group rg-kinhub-dev --template-file infra/app.bicep --parameters infra/main.dev.bicepparam --parameters postgresAdminPassword='<VALUE>'
```

Il deploy live non è implicito nel bootstrap. Verifica sempre subscription, location, policy, provider e quota prima di eseguire `create`.

## CI/CD

- `pr-quality.yml`: qualità completa, package e Bicep; nessun deploy.
- `deploy-infrastructure.yml`: tag `infra-*`; Bicep completo, migration, One Deploy, frontend e smoke test.
- `deploy-code.yml`: push `main`; build/test/validatori, migration opzionale, One Deploy e Static Web Apps, senza modifiche infrastrutturali.

Azure login usa federated credential OIDC. Il deploy Static Web Apps usa il token dedicato del servizio. Il publish profile Function è solo fallback opzionale e non è usato dal percorso primario.

## GitHub Secrets

| Nome | Scopo | Origine |
|---|---|---|
| `AZURE_CLIENT_ID` | OIDC service principal/client | configurazione manuale/federated credential |
| `AZURE_TENANT_ID` | tenant Azure | configurazione manuale |
| `AZURE_SUBSCRIPTION_ID` | subscription target | configurazione manuale |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | deploy frontend | generato da Static Web Apps |
| `POSTGRES_ADMIN_USERNAME` | amministratore deployment DB | scelto manualmente; secret per semplicità |
| `POSTGRES_ADMIN_PASSWORD` | password amministratore DB | generata e conservata come secret |
| `POSTGRES_MIGRATION_CONNECTION_STRING` | esecuzione migration da runner, opzionale per ambiente | configurata manualmente con accesso di rete controllato |
| `ENTRA_FRONTEND_CLIENT_ID` | build SPA | app registration frontend |
| `ENTRA_BACKEND_AUDIENCE` | API audience | app registration API |
| `ENTRA_API_SCOPE` | scope completo | app registration API |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | fallback opzionale | non usato dal percorso OIDC principale |

Per ambienti GitHub distinti (`dev`, `prod`) configura secret e protection rule nell'environment appropriato.

## GitHub Variables

| Nome | Valore dev / origine |
|---|---|
| `AZURE_RESOURCE_GROUP` | `rg-kinhub-dev`, manuale |
| `AZURE_LOCATION` | `italynorth`, manuale |
| `AZURE_FUNCTIONAPP_NAME` | output Bicep copiato dopo il primo provisioning |
| `AZURE_STATIC_WEB_APP_NAME` | output Bicep, informativo |
| `AZURE_STATIC_WEB_APP_URL` | `https://<BICEP_OUTPUT_HOSTNAME>`, output Bicep copiato dopo provisioning |
| `BUILD_ENVIRONMENT` | `Development` |

Non creare Variables per memoria, scala, concorrenza o always-ready: appartengono a `main.dev.bicepparam`.

### Comandi GitHub CLI

```bash
gh secret set AZURE_CLIENT_ID --body "<VALUE>"
gh secret set AZURE_TENANT_ID --body "<VALUE>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<VALUE>"
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "<VALUE>"
gh secret set POSTGRES_ADMIN_USERNAME --body "<VALUE>"
gh secret set POSTGRES_ADMIN_PASSWORD --body "<VALUE>"
gh secret set POSTGRES_MIGRATION_CONNECTION_STRING --body "<VALUE>"
gh secret set ENTRA_FRONTEND_CLIENT_ID --body "<VALUE>"
gh secret set ENTRA_BACKEND_AUDIENCE --body "<VALUE>"
gh secret set ENTRA_API_SCOPE --body "<VALUE>"

gh variable set AZURE_RESOURCE_GROUP --body "rg-kinhub-dev"
gh variable set AZURE_LOCATION --body "italynorth"
gh variable set AZURE_FUNCTIONAPP_NAME --body "<BICEP_OUTPUT>"
gh variable set AZURE_STATIC_WEB_APP_NAME --body "<BICEP_OUTPUT>"
gh variable set AZURE_STATIC_WEB_APP_URL --body "https://<BICEP_OUTPUT_HOSTNAME>"
gh variable set BUILD_ENVIRONMENT --body "Development"
```

## Costi, cold start e troubleshooting

Flex scala a zero e non usa always-ready in dev. PostgreSQL Burstable è il costo persistente principale e può essere arrestato quando non usato. Mantieni startup leggero e telemetria campionata.

- Startup fallisce: controlla `DOTNET_ENVIRONMENT`, placeholder Entra e connection string.
- `host.json` non trovato: ricrea il package con lo script e non zippare la cartella padre.
- Storage 403: verifica managed identity, ruolo Blob Data Owner, container privato e propagazione RBAC.
- Function non scala/provisiona: verifica `italynorth`, quota Flex e registrazione provider.
- Frontend su F5 restituisce 404: verifica che `staticwebapp.config.json` sia nel `dist`.
- Readiness 503: controlla PostgreSQL, Key Vault reference e migration.

## Passaggi manuali

- Creare/configurare app registration External ID e consenso.
- Creare federated credential GitHub OIDC e assegnare ruoli minimi.
- Valorizzare secret/variable per environment.
- Eseguire il primo workflow infrastrutturale tramite tag dopo validazione Azure.
- Copiare output Function/SWA nelle Variables richieste e recuperare il token SWA.
- Sostituire icone PWA placeholder e verificare installazione sui browser target.
