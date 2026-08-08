# KinHub

KinHub è una piattaforma web calma e intuitiva per la famiglia, pensata per riunire servizi come KinRecipe e KinList con poco rumore visivo. Il bootstrap offre una base full-stack reale, localizzata, osservabile e distribuibile su Azure.

Versione corrente: `0.1.0`. Lingua predefinita: italiano; lingua supportata e fallback: inglese.

## Funzionalità iniziali

- Dashboard, accesso KinList post-login e shell pubblica offline senza dati personali.
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
.agents/skills
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

Endpoint: `GET /health/live`, `GET /health/ready`, `GET /api/version`, `GET /api/status`, `GET /api/openapi.json`, `GET /api/kinhub/bootstrap`, `GET /api/kinhub/family-context?familyId=<uuid>`.

### Frontend

```bash
cd src/frontend
npm install
npm run test
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
npm run test
npm run lint
npm run typecheck
npm run i18n:validate
npm run routes:validate
npm run build

az bicep build --file infra/main.bicep
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

`infra/main.bicep` usa scope resource group e moduli per:

- piano `FC1/FlexConsumption` dedicato e Function App Linux .NET 10 isolated;
- Storage LRS e container One Deploy privato;
- PostgreSQL Flexible Server Burstable `Standard_B1ms`, 32 GiB, con Microsoft Entra admin e autenticazione passwordless per runtime/migration;
- Key Vault RBAC, Application Insights e Log Analytics;
- Static Web Apps Standard in `westeurope`, collegata alla Function tramite `/api`.

Parametri dev: `location=italynorth`, `instanceMemoryMB=2048`, `maximumInstanceCount=20`, `alwaysReadyInstanceCount=0`, concorrenza HTTP piattaforma, VNet disabilitata. Memoria/scala/always-ready restano esclusivamente in Bicep/bicepparam.

Validazione/deploy manuale:

```bash
az bicep build --file infra/main.bicep
mkdir -p artifacts/infra
az bicep build-params --file infra/environments/dev.bicepparam --outfile artifacts/infra/dev.parameters.json
az deployment group validate --resource-group rg-kinhub-dev --template-file infra/main.bicep --parameters @artifacts/infra/dev.parameters.json postgresAdminUsername='<VALUE>' postgresAdminPassword='<VALUE>' azureTenantId='<AZURE_TENANT_ID>' entraInstance='https://<TENANT_SUBDOMAIN>.ciamlogin.com/' entraTenantId='<ENTRA_TENANT_ID>' entraBackendAudience='<ENTRA_BACKEND_CLIENT_ID>' postgresEntraAdministratorName='<POSTGRES_ENTRA_ADMIN_NAME>' postgresEntraAdministratorObjectId='<POSTGRES_ENTRA_ADMIN_OBJECT_ID>'
az deployment group what-if --name kinhub-dev-infrastructure --resource-group rg-kinhub-dev --template-file infra/main.bicep --parameters @artifacts/infra/dev.parameters.json
```

Il deploy live non è implicito nel bootstrap. Verifica sempre subscription, location, policy, provider e quota prima di eseguire `create`.

## CI/CD

- `ci.yml`: qualità completa su pull request, senza secret Azure.
- `infrastructure.yml`: validate, what-if e provisioning incrementale da `main` o dispatch.
- `release.yml`: build once, migration, One Deploy Function e Static Web Apps con gli artifact della stessa release.

La release usa gli output del deployment ARM stabile e non esegue discovery euristica di hostname o risorse. Le action esterne sono fissate a SHA completi e gli artifact di release durano 30 giorni.

Azure login usa federated credential OIDC. Il workflow recupera il token Static Web Apps tramite Azure CLI dopo il login e lo maschera prima del deploy. Il publish profile Function è solo fallback opzionale e non è usato dal percorso primario.

## GitHub Secrets

| Nome | Scopo | Origine |
|---|---|---|
| `AZURE_CLIENT_ID` | OIDC service principal/client | configurazione manuale/federated credential |
| `AZURE_TENANT_ID` | tenant Azure | configurazione manuale |
| `AZURE_SUBSCRIPTION_ID` | subscription target | configurazione manuale |
| `POSTGRES_ADMIN_USERNAME` | bootstrap del server PostgreSQL Flexible Server | scelto manualmente; usato solo in provisioning infrastrutturale |
| `POSTGRES_ADMIN_PASSWORD` | bootstrap del server PostgreSQL Flexible Server | generata e conservata come secret; non usata dal runtime applicativo |
| `ENTRA_TENANT_ID` | tenant clienti External ID, distinto dal tenant Azure | app registration External ID |
| `ENTRA_FRONTEND_CLIENT_ID` | build SPA | app registration frontend |
| `ENTRA_BACKEND_AUDIENCE` | Application (client) ID GUID dell'API, uguale al claim `aud` v2 | app registration API |
| `ENTRA_API_SCOPE` | scope completo | app registration API |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | fallback opzionale | non usato dal percorso OIDC principale |

Per ambienti GitHub distinti (`dev`, `prod`) configura secret e protection rule nell'environment appropriato.

## GitHub Variables

| Nome | Valore dev / origine |
|---|---|
| `AZURE_RESOURCE_GROUP` | `rg-kinhub-dev`, manuale |
| `AZURE_LOCATION` | `italynorth`, manuale |
| `ENTRA_INSTANCE` | `https://<tenant-subdomain>.ciamlogin.com/` | tenant clienti External ID |

