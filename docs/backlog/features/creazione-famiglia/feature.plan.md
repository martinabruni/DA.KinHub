# Piano di implementazione - FEAT-002

## Obiettivo

Consentire a un utente autenticato senza membership attiva di creare una famiglia tramite il solo nome, salvare famiglia e membership del creatore nello stesso commit e aprire KinList, garantendo un unico contesto familiare anche con retry o richieste concorrenti e senza introdurre ruoli, inviti o dati persistenti nel browser.

## Contratti esecutivi

- Creazione: `POST /api/kinhub/families`, protetta dal default `ApiAccess` e non dalla policy `Family`.
- Request: `{ "name": "<nome famiglia>" }`; user ID, `familyId`, creatore, ruolo e timestamp non sono accettati dal client.
- Prima creazione: `201` con `{ "state": "family", "familyId": "<uuid>" }`.
- Retry, invio concorrente o membership attiva gia esistente: `200` con lo stesso contesto famiglia autorevole, senza una seconda scrittura.
- Nome famiglia: da 1 a 100 caratteri dopo trim e normalizzazione delle sequenze di spazi; Unicode ammesso, caratteri di controllo rifiutati, maiuscole preservate e nessuna unicita globale.
- Creatore: metadato stabile della famiglia e unico membro iniziale, senza ruolo o capacita aggiuntive.
- Errori applicabili: `400 family.nameInvalid`, errori `401`/`403` gia definiti da FEAT-001, `503 dependency.postgresqlUnavailable` e `500 internal.unexpected`.
- Risposte autenticate ed errori: `Cache-Control: no-store, private`, correlation ID e Problem Details centralizzati.
- Route frontend canonica: `/kinlist`; scelta e form restano stati interni della pagina, senza una nuova route.
- Nome inserito e `familyId` restano solo in memoria; nessuna persistenza browser, cache API o coda offline.
- Migration: preflight fail-fast se `shared.families` contiene righe legacy prive di nome e creatore; nessun backfill sintetico.

## 1. Verificare prerequisiti e baseline

Confermare che FEAT-001 sia disponibile nel codice corrente:

- bootstrap `GET /api/kinhub/bootstrap` e stato `onboarding`;
- profilo applicativo risolto tramite `(iss, oid)`;
- `KinHubAuthorizationFeature` popolata dalla pipeline HTTP;
- policy default `ApiAccess` e marker `[RequiresFamilyAccess]`;
- schema `shared` con `application_users`, `families` e `family_memberships`;
- indice univoco parziale `IX_family_memberships_single_active_user`;
- `KinListAccessGate` e contesto famiglia solo in memoria.

Prima della migration eseguire in ogni ambiente target un preflight che verifichi:

- assenza di righe in `shared.families`;
- migration FEAT-001 presente in `__EFMigrationsHistory`;
- vincolo univoco della membership attiva presente;
- principal runtime e migration ancora autorizzati sullo schema `shared`;
- readiness PostgreSQL valida.

Se esistono famiglie legacy, interrompere il rilascio e definire un backfill approvato per ogni riga. Non assegnare automaticamente nomi o creatori tecnici.

## 2. Nome e invarianti di dominio

Creare `Families/FamilyName.cs` come value object autorevole.

La normalizzazione deve:

1. Rimuovere gli spazi iniziali e finali.
2. Comprimere ogni sequenza di whitespace interno in un singolo spazio.
3. Rifiutare il valore vuoto dopo la normalizzazione.
4. Rifiutare oltre 100 caratteri dopo la normalizzazione.
5. Rifiutare caratteri di controllo.
6. Preservare lettere Unicode, segni diacritici, apostrofi, punteggiatura e maiuscole valide.

Non introdurre confronto case-insensitive, slug, indice univoco sul nome o regole ASCII-only.

Aggiornare `Families/Family.cs` con:

- `FamilyName Name`;
- `Guid CreatedByApplicationUserId`;
- factory che richiede nome, creatore e timestamp server-side;
- invarianti per creatore non vuoto e famiglia inizialmente attiva.

