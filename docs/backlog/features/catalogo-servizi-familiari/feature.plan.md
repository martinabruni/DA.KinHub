# Piano di implementazione - FEAT-015

## Obiettivo

Introdurre il catalogo persistito dei KinService, inizialmente con KinList, assegnare automaticamente la disponibilità a tutte le famiglie esistenti e nuove e rendere la Home dipendente dai servizi autorizzati della famiglia. L'accesso a `/kinlist` deve verificare server-side sessione, membership attiva, famiglia attiva e disponibilità del servizio, senza cache locale di dati familiari, UI amministrativa o nuovi ruoli.

## Contratti esecutivi

- Contratto CP-001: la chiave tecnica di KinList è `kinlist` e la route persistita è `/kinlist`.
- Schema shared: `shared.kin_services`, `shared.kin_service_localizations` e `shared.family_kin_service_availabilities`.
- `KinService`: chiave e route univoche, stato globale attivo/inattivo e flag `IsPreconfigured`; KinList è preconfigurato e attivo.
- Localizzazioni: una riga per coppia servizio-lingua, con entrambe le lingue `it` ed `en` per KinList.
- Disponibilità: una sola riga per coppia famiglia-servizio, con stato attivo/inattivo; la chiave primaria o un vincolo univoco impedisce duplicati.
- Catalogo: `GET /api/kinhub/services?familyId=<uuid>&language=<it|en>`, protetto da `Family`, con risposta `200` `{ "services": [...] }`.
- Voce catalogo: `{ "key": "kinlist", "route": "/kinlist", "name": "...", "description": "..." }`; la risposta contiene soltanto servizi globalmente attivi e disponibili per la famiglia autorizzata.
- Lingua: `it` è il default; valori mancanti o non supportati usano `en` come fallback tecnico. Il fallback avviene server-side.
- Verifica accesso: `GET /api/kinhub/services/{serviceKey}/access?familyId=<uuid>`, protetto da `Family`; `204` se consentito, `403 service.accessDenied` per servizio sconosciuto, inattivo o non disponibile.
- `familyId` è un parametro di contratto per gli endpoint, ma il server verifica che coincida con il contesto autorizzato; l'identità autorevole non arriva dal client.
- Gli endpoint non espongono dettagli su altre famiglie, servizi inattivi o motivi specifici del rifiuto.
- Risposte autenticate ed errori: `Cache-Control: no-store, private`, correlation ID e Problem Details centralizzati.
- Telemetria: operation e outcome a cardinalità finita, senza token, claim completi, `familyId`, service key dinamiche, lingua, testi localizzati o payload.
- Creazione famiglia: famiglia, membership e disponibilità dei servizi preconfigurati attivi sono un unico commit transazionale.
- Migration: seed e backfill sono idempotenti; il `Down` è usabile su database disposable o prima di scritture dipendenti, mentre dopo il rilascio dei dati il rollback operativo usa una migration correttiva compatibile.
- Frontend: `/` mostra solo login per visitatori, onboarding senza famiglia e servizi autorizzati per membri; Release Notes resta fuori dal catalogo e nelle superfici informative esistenti.
- KinList: il contenuto non viene montato prima della risposta `204` del controllo accesso; il catalogo della Home non è una prova di autorizzazione.
- Nessuna disponibilità, localizzazione o risposta autenticata viene salvata in `localStorage`, `sessionStorage`, IndexedDB, Cache API o service worker.

## 1. Verificare prerequisiti e congelare CP-001

Confermare nel codice corrente l'integrazione di FEAT-002 e FEAT-014:

