# Piano di implementazione - FEAT-001

## Obiettivo

Implementare il collegamento idempotente `(iss, oid)` verso il profilo interno, determinare la membership familiare attiva, instradare la PWA verso KinList o onboarding e introdurre il confine autorizzativo `Family`, senza esporre dati familiari o conservarli offline.

## Contratti esecutivi

- Bootstrap: `GET /api/kinhub/bootstrap`, protetto da `ApiAccess`.
- Risposta associata: `{ "state": "family", "familyId": "<uuid>" }`.
- Risposta non associata: `{ "state": "onboarding" }`, senza campi familiari.
- `familyId`: UUID obbligatorio nella query di ogni futura API protetta da `Family`.
- Membership inattiva o assente: onboarding nel bootstrap, `403` nelle API `Family`.
- Profilo inattivo: accesso negato senza riattivazione o ricreazione automatica.
- Errori: `401 auth.required`, `401 auth.requiredClaims`, `400 family.idRequired`, `400 family.idInvalid`, `403 family.accessDenied`, `503 dependency.postgresqlUnavailable`.
- Risposte autenticate: `Cache-Control: no-store, private`.
- Route frontend canonica: `/kinlist`.
- Le superfici dimostrative `/projects` vengono rimosse dal routing e dagli endpoint pubblici; tabella e migration esistenti non vengono riscritte o eliminate.
- `familyId` e dati familiari restano solo in memoria; la cache MSAL di account/token usa `sessionStorage`, limitata alla sessione della scheda e distinta dalla persistenza applicativa vietata.

## 1. Chiudere i gate tecnici

### TECH-001

- Verificare authority External ID, issuer, audience e forma effettiva dello scope `scp`.
- Acquisire token rappresentativi senza versionarli.
- Verificare token valido, audience/scope errati e assenza di `iss` o `oid`.
- Configurare `MapInboundClaims = false`.
- Documentare evidenze e configurazione in `docs/operations/entra-external-id.md`.

### TECH-002

- Inventariare Flexible Server, rete, amministratore Entra, Function managed identity e principal OIDC della pipeline.
- Verificare la connessione PostgreSQL tramite token Entra.
- Definire principal separati per runtime, migration e amministrazione.
- Confermare che il percorso non richieda nuove risorse Azure.
- Preparare cutover in due passaggi: abilitazione Entra e verifica, poi disabilitazione password e rimozione del secret legacy.

## 2. Modello shared e persistenza

Creare nei layer esistenti:

- `Identity/ExternalIdentity.cs`
- `Identity/ApplicationUser.cs`
- `Identity/IApplicationUserRepository.cs`
- `Families/Family.cs`
- `Families/FamilyMembership.cs`
- `Families/IFamilyMembershipRepository.cs`

Aggiungere allo schema PostgreSQL `shared`:

| Tabella | Contenuto principale |
|---|---|
| `application_users` | ID interno, issuer, object ID, creazione, inattivazione |
| `families` | ID e lifecycle minimo; nome e creazione restano FEAT-002 |
| `family_memberships` | user, family, creazione, inattivazione |

Vincoli:

- Unique `(issuer, object_id)`.
- Unique `(user_id, family_id)`.
- Unique parziale su `user_id WHERE inactive_at IS NULL`.
- Foreign key verso profilo e famiglia.
- Indici per bootstrap e verifica `Family`.

Implementare `GetOrCreateAsync` atomicamente con `INSERT ... ON CONFLICT ... RETURNING`, evitando il pattern concorrente select-then-insert.

Aggiornare:

- `KinHubDbContext`
- configurazioni EF separate;
- repository Infrastructure;
- registrazioni DI;
- snapshot EF;
- nuova migration completa di designer.

Prima della nuova migration verificare che `InitialCreate` venga scoperta correttamente da EF, senza modificarne la storia se già applicata.

## 3. Business e bootstrap

Creare un servizio scoped di bootstrap che:

1. Riceve esclusivamente un'identità canonica già estratta da claim verificati.
2. Crea o riusa atomicamente il profilo.
3. Cerca la sola membership attiva.
4. Restituisce `family` con il solo `familyId`, oppure `onboarding`.
5. Propaga `CancellationToken`.
6. Traduce un guasto repository in errore tecnico, mai in onboarding.