Mantenere `FamilyMembership` priva di ruolo, owner flag o permessi. Il collegamento fra `CreatedByApplicationUserId` e il creatore e un metadato della famiglia, non un confine autorizzativo.

## 3. Contratto atomico di persistenza

Creare nel Domain un repository mirato alla famiglia, senza estendere il repository read-only delle membership con una generica unita di lavoro.

Il contratto di creazione deve:

- ricevere profilo, famiglia candidata e membership candidata oppure gli input dominio equivalenti;
- restituire un risultato tipizzato `Created` o `Existing` con il solo `familyId` necessario al Business;
- propagare `CancellationToken`;
- garantire che `Created` corrisponda a un unico commit di famiglia e membership;
- restituire il contesto esistente quando una membership attiva e gia presente.

Implementare il repository Infrastructure usando la execution strategy EF configurata e una transazione esplicita:

1. Avviare l'intera operazione dentro `Database.CreateExecutionStrategy().ExecuteAsync(...)`.
2. Aprire la transazione PostgreSQL.
3. Acquisire un lock `SELECT ... FOR UPDATE` sulla riga `application_users` del chiamante.
4. Ricontrollare dopo il lock l'eventuale membership attiva e la famiglia attiva collegata.
5. Se esiste, restituire `Existing` senza modificare nome, creatore o membership.
6. Se non esiste, aggiungere `Family` e `FamilyMembership` allo stesso `KinHubDbContext`.
7. Eseguire un solo `SaveChangesAsync` e commit.
8. Su errore, eseguire rollback e non lasciare la famiglia candidata nel database.

L'indice univoco parziale resta il backstop dati per chiamanti concorrenti. Gestire una violazione attesa tramite SQLSTATE e nome esatto del vincolo, senza classificare ogni errore PostgreSQL come indisponibilita. Dopo un conflitto concorrente riconciliabile, rileggere il contesto autorevole e restituire `Existing`.

Aggiornare le query di membership affinche una membership conceda contesto solo quando sia la membership sia la famiglia associata sono attive.

## 4. Migration dello schema shared

Aggiornare la configurazione EF di `Family` e aggiungere a `shared.families`:

| Colonna | Tipo | Vincolo |
|---|---|---|
| `name` | `varchar(100)` | `NOT NULL` |
| `created_by_application_user_id` | `uuid` | `NOT NULL`, FK verso `shared.application_users` |

Usare `DeleteBehavior.Restrict` per il creatore e non aggiungere un indice univoco sul nome.

Generare una migration additiva dal design-time factory autorevole in Infrastructure. La migration deve:

- eseguire per prima una verifica fail-fast delle righe gia presenti in `shared.families`;
- aggiungere colonne senza default vuoti o GUID sintetici;
- aggiungere la foreign key del creatore;
- preservare tabelle, indici e migration FEAT-001;
- includere designer e snapshot generati da EF;
- avere un `Down` che rimuove soltanto foreign key e colonne FEAT-002.

Il `Down` e operativo soltanto prima di scritture con il nuovo modello. Dopo la prima famiglia creata, il rollback dati preferisce una migration correttiva e richiede verifica di reversibilita e backup/PITR.

## 5. Business e gestione degli esiti

Creare un servizio scoped `FamilyCreationService` che:

1. Riceve `ExternalIdentity`, nome fornito e `CancellationToken`.
2. Crea o riusa il profilo applicativo tramite il contratto FEAT-001.
3. Rifiuta un profilo inattivo senza riattivarlo o ricrearlo.
4. Costruisce `FamilyName`, traducendo la violazione in `BusinessValidationException` con `family.nameInvalid`.
5. Acquisisce il timestamp da `TimeProvider`, non dalla request.
6. Costruisce famiglia e membership del solo creatore.
7. Invoca il repository atomico.
8. Restituisce un risultato applicativo `Created` o `Existing` con `familyId`.
9. Traduce soltanto i guasti reali di persistenza in `BusinessDependencyException`.
10. Preserva cancellazioni e bug applicativi senza convertirli in indisponibilita PostgreSQL.

La validazione del body e del nome precede l'esito idempotente: una request malformata resta `400` anche quando il chiamante possiede gia una famiglia. Una request valida di un utente gia associato restituisce invece il contesto esistente.