- profilo applicativo, bootstrap e stato `onboarding`/`family`;
- transazione e lock già usati da `FamilyRepository.CreateWithCreatorAsync`;
- policy `ApiAccess` e marker `[RequiresFamilyAccess]`;
- `KinHubAuthorizationFeature` con `familyId` verificato dalla pipeline;
- `KinHubDbContext` e migration shared esistenti;
- `PageScaffold`, `StatePanel`, `KinServiceGrid`, `KinServiceCard` e shell con login globale;
- `KinListAccessGate` e contesto famiglia in memoria;
- endpoint `/api/kinhub/family-context` e contratto di autorizzazione corrente.

Prima di modificare migration shared, Home, client API o gate KinList, registrare nel change set il checkpoint CP-001 con:

- nomi delle tre tabelle e vincoli;
- chiave `kinlist`, route `/kinlist` e flag di preconfigurazione;
- query `language` e fallback `en`;
- forma delle risposte catalogo e accesso;
- codici Problem Details `service.accessDenied` e dipendenza PostgreSQL;
- responsabilità distinta fra policy `Family`, catalogo e guard KinService;
- ownership delle migration shared rispetto a FEAT-003 e FEAT-004.

Non introdurre una nuova policy `KinService` in questa feature: `Family` verifica il perimetro familiare, mentre il caso d'uso applicativo verifica la disponibilità del servizio. Se FEAT-003 richiede un guard riusabile per più endpoint, congelare prima il suo confine e applicarlo senza duplicare controlli.

## 2. Modello e invarianti di dominio

Creare il modello shared nel namespace di dominio coerente con le convenzioni esistenti, senza dipendenze da EF Core, Azure o HTTP.

Il modello deve comprendere:

- `KinService` con identificativo, key, route, stato generale, preconfigurazione e timestamp;
- `KinServiceLocalization` con servizio, lingua, nome e descrizione;
- `FamilyKinServiceAvailability` con famiglia, servizio, stato e timestamp.

Applicare queste invarianti:

1. key e route non sono vuote e sono stabili per il servizio;
2. la lingua è normalizzata a codice minuscolo e appartiene alle lingue supportate dal contratto;
3. nome e descrizione non sono vuoti;
4. una disponibilità identifica una sola coppia famiglia-servizio;
5. lo stato globale e quello familiare restano concetti distinti;
6. l'inattivazione impedisce l'accesso ordinario senza cancellare la storia necessaria;
7. nessuna entità introduce ruolo, amministratore o privilegio per il creatore della famiglia.

Aggiungere contratti di repository mirati, con `CancellationToken`, per:

- elencare i servizi attivi e disponibili per una famiglia con localizzazione e fallback;
- verificare se un servizio è attivo globalmente e disponibile per una famiglia;
- ottenere i servizi preconfigurati attivi necessari alla creazione di una nuova famiglia.

Non introdurre generic repository, `GetAll`, mediator, CQRS o un current user ambientale.

## 3. Persistenza shared e migration

Aggiornare `KinHubDbContext` e le configurazioni EF per le tre tabelle nello schema `shared`.

Vincoli e indici minimi:

| Tabella | Vincoli principali |
|---|---|
| `kin_services` | key univoca, route univoca, stato e preconfigurazione persistiti |
| `kin_service_localizations` | FK al servizio, key univoca `(kin_service_id, language)`, nome e descrizione obbligatori |
| `family_kin_service_availabilities` | FK a famiglia e servizio, key univoca `(family_id, kin_service_id)`, stato persistito |

Usare `DeleteBehavior.Restrict` per impedire cancellazioni accidentali di servizi usati da famiglie. Aggiungere gli indici necessari alle query per famiglia e servizio senza introdurre indici speculativi.

Generare una migration additiva dal progetto Infrastructure e dalla design-time factory autorevole. La migration deve:

1. creare le tre tabelle e i vincoli;
2. inserire KinList con identificativo deterministico, key `kinlist`, route `/kinlist`, preconfigurato e attivo;
3. inserire le localizzazioni `it` ed `en` in modo idempotente;
4. assegnare KinList attivo a ogni famiglia attiva esistente senza duplicati;
5. usare conflitti idempotenti per seed, localizzazioni e disponibilità;
6. lasciare invariato lo schema `kinlist` e le migration delle feature concorrenti;
7. generare designer e model snapshot tramite EF, senza correzioni manuali;
8. rimuovere nel `Down` prima disponibilità, poi localizzazioni e infine catalogo.