Creare un servizio mirato per la verifica della membership usato dalla policy `Family`. Non introdurre generic repository, mediator o cache membership.

Aggiungere test unitari per:

- identità valida e claim concettualmente invalidi;
- profilo creato e riusato;
- membership attiva, inattiva e assente;
- errore repository distinto da onboarding;
- propagazione della cancellazione.

## 4. Autenticazione e policy `Family`

Rifattorizzare `ApiAuthorization`:

- lifetime scoped;
- risultato tipizzato invece di `bool`;
- distinzione fra autenticazione mancante, scope negato e claim obbligatori assenti;
- nessun bypass identificativo quando `Entra:Enabled` è falso.

Creare:

- resolver dei claim `iss` e `oid`;
- `FamilyAuthorizationRequirement`;
- resource contenente il `familyId` validato;
- `FamilyAuthorizationHandler` scoped e asincrono.

Registrare una policy chiamata esattamente `Family`.

Flusso delle API familiari:

1. Autenticare e verificare `ApiAccess`.
2. Validare `familyId` dalla query.
3. Risolvere `(iss, oid)`.
4. Verificare la membership nel database.
5. Restituire `403` generico quando non attiva.
6. Restituire `503` per dipendenza indisponibile.
7. Passare lo stesso `familyId` a caso d'uso e repository.

## 5. Endpoint, errori e telemetria

Creare la Function di bootstrap e aggiornare OpenAPI.

Estendere `ApiResults` per:

- applicare correlation ID in modo uniforme;
- aggiungere gli header `no-store`;
- produrre sempre Problem Details con `code` e `traceId`;
- non includere claim, `familyId` o dettagli famiglia nei `403`.

Introdurre trace e metriche a bassa cardinalità:

- durata/esito bootstrap;
- profilo creato o riusato;
- esito family/onboarding;
- durata/esito policy;
- conteggio `403`;
- claim mancanti;
- guasti repository.

Usare solo dimensioni come `operation`, `outcome` ed `errorCategory`. Non registrare token, issuer, oid, familyId, nomi o payload.

## 6. Frontend e instradamento

Centralizzare in `lib/auth.ts`:

- account attivo;
- acquisizione token;
- classificazione della sessione scaduta;
- login e logout;
- cache MSAL esclusivamente in memoria.

Estendere `lib/api.ts` con:

- DTO discriminato del bootstrap;
- parsing robusto di Problem Details;
- errori classificati `unauthenticated`, `forbidden`, `network`, `server`, `invalid-response`;
- `cache: "no-store"`;
- correlation ID;
- nessuna richiesta senza token.

Creare:

- `KinListAccessGate`;
- contesto famiglia solo in memoria;
- provider/hook di connettività;
- `KinListPage`.

La macchina a stati distingue:

| Stato | Comportamento |
|---|---|
| Loading | Nessun contenuto familiare montato |
| Family | Shell KinList con `familyId` solo in memoria |
| Onboarding | Solo `Crea una famiglia` e `Unisciti con un codice` |
| Sessione scaduta | Nuovo login |
| Accesso negato | Messaggio distinto dall'empty state |
| Errore tecnico | Retry esplicito |
| Offline | Shell pubblica, dati rimossi e operazioni remote disabilitate |

Su logout, cambio account, `401`, `403` o evento offline, cancellare immediatamente contesto e componenti familiari. Alla riconnessione eseguire un nuovo bootstrap autorevole.

Registrare `/kinlist` in `App.tsx` e `route-registry.json`, usando `PageScaffold`.

## 7. PWA, localizzazione e accessibilità

Aggiornare Workbox:

- precache limitata agli asset pubblici versionati;
- `/api/**` esplicitamente `NetworkOnly`;
- nessun Background Sync;
- cache runtime solo per metadata pubblici già approvati;
- pulizia delle cache obsolete.

Aggiungere testi `it` ed `en` per tutti gli stati, help completo e guide bilingui KinList.

Verificare:

- focus al titolo e dopo i cambi di stato;
- `role="status"` per loading;
- `role="alert"` per errori;
- nomi accessibili nella navigazione mobile;
- tastiera, touch target, zoom, temi e reduced motion;
- nessun flash di dati familiari precedenti.

## 8. PostgreSQL con managed identity

Introdurre una configurazione database tipizzata:

