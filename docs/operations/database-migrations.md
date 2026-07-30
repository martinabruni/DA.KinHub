# Migrazioni PostgreSQL

La Function App può avere più istanze: non applicare migration indiscriminatamente al cold start. In locale la feature flag `Database:ApplyMigrationsOnStartup` abilita l'esecuzione protetta da advisory lock PostgreSQL e timeout esplicito.

La slice KinHub FEAT-001 introduce lo schema `shared` con profili applicativi, famiglie e membership. Ogni migration deve quindi verificare sia `__EFMigrationsHistory` sia la presenza di vincoli univoci su identità esterna e membership attiva.

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

Per FEAT-002 verificare inoltre prima del deploy:

- assenza di righe legacy in `shared.families` con `SELECT COUNT(*) FROM shared.families;` e stop immediato se il risultato non è `0`;
- presenza della migration FEAT-001 in `__EFMigrationsHistory`;
- grant runtime e migration ancora validi sullo schema `shared`.

Dopo la migration FEAT-002 verificare:

- colonne `name` e `created_by_application_user_id` in `shared.families`;
- foreign key `FK_families_application_users_created_by_application_user_id` presente;
- indice parziale `IX_family_memberships_single_active_user` ancora presente;
- nessuna famiglia orfana con:

```sql
SELECT f."Id"
FROM shared.families f
LEFT JOIN shared.family_memberships fm ON fm.family_id = f."Id" AND fm.inactive_at IS NULL
WHERE fm."Id" IS NULL;
```

Il rollback operativo di FEAT-002 usa il `Down` solo prima di creare la prima famiglia nel nuovo modello. Dopo scritture reali preferire una migration correttiva compatibile con i dati e verificare backup o PITR prima di ogni inversione.

## FEAT-015 - Catalogo servizi familiari

Prima della migration FEAT-015 verificare inoltre:

- presenza della migration FEAT-002 in `__EFMigrationsHistory`;
- grant runtime e migration ancora validi sullo schema `shared`;
- assenza preventiva di tabelle applicative omonime create manualmente fuori da EF.

Dopo la migration FEAT-015 verificare:

- tabelle `shared.kin_services`, `shared.kin_service_localizations` e `shared.family_kin_service_availabilities` presenti;
- servizio `kinlist` presente una sola volta con route `/kinlist`, `is_active = true` e `is_preconfigured = true`;
- due localizzazioni `it` ed `en` presenti per `kinlist`;
- una disponibilita attiva per ogni famiglia attiva esistente con:

```sql
SELECT COUNT(*)
FROM shared.families f
LEFT JOIN shared.family_kin_service_availabilities availability
  ON availability.family_id = f."Id"
 AND availability.kin_service_id = (SELECT "Id" FROM shared.kin_services WHERE key = 'kinlist')
WHERE f.inactive_at IS NULL
  AND availability."Id" IS NULL;
```

- assenza di duplicati con:

```sql
SELECT family_id, kin_service_id, COUNT(*)
FROM shared.family_kin_service_availabilities
GROUP BY family_id, kin_service_id
HAVING COUNT(*) > 1;
```

- assenza di record orfani verso famiglie o servizi.

Il `Down` di FEAT-015 e usabile solo su database disposable o prima di scritture dipendenti. Dopo famiglie o disponibilita reali il rollback operativo usa una migration correttiva compatibile, preceduta da backup o PITR e da verifica esplicita di servizi, localizzazioni e disponibilita gia scritte.
