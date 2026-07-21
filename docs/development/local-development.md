# Sviluppo locale

Prerequisiti: .NET 10 SDK, Node.js 22, PostgreSQL 16+, Azure Functions Core Tools 4 e Azurite.

1. Copia `local.settings.json.example` in `local.settings.json` nella Function App.
2. Avvia PostgreSQL e crea database/utente locali `kinhub`.
3. Avvia Azurite.
4. Esegui `dotnet restore KinHub.slnx`, quindi `dotnet build KinHub.slnx`.
5. Da `src/backend/applications/DA.KinHub.Functions`, esegui `func start`.
6. Da `src/frontend`, esegui `npm ci` e `npm run dev`.

Le migration automatiche sono disabilitate per default. Abilita `Database__ApplyMigrationsOnStartup=true` solo in Development. Per crearne una usa:

```bash
dotnet ef migrations add <Name> --project src/backend/infrastructure/DA.KinHub.Infrastructure --startup-project src/backend/applications/DA.KinHub.Functions
```