Documentare nel runbook:

- preflight di migration history, schema, vincoli e permessi;
- query di conteggio di servizi, localizzazioni e disponibilità;
- verifica di una disponibilità per ogni famiglia attiva;
- verifica che non esistano duplicati o record orfani;
- rollback su database disposable;
- procedura correttiva dopo scritture reali, con backup/PITR e nessun `Down` distruttivo automatico.

Gestire la finestra fra migration e deploy con backfill idempotente e riconciliazione post-deploy documentata. Se l'ambiente richiede coerenza assoluta durante il rollout, il rilascio deve prevedere una breve sospensione della creazione famiglia oppure una seconda migration correttiva; non affidarsi al solo backfill iniziale.

## 4. Repository e creazione atomica della famiglia

Estendere il contratto di creazione famiglia affinché la disponibilità venga assegnata nello stesso confine transazionale già esistente.

Nel percorso `CreateWithCreatorAsync` o nel contratto equivalente:

1. avviare l'operazione nella execution strategy EF esistente;
2. aprire la transazione PostgreSQL;
3. acquisire il lock `FOR UPDATE` sul profilo chiamante;
4. rileggere la membership e la famiglia attive;
5. se il contesto esiste, restituire `Existing` senza duplicare dati;
6. leggere dentro la transazione i servizi `IsPreconfigured` attivi;
7. aggiungere famiglia, membership e disponibilità candidate allo stesso `DbContext`;
8. eseguire un solo `SaveChangesAsync` e commit;
9. su errore, eseguire rollback e pulire il change tracker quando serve;
10. su conflitto univoco riconciliabile, rileggere il contesto autorevole e restituire `Existing`.

Il risultato `Created` è valido soltanto se famiglia, membership e tutte le disponibilità preconfigurate richieste sono state committate. Un retry o una richiesta concorrente non deve creare una seconda famiglia, una seconda disponibilità o una famiglia senza KinList.

Aggiornare i test di `FamilyRepository` e `FamilyCreationService` per verificare anche conteggi e atomicità delle disponibilità. Non risolvere la coerenza con una seconda chiamata dopo il commit.

## 5. Business e gestione degli esiti

Implementare servizi scoped per:

- lettura del catalogo attivo per la famiglia;
- verifica dell'accesso a un servizio.

Il servizio catalogo deve:

1. ricevere `familyId`, lingua richiesta e `CancellationToken`;
2. normalizzare la lingua con `it` default e `en` fallback;
3. delegare al repository una query già limitata a famiglia, disponibilità attiva e servizio globale attivo;
4. proiettare soltanto key, route, nome e descrizione;
5. restituire una lista vuota quando nessun servizio è attivo, senza confonderla con accesso negato;
6. tradurre guasti di persistenza nel codice tecnico standard.

Il servizio accesso deve:

1. ricevere `familyId`, key del servizio e `CancellationToken`;
2. verificare disponibilità familiare e stato globale;
3. restituire successo senza payload quando il servizio è disponibile;
4. usare `service.accessDenied` per key inesistente, servizio inattivo o disponibilità mancante;
5. non distinguere questi casi nella risposta pubblica;
6. propagare cancellazioni e bug senza trasformarli in `500` impropri.

Registrare repository e servizi nella DI come scoped. Aggiungere codici e mapping nell'area Business esistente, riusando Problem Details e middleware centralizzati.

## 6. Endpoint, pipeline e OpenAPI

Aggiornare `Http/ApiRoutes.cs` con le route del catalogo e della verifica accesso. Creare Function sottili con `AuthorizationLevel.Anonymous`, senza replicare autenticazione, correlation ID, cache header o mapping delle eccezioni.