- `ManagedIdentity` obbligatoria fuori Development.
- Modalità locale esplicita e senza fallback automatico.
- Host, database, username, SSL e timeout validati all'avvio.

Configurare Npgsql con un token provider periodico per lo scope Azure PostgreSQL e usare il resulting `NpgsqlDataSource` nel DbContext e negli health check.

Aggiornare Bicep per:

- abilitare Entra sul Flexible Server;
- configurare l'amministratore Entra approvato;
- passare alla Function solo host, database, username e modalità;
- rimuovere la connection string PostgreSQL dal Key Vault;
- disabilitare password auth dopo il preflight;
- mantenere invariati piano, rete e risorse esistenti.

Aggiornare i workflow per:

- serializzare i deploy per ambiente;
- eseguire Azure login prima della migration;
- acquisire token PostgreSQL senza stamparlo;
- creare/verificare idempotentemente i principal;
- rendere la migration obbligatoria prima del deploy codice;
- verificare migration history, schema, indici e grant;
- conservare bundle, checksum e ZIP N-1.

## 9. Test

Aggiungere test PostgreSQL reali, preferibilmente con Testcontainers:

- applicazione migration su database vuoto;
- bootstrap concorrenti con DbContext separati;
- un solo profilo per `(iss, oid)`;
- una sola membership attiva;
- membership inattiva non autorizzante;
- family diversa non autorizzante;
- rollback e indici attesi.

Aggiungere test Functions per:

- `401`, `403`, `503`;
- claim mancanti senza fallback;
- policy `Family` e lifetime scoped;
- assenza di dettagli sensibili;
- correlation ID e media type;
- endpoint bootstrap e OpenAPI.

Aggiungere test frontend mirati per:

- routing family/onboarding;
- loading senza mount dei figli;
- invalidazione su account/offline;
- retry dopo rete o errore tecnico;
- sessione scaduta e accesso negato;
- API network-only e no-store;
- accessibilità essenziale.

## 10. Documentazione e artefatti

Aggiornare:

- `docs/operations/database-migrations.md`
- `docs/operations/entra-external-id.md`
- `docs/operations/azure-deployment.md`
- guida KinList `it`/`en`
- help e route registry
- `README.md` se cambiano configurazione o comandi
- skill backend/frontend e cataloghi se i nuovi pattern vengono promossi
- `AGENTS.md` se viene reso obbligatorio un nuovo comando di test
- change fragment bilingue

Rigenerare documentazione, registry skill e release notes senza modificare manualmente gli artefatti generati.

## 11. Verifica finale

Eseguire:

```text
dotnet restore KinHub.slnx
dotnet build KinHub.slnx --configuration Release --no-restore
dotnet test KinHub.slnx --configuration Release --no-build
dotnet ef migrations list ...
dotnet ef migrations script --idempotent ...
dotnet ef migrations bundle ...
dotnet publish ...
./scripts/package-backend.ps1 -Environment Development

npm ci --prefix src/frontend
npm run --prefix src/frontend test
npm run --prefix src/frontend lint
npm run --prefix src/frontend typecheck
npm run --prefix src/frontend i18n:validate
npm run --prefix src/frontend routes:validate
npm run --prefix src/frontend build

npm run docs:validate
npm run docs:sync
npm run skills:validate
npm run skills:build
npm run release:validate
az bicep build --file infra/main.bicep
```

Completare test manuali su Chrome desktop, Chrome Android/PWA ed Edge per login associato/non associato, refresh, URL diretto, sessione scaduta, cambio account, perdita rete, riconnessione, installazione e avvio offline.

## Sequenza di rilascio

1. Chiudere TECH-001 e inventario TECH-002.
2. Implementare schema shared e contratti backend.
3. Applicare migration additive.
4. Abilitare Entra PostgreSQL mantenendo temporaneamente il percorso amministrativo esistente.
5. Provisionare e verificare i principal.
6. Distribuire backend managed-identity e verificare readiness.
7. Distribuire frontend e verificare bootstrap/offline.
8. Disabilitare password PostgreSQL.
9. Rimuovere secret e riferimenti legacy.
10. Conservare evidenze, bundle e ZIP N-1.

Il rollback applicativo usa ZIP N-1; quello dati preferisce una migration correttiva. Non eseguire `Down` dopo scritture senza verifica di reversibilità e backup.