Registrare servizio e repository come scoped nelle rispettive estensioni DI. Non introdurre mediator, generic repository, service locator, current user ambientale o cache della membership.

## 6. Endpoint, Problem Details e OpenAPI

Creare una HTTP Function dedicata alla creazione famiglia:

- binding `POST` con `AuthorizationLevel.Anonymous`;
- nessun `[AllowAnonymous]`;
- nessun `[RequiresFamilyAccess]`;
- identita letta esclusivamente da `KinHubAuthorizationFeature`;
- body con il solo campo `name`;
- `201` per `Created` e `200` per `Existing`;
- stesso payload famiglia per entrambi gli esiti;
- nessun `familyId` nella query, perche il contesto non esiste ancora.

JSON assente, malformato o incompatibile deve diventare `400 family.nameInvalid` o un codice sintattico stabile documentato, mai `500`. L'endpoint non replica autenticazione, correlation ID, cache header, costruzione Problem Details o mapping delle eccezioni gia gestiti dai middleware.

Aggiornare:

- `Http/ApiRoutes.cs` con la route condivisa;
- `OpenApi/OpenApiDocumentProvider.cs` con request body, response e security bearer;
- test di parita route/OpenAPI per leggere i metodi reali da `HttpTriggerAttribute` invece di assumere sempre `GET`;
- documentazione OpenAPI di `400`, `401`, `403`, `500` e `503` applicabili.

## 7. Telemetria redatta

Aggiungere un operation name KinHub per la creazione famiglia e usare l'operation scope condiviso.

Misurare con dimensioni finite e a bassa cardinalita:

- tentativo di creazione;
- esito `created`;
- esito `existing` per retry o membership gia attiva;
- validazione rifiutata;
- conflitto concorrente riconciliato;
- rollback della transazione;
- dipendenza PostgreSQL indisponibile;
- durata complessiva.

L'esito pubblico di un conflitto riconciliato resta `200` con il contesto esistente. La metrica distingue internamente il percorso senza esporre altri dati.

Non registrare nome famiglia, payload, token, claim completi, issuer, oid, user ID, family ID o SQL parametrizzato con dati personali. Mantenere una sola pipeline Azure Monitor OpenTelemetry e non aggiungere un exporter parallelo.

## 8. Client API e macchina a stati frontend

Estendere `lib/api.ts` con:

- request tipizzata `{ name: string }`;
- response `{ state: "family"; familyId: string }`;
- metodo `createFamily` con `POST` JSON;
- `Content-Type: application/json`;
- token MSAL, correlation ID, `cache: "no-store"` e `credentials: "omit"`;
- parsing dei Problem Details esistente;
- supporto ad `AbortSignal`.

Estendere `KinListAccessGate` mantenendo `/kinlist` come unica route:

| Stato | Comportamento |
|---|---|
| Scelta onboarding | `Crea una famiglia` abilitato; join resta indisponibile fino a FEAT-005 |
| Form creazione | Solo nome famiglia, helper text, Crea e Indietro |
| Invio | Form leggibile, `aria-busy`, nessun secondo submit |
| Validazione | Errore inline associato al campo e nome preservato |
| Errore recuperabile | Nome preservato e azione Riprova |
| Successo created/existing | `familyId` in memoria e passaggio diretto a KinList |
| Offline | Nessuna richiesta, nessuna coda e sola shell pubblica |

Mantenere il nome controllato nello stato del gate o di un parent che sopravviva agli errori recuperabili. Non salvarlo in localStorage, sessionStorage, IndexedDB, Cache API, URL o service worker.

Sul submit usare sia lo stato pending sia un lock sincrono per impedire due eventi prima del rerender React. La mutazione parte soltanto dall'evento esplicito del form, non da un effect soggetto a replay in Strict Mode.

Su logout, cambio account, `401`, `403` o unmount:

- abortire la request in corso;
- ignorare risultati stale;
- rimuovere `familyId` e nome inserito;
- non mostrare dati appartenenti all'account precedente.

Un errore di rete con `navigator.onLine` ancora vero deve restare recuperabile dal form e non diventare uno stato offline senza Riprova.

