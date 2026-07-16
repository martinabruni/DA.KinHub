# Migrazioni PostgreSQL

La Function App può avere più istanze: non applicare migration indiscriminatamente al cold start. In locale la feature flag `Database:ApplyMigrationsOnStartup` abilita l'esecuzione protetta da advisory lock PostgreSQL e timeout esplicito.

Per ambienti condivisi genera un bundle:

```bash
dotnet ef migrations bundle --project src/backend/infrastructure/AdvancedFrontier.Infrastructure --startup-project src/backend/applications/AdvancedFrontier.Functions --configuration Release --self-contained false --output artifacts/migrations/kinhub-migrations
```

Esegui il bundle una volta prima del deploy applicativo. Verifica `__EFMigrationsHistory`, health readiness e log. Il rollback è una migration correttiva versionata; usa `dotnet ef database update <PreviousMigration>` soltanto dopo aver verificato la reversibilità e un backup.
