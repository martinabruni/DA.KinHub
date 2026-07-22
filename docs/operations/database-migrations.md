# Migrazioni PostgreSQL

La Function App può avere più istanze: non applicare migration indiscriminatamente al cold start. In locale la feature flag `Database:ApplyMigrationsOnStartup` abilita l'esecuzione protetta da advisory lock PostgreSQL e timeout esplicito.

La slice KinList FEAT-001 introduce lo schema `shared` con profili applicativi, famiglie e membership. Ogni migration deve quindi verificare sia `__EFMigrationsHistory` sia la presenza di vincoli univoci su identità esterna e membership attiva.

Per ambienti condivisi genera un bundle:

```bash
dotnet ef migrations bundle --project src/backend/infrastructure/DA.KinHub.Infrastructure --startup-project src/backend/applications/DA.KinHub.Functions --configuration Release --self-contained false --output artifacts/migrations/kinhub-migrations
```

Esegui il bundle una volta prima del deploy applicativo. In Azure il bundle usa una connection string costruita al volo con host/database/username Entra e token `oss-rdbms` come password temporanea. Verifica `__EFMigrationsHistory`, health readiness e log. Il rollback è una migration correttiva versionata; usa `dotnet ef database update <PreviousMigration>` soltanto dopo aver verificato la reversibilità e un backup.

Prima della migration in ambienti condivisi verifica anche:

- Microsoft Entra administrator presente sul server PostgreSQL;
- principal database `kinhub_migrator` e `kinhub_app` creati o riallineati;
- grant runtime sullo schema `shared` applicati dopo il bundle;
- eventuale firewall rule temporanea del runner rimossa a fine workflow.

Per FEAT-001 verificare inoltre:

- schema `shared` creato correttamente;
- tabelle `application_users`, `families` e `family_memberships` presenti;
- indice univoco `(external_issuer, external_object_id)` presente;
- indice univoco parziale per una sola membership attiva presente;
- readiness applicativa valida dopo la migration.