Non creare Variables per memoria, scala, concorrenza o always-ready: appartengono a `environments/dev.bicepparam`.

### Comandi GitHub CLI

```bash
gh secret set AZURE_CLIENT_ID --body "<VALUE>"
gh secret set AZURE_TENANT_ID --body "<VALUE>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<VALUE>"
gh secret set POSTGRES_ADMIN_USERNAME --body "<VALUE>"
gh secret set POSTGRES_ADMIN_PASSWORD --body "<VALUE>"
gh secret set ENTRA_TENANT_ID --body "<VALUE>"
gh secret set ENTRA_FRONTEND_CLIENT_ID --body "<VALUE>"
gh secret set ENTRA_BACKEND_AUDIENCE --body "<VALUE>"
gh secret set ENTRA_API_SCOPE --body "<VALUE>"

gh variable set AZURE_RESOURCE_GROUP --body "rg-kinhub-dev"
gh variable set AZURE_LOCATION --body "italynorth"
gh variable set AZURE_FUNCTIONAPP_NAME --body "<BICEP_OUTPUT>"
gh variable set AZURE_FUNCTIONAPP_URL --body "https://<BICEP_OUTPUT_HOSTNAME>"
gh variable set AZURE_STATIC_WEB_APP_NAME --body "<BICEP_OUTPUT>"
gh variable set AZURE_STATIC_WEB_APP_URL --body "https://<BICEP_OUTPUT_HOSTNAME>"
gh variable set BUILD_ENVIRONMENT --body "Development"
gh variable set ENTRA_INSTANCE --body "https://<TENANT_SUBDOMAIN>.ciamlogin.com/"
```

## Costi, cold start e troubleshooting

Flex scala a zero e non usa always-ready in dev. PostgreSQL Burstable è il costo persistente principale e può essere arrestato quando non usato. Mantieni startup leggero e telemetria campionata.

- Startup fallisce: controlla `DOTNET_ENVIRONMENT`, placeholder Entra e impostazioni `Database__Mode`/`Database__Host`/`Database__Username`.
- `host.json` non trovato: ricrea il package con lo script e non zippare la cartella padre.
- Storage 403: verifica managed identity, ruolo Blob Data Owner, container privato e propagazione RBAC.
- Function non scala/provisiona: verifica `italynorth`, quota Flex e registrazione provider.
- Frontend su F5 restituisce 404: verifica che `staticwebapp.config.json` sia nel `dist`.
- Readiness 503: controlla PostgreSQL, principal Entra, grant runtime e migration.

## Passaggi manuali

- Creare/configurare app registration External ID e consenso.
- Creare federated credential GitHub OIDC e assegnare ruoli minimi.
- Valorizzare secret/variable per environment.
- Per un ambiente nuovo, eseguire prima `infrastructure.yml` da `main` o dispatch manuale, recuperare il token SWA e configurare i secret dell'environment `dev`; quindi eseguire `release.yml`.
- Copiare gli output Function/SWA nelle Variables richieste; i merge successivi su `main` attiveranno soltanto gli scope le cui cartelle sono cambiate.
- Sostituire icone PWA placeholder e verificare installazione sui browser target.
