# KinHub

KinHub è la home familiare minimalista per accedere a KinRecipe, KinList, KinDrive e futuri servizi. Lingua predefinita `it`, fallback `en`.

## Avvio

Prerequisiti: .NET 10 SDK, Node.js 22+, PostgreSQL 16 opzionale.

```bash
dotnet build KinHub.sln
dotnet test KinHub.sln
cd src/frontend && npm install && npm run dev
```

Usare `src/frontend/.env.example` e i placeholder Entra/Azure documentati in `infra/README.md`. I workflow usano OIDC; non inserire segreti nei file.

## Controlli

```bash
npm run skills:validate
npm run docs:validate
npm run i18n:validate
npm run changes:validate
```

Il frontend include Home, Progetti, Impostazioni, About/Version, Release notes, 404, i18n it/en, tema light/dark/system, tutorial, help accordion e PWA. `VERSION` è la fonte SemVer condivisa.

Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_STATIC_WEB_APPS_API_TOKEN`, `POSTGRES_ADMIN_PASSWORD`.

Variables: `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION`, `AZURE_ACR_NAME`, `AZURE_WEBAPP_NAME`, `ENTRA_FRONTEND_CLIENT_ID`, `ENTRA_BACKEND_AUDIENCE`, `ENTRA_API_SCOPE`, `POSTGRES_ADMIN_USERNAME`.

```bash
gh secret set AZURE_CLIENT_ID --body "<VALUE>"
gh secret set AZURE_TENANT_ID --body "<VALUE>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<VALUE>"
gh variable set AZURE_RESOURCE_GROUP --body "<VALUE>"
gh variable set AZURE_LOCATION --body "westeurope"
```

Configurazione manuale: app registration Entra frontend, redirect URI `<ENTRA_REDIRECT_URI>`, expose API e scope `<ENTRA_API_SCOPE>`; Azure resource group, provider e password PostgreSQL. Nessun valore reale è versionato.