Endpoint catalogo:

- applicare `[RequiresFamilyAccess]`;
- leggere il contesto autorizzato dalla feature HTTP tipizzata;
- validare `familyId` e `language` secondo il contratto;
- restituire `200` anche con `services: []`;
- documentare `400`, `401`, `403`, `500` e `503` applicabili.

Endpoint accesso:

- applicare `[RequiresFamilyAccess]`;
- validare `serviceKey` e `familyId`;
- restituire `204` senza body quando autorizzato;
- restituire `403 service.accessDenied` per ogni rifiuto funzionale;
- non restituire catalogo, nome servizio o motivazione del rifiuto.

Estendere `OpenApiDocumentProvider` con parametri, security bearer, response body del catalogo, `204`, Problem Details e media type. Estendere i test di parità affinché controllino route, verbo, security e contratti dei nuovi endpoint.

## 7. Telemetria redatta

Aggiungere operation name condivisi per catalogo e verifica accesso. Ogni operation deve chiudersi con esiti finiti e misurare la durata complessiva.

Outcome ammessi:

- catalogo: `success`, `empty`, `denied`, `dependency_failure`, `technical_failure`;
- accesso: `granted`, `denied`, `dependency_failure`, `technical_failure`.

Registrare separatamente i segnali di autenticazione e autorizzazione già gestiti dal middleware, senza duplicare logica trasversale negli endpoint. Non registrare service key dinamiche, lingua, testi, `familyId`, token, claim, payload o SQL con valori sensibili.

Testare che metriche, trace e log contengano soltanto operation, outcome, categoria errore e durata.

## 8. Client API e bootstrap frontend condiviso

Estendere `src/frontend/src/lib/api.ts` con:

- tipi per catalogo e voce servizio;
- metodo `getFamilyServices(familyId, language, signal)`;
- metodo `checkServiceAccess(serviceKey, familyId, signal)`;
- query string codificata correttamente;
- token MSAL, correlation ID, `cache: "no-store"` e `credentials: "omit"`;
- supporto a `204 No Content` senza chiamare `response.json()`;
- propagazione di `AbortSignal`, Problem Details e correlation ID.

Estrarre dal gate soltanto il comportamento comune di bootstrap e onboarding, con un hook/controller o componente condiviso che:

- osservi lo stato MSAL e l'account attivo;
- non chiami API per visitatori o browser offline;
- risolva `onboarding` e `family` tramite bootstrap;
- mantenga `familyId` solo in memoria;
- esponga retry e creazione famiglia;
- annulli richieste obsolete;
- cancelli contesto e stato visibile su logout, cambio account, `401`, `403`, offline o unmount.

Non caricare automaticamente il catalogo nel provider globale: le route informative e pubbliche non devono effettuare richieste familiari.

## 9. Home dinamica

Aggiornare `HomePage.tsx` mantenendo `PageScaffold` e riusando `StatePanel`, `KinServiceGrid` e `KinServiceCard` esistenti.

La macchina a stati deve rispettare questa tabella:

| Stato | Comportamento |
|---|---|
| MSAL in inizializzazione | Loading accessibile, senza card o login duplicato |
| Visitatore | Messaggio localizzato e sola azione `Accedi` della shell globale |
| Autenticato senza famiglia | Onboarding e creazione famiglia, senza servizi |
| Bootstrap/catalogo in corso | `StatePanel` busy e nessun dato stale |
| Servizi disponibili | Card generate esclusivamente dalla risposta API |
| Catalogo vuoto | Stato empty distinto da onboarding e accesso negato |
| Offline | Stato offline, senza dati familiari conservati o nuove chiamate |
| Sessione scaduta | Stato auth dedicato e nessuna card |
| Accesso negato | Stato `403` distinto dal catalogo vuoto e senza dettagli sensibili |
| Errore recuperabile | Stato errore con `Riprova`; nessun dato precedente |