## 9. Accessibilita, temi e PWA

Creare un componente mirato `CreateFamilyForm` oppure mantenere il form nel gate solo se la responsabilita resta leggibile. Non promuovere un componente specifico della feature nel catalogo UI senza un riuso reale.

Il form deve avere:

- `<label>` persistente collegata all'input;
- helper text con limite 1-100;
- `aria-describedby` stabile;
- `aria-invalid` solo durante un errore del campo;
- errore inline annunciato e focus sul campo invalido;
- focus iniziale sul nome dopo la scelta Crea;
- ripristino focus sul pulsante Crea dopo Indietro;
- stato pending annunciato senza nascondere label o valore;
- submit disabilitato durante la request;
- layout mobile-first, touch target, zoom e contrasto validi.

Dopo il successo spostare il focus sul titolo KinList senza una schermata celebrativa. Se necessario, estendere `PageScaffold` con un riferimento opzionale al titolo e aggiornare esempio, catalogo e skill frontend per la nuova API riusabile.

Verificare light, dark e system, focus visibile, reduced motion e screen reader. La route `/api/**` resta `NetworkOnly`; non sono richieste modifiche Workbox, Background Sync o Static Web Apps se l'endpoint rimane sotto `/api`.

## 10. Test

Aggiungere test Domain per:

- trim e compressione whitespace;
- nome vuoto, oltre 100 e caratteri di controllo;
- Unicode, apostrofi, punteggiatura e maiuscole preservati;
- famiglia con nome, creatore e timestamp validi;
- membership senza ruolo o privilegio.

Aggiungere test Business per:

- creazione valida con `Created`;
- membership gia attiva con `Existing` e stesso `familyId`;
- nome invalido senza chiamata di scrittura;
- profilo inattivo;
- timestamp derivato da `TimeProvider`;
- dipendenza indisponibile distinta da bug applicativo;
- propagazione del `CancellationToken`.

Aggiungere test PostgreSQL reali, preferibilmente con Testcontainers:

- applicazione di tutte le migration su database vuoto;
- preflight che fallisce in presenza di una famiglia legacy;
- colonne, foreign key e indice parziale attesi;
- una request valida produce una famiglia e una membership;
- due DbContext/connessioni separati creano contemporaneamente per lo stesso user;
- esito finale di concorrenza: una famiglia, una membership e zero famiglie orfane;
- retry dopo successo restituisce la stessa famiglia;
- errore controllato prima del commit produce rollback completo;
- membership inattiva non blocca la futura regola di riattivazione ma non autorizza;
- famiglia inattiva non autorizza una membership apparentemente attiva.

Aggiungere test Functions e pipeline per:

- metadata default `ApiAccess` dell'endpoint;
- body valido, assente e JSON malformato;
- `201`, `200`, `400`, `401`, `403`, `500` e `503`;
- correlation ID, media type Problem Details e `no-store, private`;
- assenza di nome e identificativi sensibili in risposte e telemetria;
- parita route, metodo POST, OpenAPI e security;
- cancellazione non convertita in `500`;
- almeno uno smoke test attraverso il worker Functions reale.

Aggiungere test frontend per:

- scelta onboarding con create abilitato e join ancora indisponibile;
- apertura del solo form nome e focus iniziale;
- Indietro e ripristino focus;
- validazione inline e associazioni ARIA;
- nome preservato dopo validazione, rete, `503` o errore tecnico;
- due submit rapidi producono una sola chiamata;
- request POST, body e header corretti;
- `Created` ed `Existing` impostano il family context e aprono KinList;
- cambio account, logout, offline e abort impediscono risultati stale;
- focus sul titolo KinList dopo il successo.

## 11. Documentazione e artefatti

Aggiornare:

- `docs/operations/database-migrations.md` con preflight, verifica e rollback della migration FEAT-002;
- `README.md` e `docs/development/local-development.md` se contengono comandi EF non allineati al design-time factory;
- guide KinList `docs/user-guide/it/kinlist.md` e `docs/user-guide/en/kinlist.md`;
- help contestuale `it` ed `en` per nome, azioni, prerequisiti, errori e limite online;
- testi pagina `it` ed `en` per campo, helper, pending ed errori;
- OpenAPI generata o documentata;
- skill frontend ed esempio soltanto se cambia l'API pubblica di `PageScaffold`;
- change fragment bilingue di tipo `added`;
- patch note e JSON release generati;
- documentazione migration con query di verifica per history, colonne, foreign key, indice e dati orfani.