Rimuovere dalla Home le card hardcoded KinList e Release Notes. Conservare Release Notes nella route, nel menu Informazioni e nella notifica di aggiornamento esistenti.

Al cambio lingua, account, famiglia o stato di rete:

- abortire la richiesta corrente;
- rimuovere subito le card precedenti;
- effettuare una nuova lettura network-only nella lingua selezionata;
- impedire che una risposta precedente ripopoli la Home.

Verificare che `StatePanel` usi heading level coerente con il titolo della pagina, ruoli live e `aria-busy`, senza nuovo CSS se le primitive e gli stili esistenti sono sufficienti.

## 10. Guard diretto di KinList

Aggiornare `KinListAccessGate.tsx` e `KinListPage.tsx` mantenendo `/kinlist` come unica route.

Flusso obbligatorio:

1. `ProtectedRoute` ferma il visitatore prima del rendering della pagina protetta;
2. il gate risolve il bootstrap;
3. senza famiglia mostra onboarding condiviso;
4. con famiglia chiama `checkServiceAccess("kinlist", familyId)`;
5. durante bootstrap o verifica non monta il contenuto KinList;
6. dopo `204` monta `children` o il confine concordato con FEAT-003;
7. dopo `403` mostra accesso negato e non dati KinList;
8. offline o errore tecnico mostrano il rispettivo stato senza dati precedentemente autorizzati.

Il gate non deve dedurre l'autorizzazione dalla presenza della card Home, dal solo `familyId` o dal risultato del bootstrap. Le future API di FEAT-003 devono applicare nuovamente il controllo server-side del servizio prima dei dati KinList.

## 11. Test

Aggiungere test Domain/Business per:

- invarianti di key, route, lingua, localizzazione e disponibilità;
- servizio globale inattivo distinto dalla disponibilità familiare inattiva;
- fallback `it`/`en` e lingua non supportata;
- catalogo filtrato per famiglia e senza servizi inattivi;
- servizio sconosciuto, inattivo o non disponibile con lo stesso rifiuto applicativo;
- creazione nuova famiglia con disponibilità KinList nello stesso esito;
- retry e concorrenza senza duplicati o record parziali;
- propagazione di `CancellationToken` e distinzione dei guasti PostgreSQL.

Aggiungere test PostgreSQL reali, preferibilmente con Testcontainers, per:

- applicazione della migration su database con famiglie esistenti;
- seed singolo di KinList e due localizzazioni;
- backfill di tutte le famiglie attive senza duplicati;
- vincoli, foreign key, indici e assenza di record orfani;
- nuova famiglia con famiglia, membership e disponibilità nello stesso commit;
- retry e richieste concorrenti;
- rollback prima del commit;
- isolamento fra due famiglie e disponibilità inattiva.

Aggiungere test Functions/pipeline/OpenAPI per:

- default `ApiAccess` e policy `Family`;
- catalogo `200`, lista vuota e query lingua;
- accesso `204` e `403 service.accessDenied` senza enumerazione;
- `401`, `400`, `500`, `503`, Problem Details e `no-store, private`;
- correlazione, media type, route/verbo/security e risposta `204` senza body;
- cancellazione non convertita in `500`;
- telemetria priva di identificativi e testi localizzati.

Aggiungere test frontend per:

- visitatore senza card e senza secondo pulsante login;
- onboarding autenticato senza famiglia;
- creazione famiglia seguita da rilettura catalogo;
- catalogo italiano, inglese e fallback server-side;
- card esclusivamente API e assenza Release Notes dalla Home;
- loading, empty, offline, errore, `401` e `403` distinti;
- `Riprova` soltanto nello stato recuperabile;
- cambio lingua/account e risposte stale;
- gate che non monta KinList prima del `204`;
- accesso negato, offline, abort e logout senza dati residui;
- tastiera, focus, `aria-live`, tema chiaro/scuro, mobile e reduced motion.