Non modificare `route-registry.json` se il flusso resta uno stato interno di `/kinlist`. Rigenerare documentazione, registry skill e release notes dalle fonti autorevoli, senza correggere manualmente file generati.

## 12. Verifica finale

Eseguire:

```text
npm run skills:read -- implementation
npm run skills:read -- backend
npm run skills:read -- frontend
npm run skills:read -- documentation
npm run skills:read -- release

dotnet tool restore
dotnet restore KinHub.slnx
dotnet build KinHub.slnx --configuration Release --no-restore
dotnet test KinHub.slnx --configuration Release --no-build
dotnet ef migrations list --project src/backend/infrastructure/DA.KinHub.Infrastructure --configuration Release
dotnet ef migrations script --idempotent --project src/backend/infrastructure/DA.KinHub.Infrastructure --configuration Release --output artifacts/migrations/kinhub-idempotent.sql
dotnet ef migrations bundle --project src/backend/infrastructure/DA.KinHub.Infrastructure --configuration Release --force --output artifacts/migrations/kinhub-migrations
dotnet publish src/backend/applications/DA.KinHub.Functions/DA.KinHub.Functions.csproj -c Release -o artifacts/backend/publish
./scripts/package-backend.ps1 -Environment Development

npm ci --prefix src/frontend
npm run --prefix src/frontend test
npm run --prefix src/frontend lint
npm run --prefix src/frontend typecheck
npm run --prefix src/frontend i18n:validate
npm run --prefix src/frontend routes:validate
npm run --prefix src/frontend build

npm run docs:sync
npm run docs:validate
npm run release:generate
npm run release:validate
npm run skills:build
npm run skills:validate
az bicep build --file infra/app.bicep
az bicep build-params --file infra/main.dev.bicepparam
```

Completare il flusso manuale autenticato:

1. Login di un utente senza famiglia.
2. Scelta Crea famiglia e ritorno con Indietro.
3. Nome invalido con errore inline e input preservato.
4. Guasto recuperabile con input preservato e Riprova.
5. Creazione valida e ingresso diretto in KinList.
6. Refresh con bootstrap nello stato `family`.
7. Retry della stessa creazione con risposta `200` e stesso `familyId`.
8. Due invii concorrenti senza seconda famiglia o record orfani.
9. Cambio account, perdita rete e riconnessione senza leak di stato.
10. Verifica Chrome desktop, Chrome Android/PWA ed Edge con tastiera, zoom, temi e screen reader.

In un ambiente distribuito verificare inoltre migration history, schema effettivo, `/health/live`, `/health/ready`, `/api/version`, flusso autenticato e ingestione di trace/metriche senza nome famiglia o identificativi sensibili.

## Sequenza di rilascio

1. Verificare FEAT-001 e il preflight di assenza famiglie legacy.
2. Implementare value object, invarianti e contratti Business.
3. Implementare repository atomico e test PostgreSQL reali.
4. Generare e verificare la migration additiva e il bundle.
5. Implementare endpoint, OpenAPI, errori e telemetria.
6. Implementare form, macchina a stati, accessibilita e test frontend.
7. Aggiornare guide, help, migration runbook e artefatti generati.
8. Eseguire build, test, lint, packaging e validator completi.
9. Portare la feature da `In progress` a `In review`, senza contrassegnarla autonomamente `Completed`.
10. Creare commit su `dev`, push e pull request verso `main`; attendere tutte le GitHub Actions verdi senza eseguire merge.

Il rollback applicativo usa lo ZIP backend e gli asset frontend N-1. Prima di qualsiasi creazione il rollback schema puo usare il `Down` verificato su database disposable; dopo scritture FEAT-002 si usa una migration correttiva compatibile con i dati, preceduta da backup e verifica esplicita.