## 12. Documentazione e artefatti

Aggiornare:

- `docs/operations/database-migrations.md` con preflight, seed, backfill, verifica e rollback FEAT-015;
- `docs/user-guide/it/getting-started.md` e `docs/user-guide/en/getting-started.md` con Home, onboarding e catalogo;
- `docs/user-guide/it/kinlist.md` e `docs/user-guide/en/kinlist.md` con accesso diretto e disponibilità;
- help Home e KinList in `it` ed `en` con scopo, prerequisiti, stati, azioni e limiti;
- testi `pages` e `help` per ogni stato visibile, senza stringhe hardcoded;
- OpenAPI e contratti endpoint;
- change fragment bilingue;
- patch note e JSON release generati dalle fonti autorevoli.

Non modificare `route-registry.json` se le route esistenti restano `/` e `/kinlist` e non aggiungere una route amministrativa. Non modificare manualmente gli artefatti generati da docs-sync o release-notes.

## 13. Verifica finale

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
npm run --prefix src/frontend design-system:validate
npm run --prefix src/frontend build

npm run docs:sync
npm run docs:validate
npm run release:generate
npm run release:validate
npm run skills:build
npm run skills:validate

az bicep build --file infra/main.bicep
az bicep build-params --file infra/environments/dev.bicepparam
```

Completare il flusso manuale autenticato:

1. Aprire la Home da visitatore e verificare l'assenza di servizi e del login duplicato.
2. Accedere con un utente senza famiglia e verificare onboarding e creazione.
3. Creare la famiglia e verificare disponibilità KinList e card localizzata.
4. Cambiare lingua e verificare il fallback inglese quando applicabile.
5. Aprire `/kinlist` direttamente con sessione valida e verificare il controllo prima del contenuto.
6. Provare sessione assente, famiglia assente, membership inattiva e disponibilità negata.
7. Provare offline, refresh, errore recuperabile e risposte lente senza dati stale.
8. Verificare desktop, mobile/PWA, tastiera, zoom, temi e screen reader.

In un ambiente distribuito verificare inoltre migration history, schema effettivo, `/health/live`, `/health/ready`, `/api/version`, endpoint autenticati, header di cache e ingestione di trace/metriche senza identificativi o testi sensibili.

## Sequenza di rilascio

1. Confermare FEAT-002/FEAT-014 e congelare CP-001 con FEAT-003 prima di modificare i contratti condivisi.
2. Implementare entità, invarianti e contratti del catalogo nel Domain.
3. Implementare configurazioni EF, repository e test PostgreSQL.
4. Generare migration, seed, backfill, script e bundle; verificare rollback.
5. Estendere la transazione di creazione famiglia e i test di retry/concorrenza.
6. Implementare servizi Business, codici errore, DI e telemetria.
7. Implementare endpoint, pipeline, OpenAPI e test di parità.
8. Estendere client API e controller bootstrap condiviso.
9. Implementare Home dinamica, onboarding condiviso e guard KinList.
10. Aggiornare i18n, help, guide, migration runbook, change fragment e artefatti generati.
11. Eseguire test, build, lint, typecheck, packaging e validator completi.
12. Eseguire smoke test autenticati e verifiche operative applicabili.
13. Portare la feature da `In progress` a `In review` solo quando la Definition of Done è verificata; non impostare autonomamente `Completed`.
14. Creare commit su `dev`, push e pull request verso `main`; attendere tutte le GitHub Actions verdi senza eseguire merge.

Il rollback applicativo usa lo ZIP backend e gli asset frontend N-1. Il rollback dello schema usa il `Down` soltanto su database disposable o prima di scritture dipendenti; dopo la creazione di famiglie o disponibilità reali si usa una migration correttiva compatibile, preceduta da backup/PITR e verifica esplicita dei dati.
