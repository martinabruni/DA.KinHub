# Piano di implementazione - FEAT-003

## Obiettivo

Consentire a un membro autorizzato di consultare pagine limitate di soli item KinList `Active` visibili nella propria famiglia, ordinate stabilmente per gruppo e posizione originaria, con categorie essenziali, autore, navigazione keyset avanti/indietro e stati UI distinti.

La slice introduce il modello persistente minimo KinList e il contratto CP-002 riusabile dalle feature successive, senza aggiungere creazione manuale, microfono, filtro, drawer o completamento.

## Contratti esecutivi

- FEAT-002, FEAT-014 e FEAT-015 sono prerequisiti integrati; CP-001 resta quello definito da FEAT-015.
- Endpoint: `GET /api/kinlist/items?familyId=<uuid>&pageSize=<positive>&cursor=<opaque?>`.
- L'endpoint usa `AuthorizationLevel.Anonymous`, autenticazione `ApiAccess`, marker `[RequiresFamilyAccess]` e policy esattamente `Family`.
- Il caso d'uso verifica nuovamente che il KinService `kinlist` sia disponibile per la famiglia prima di leggere dati.
- L'identita interna verificata dalla policy viene propagata nella feature HTTP tipizzata e passata esplicitamente al Business e al repository.
- La query applica famiglia, stato `Active`, assenza di inattivazione e visibility prima di ordine, pagina, categorie e proiezione.
- Predicato autorevole: `Shared` della famiglia oppure `Personal` con owner uguale all'utente applicativo corrente.
- Ordine totale `active-items-v1`: `GroupCreatedAt DESC`, `GroupId DESC`, `PositionInGroup ASC`, `ItemId ASC`.
- `UpdatedAt`, nome, categorie, versione e ultimo autore non partecipano all'ordine.
- Pagina client iniziale: 50 item, senza selettore di dimensione in FEAT-003.
- Limite server: `effectivePageSize = min(pageSize, configuredReadMax)`.
- `configuredReadMax` iniziale e ceiling assoluto: 5000; valori non positivi o superiori impediscono l'avvio.
- Il repository legge al massimo `effectivePageSize + 1` record e non espone `GetAll`, offset o numero pagina.
- I cursori sono opachi, protetti, associati a collezione, famiglia, utente, stato, ordine, direzione e dimensione effettiva.
- Durata cursore `active-items-v1`: 30 minuti.
- Le chiavi Data Protection sono persistite nello Storage applicativo esistente, non nel filesystem effimero dell'istanza.
- Nessun cursore contiene dati leggibili, nomi, categorie, claim o identificativi esterni.
- Un cursore alterato, scaduto, cross-family, cross-user, con versione, ordine, filtro o dimensione incompatibili restituisce `400 pagination.cursorInvalid` senza item.
- Una pagina vuota con cursore in ingresso e trattata come cursore stale e permette di ripartire dalla prima pagina; soltanto la prima pagina vuota produce lo stato empty reale.
- La risposta non contiene `totalCount`, numero pagina, chiavi d'ordine, owner ID o `familyId`.
- La proiezione riga espone nome, massimo tre categorie, numero di categorie ulteriori, autore e versione concorrente opaca.
- Finche il profilo non possiede un display name, l'API restituisce `displayName: null`; la UI usa `Membro`/`Member` come nome accessibile e `?` come contenuto dell'avatar.
- Il refresh manuale riparte sempre dalla prima pagina.
- Durante navigazione o refresh la pagina valida corrente resta leggibile; `401`, `403`, offline, cambio account o famiglia cancellano immediatamente item e cursori.
- Nessun item, pagina o cursore viene salvato in `localStorage`, `sessionStorage`, IndexedDB, Cache API o service worker.
- Nessun microfono, filtro categoria, checkbox, selezione, drawer, completamento o comando di creazione viene anticipato.

### Contratto CP-002

Richiesta:

```http
GET /api/kinlist/items?familyId=<uuid>&pageSize=50
GET /api/kinlist/items?familyId=<uuid>&pageSize=50&cursor=<opaque>
```

Risposta:

```json
{
  "items": [
    {
      "id": "uuid",
      "name": "Latte",
      "categories": [{ "id": "uuid", "name": "Spesa" }],
      "remainingCategoryCount": 0,
      "author": { "displayName": null },
      "version": "1"
    }
  ],
  "effectivePageSize": 50,
  "maxPageSize": 5000,
  "previousCursor": null,
  "nextCursor": "opaque"
}
```

Errori applicabili:

| HTTP | Codice | Uso |
|---|---|---|
| `400` | `pagination.pageSizeInvalid` | Dimensione assente, malformata o non positiva |
| `400` | `pagination.cursorInvalid` | Cursore alterato, scaduto, stale o incompatibile |
| `401` | codici auth esistenti | Sessione o token non validi |
| `403` | `family.accessDenied` | Famiglia o membership non autorizzata |
| `403` | `service.accessDenied` | KinList non disponibile per la famiglia |
| `503` | `dependency.postgresqlUnavailable` | PostgreSQL non disponibile |
| `503` | `dependency.storageUnavailable` | Key ring Data Protection non disponibile |
| `500` | `internal.unexpected` | Errore inatteso |

## 1. Prerequisiti e checkpoint

Confermare nella baseline:

- bootstrap e contesto famiglia esclusivamente in memoria;
- famiglia e membership attive create da FEAT-002;
- catalogo e disponibilita KinList introdotti da FEAT-015;
- `KinHubAuthorizationFeature` popolata soltanto dopo autenticazione e policy applicabili;
- `KinListAccessGate` che non monta il contenuto prima del `204` del controllo servizio;
- schema `shared` e migration FEAT-001/002/015;
- design system FEAT-014, `PageScaffold`, `StatePanel`, `Avatar`, `Pagination` e `KinListItem`;
- API autenticate `NetworkOnly` nella PWA;
- Storage applicativo e managed identity gia disponibili.

Registrare CP-002 come contratto autorevole per forma pagina e cursori, codici Problem Details, predicato di visibilita, ordine `active-items-v1`, convenzioni del client API, opzioni di lettura e comportamento UI per cursore stale.

TECH-003 e chiuso per `active-items` soltanto dopo test del codec, query PostgreSQL avanti/indietro, indici e piani rappresentativi. Categorie, timeline e collezioni di manutenzione definiranno ordini e purpose distinti nelle rispettive feature.

## 2. Contesto Family con identita verificata

Estendere il risultato di `IFamilyAccessService` affinche restituisca un risultato tipizzato contenente `FamilyAccessOutcome` e `ApplicationUserId` soltanto per l'esito `Granted`.

Aggiornare `FamilyAuthorizationResource` e `KinHubAuthorizationFeature` per conservare l'ID applicativo ottenuto dall'handler. Aggiungere `RequireApplicationUserId()` accanto a `RequireFamilyId()`.

La Function lista legge gli ID dalla feature e li passa esplicitamente al caso d'uso. Business e Domain non accedono a `HttpContext`, `IHttpContextAccessor`, `AsyncLocal` o servizi current-user.

Questa estensione evita un secondo lookup `(iss, oid)` per ogni pagina e non modifica la policy, la query `familyId` o il comportamento pubblico degli endpoint esistenti.

## 3. Modello e invarianti Domain

Creare l'area `KinList` nel progetto Domain con:

- `RegistrationGroup`;
- `KinListItem`;
- `KinListCategory`;
- `KinListItemCategory`;
- `ItemVisibility`;
- `ItemStatus`;
- value object per nome item e nome categoria;
- contratto repository paginato mirato.

`ItemVisibility` contiene `Shared` e `Personal`. `ItemStatus` contiene almeno `Active` e `Completed`.

`RegistrationGroup` conserva ID, famiglia, `RecordingId`, creatore applicativo, `CreatedAt` e `InactiveAt` opzionale.

`KinListItem` conserva ID, famiglia, gruppo, nome, posizione originaria, owner applicativo immutabile, visibility, stato, timestamp, metadati di modifica/completamento, `InactiveAt` e revisione concorrente.

La factory `CreateShared` deve richiedere famiglia, gruppo, nome, posizione, owner e timestamp server-side, assegnare sempre `Shared`, stato `Active` e revisione iniziale `1`, rifiutando GUID vuoti e posizione negativa.

Owner, gruppo, famiglia, posizione e momento di creazione restano immutabili. Non introdurre metodi pubblici di modifica, completamento, undo o conversione visibility.

Le categorie usano Unicode NFKC, trim, compressione whitespace e case folding invariant per `normalized_name`, preservando gli accenti e il testo visuale originale.

Non introdurre timeline, command ID, eventi, provider AI o salvataggio vocale in questa slice.

## 4. Schema `kinlist` e migration

Aggiungere al `KinHubDbContext` le nuove entita e configurazioni EF separate.

### `registration_groups`

- UUID PK;
- `family_id` FK verso `shared.families`;
- `recording_id` obbligatorio;
- creator FK verso `shared.application_users`;
- timestamp UTC e `inactive_at`;
- unique `(family_id, recording_id)`;
- alternate key `(Id, family_id)` per FK composite.

### `items`

- UUID PK e `family_id` obbligatorio;
- FK composite verso `registration_groups`;
- nome non vuoto;
- `position_in_group >= 0`;
- owner FK obbligatorio;
- enum visibility e stato chiusi;
- timestamp di creazione, modifica e completamento;
- `inactive_at`;
- `revision >= 1` come concurrency token;
- unique `(registration_group_id, position_in_group)`;
- alternate key `(Id, family_id)`;
- check per posizione, revisione, visibility e stato.

### `categories` e `item_categories`

`categories` conserva famiglia, nome, `normalized_name`, autore, timestamp e inattivazione. Usare unique parziale `(family_id, normalized_name)` per categorie attive.

`item_categories` conserva `family_id`, `item_id` e `category_id`, con PK `(item_id, category_id)` e FK composite per impedire associazioni cross-family.

Usare `DeleteBehavior.Restrict` per le relazioni verso shared e per i collegamenti KinList.

Indici candidati da verificare con dataset reale:

```text
registration_groups (family_id, created_at DESC, Id DESC)
items Shared attivi (registration_group_id, position_in_group ASC, Id ASC)
items Personal attivi (registration_group_id, owner_application_user_id, position_in_group ASC, Id ASC)
item_categories (family_id, item_id)
item_categories (family_id, category_id, item_id)
```

Gli indici definitivi devono essere confermati con `ANALYZE` ed `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)`. Non fissare test fragili su uno specifico nodo del planner.

Generare migration, designer e snapshot tramite EF. La migration crea lo schema `kinlist`, non modifica seed o tabelle `shared`, non inserisce dati dimostrativi e rimuove nel `Down` le tabelle in ordine inverso.

Il rollback `Down` e valido su database disposable o prima di scritture KinList. Dopo dati reali usare una migration correttiva preceduta da backup/PITR.

## 5. Opzioni e cursori

Introdurre opzioni tipizzate per:

```text
Pagination:ReadMax = 5000
```

Il validator deve impedire l'avvio con valori minori o uguali a zero o superiori al ceiling 5000. Aggiornare `appsettings`, `local.settings.json.example`, Bicep e output generati.

Definire un codec specifico `active-items`, senza un framework universale per ogni repository. Il payload logico protetto contiene:

```text
formatVersion
collection = active-items
orderVersion = active-items-v1
direction = previous | next
effectivePageSize
groupCreatedAt
groupId
positionInGroup
itemId
expiresAt
```

Famiglia, utente applicativo, stato `Active` e filtro senza categoria sono associati al purpose Data Protection della richiesta.

Usare ASP.NET Core Data Protection con application name stabile `KinHub`, purpose versionato, payload Base64Url, `TimeProvider`, durata 30 minuti e key ring persistito nello Storage applicativo esistente in un path tecnico separato.

Usare Azurite in Development e managed identity tramite `Storage:AccountUri` in Azure. Non usare filesystem locale, chiavi ephemeral, segreti nel frontend, Redis o stato cursore nel database.

Il codec traduce alterazione, scadenza, scope errato e formato non supportato in `pagination.cursorInvalid`; guasti reali dello Storage restano distinti come dipendenza indisponibile.

## 6. Repository keyset

Creare un repository read-only specifico per la pagina degli item attivi. Il contratto richiede sempre famiglia, utente applicativo, direzione, anchor opzionale, dimensione effettiva positiva e `CancellationToken`.

Non aggiungere overload senza pagina.

Centralizzare un'espressione equivalente a:

```text
FamilyId == familyId
AND InactiveAt IS NULL
AND Status == Active
AND (
  Visibility == Shared
  OR (Visibility == Personal AND OwnerApplicationUserId == applicationUserId)
)
```

Per avanti usare l'ordine canonico e il confronto lessicografico. Per indietro invertire confronto e ordine, leggere `effectivePageSize + 1`, rimuovere la sentinella, invertire in memoria soltanto la pagina e restituirla nell'ordine visuale canonico.

Caricare le categorie con una query limitata agli ID della pagina, evitando N+1 e join che alterino il limite. Restituire massimo tre categorie per riga più `remainingCategoryCount`.

La prima pagina non ha previous; la sentinella determina next. Una pagina vuota con cursore in ingresso e stale, non empty reale. Cancellare o completare l'anchor non invalida automaticamente un cursore autosufficiente.

## 7. Business e API

Creare un servizio scoped:

```text
GetActiveItemsPageAsync(
  applicationUserId,
  familyId,
  requestedPageSize,
  opaqueCursor,
  cancellationToken)
```

Il servizio verifica disponibilita `kinlist`, page size positivo, clamp al limite configurato, codec prima della query, repository scoped, cursori di risposta e DTO minimo. Traduce soltanto guasti attesi di PostgreSQL/Storage e propaga cancellazioni e bug applicativi.

Aggiungere la route autorevole `api/kinlist/items` e una Function sottile con `[RequiresFamilyAccess]`, trigger `GET`, `AuthorizationLevel.Anonymous`, feature HTTP obbligatoria, risposta `200` anche per pagina vuota e nessun mapping trasversale duplicato.

Aggiornare `BusinessErrorCodes`, `ApiRoutes`, `OpenApiDocumentProvider` e i test di parita affinche verifichino metodo, route, security e parametri reali.

OpenAPI deve descrivere `familyId`, `pageSize`, `cursor`, gli schemi pagina/item/categoria/autore, risposte `200`, `400`, `401`, `403`, `500`, `503` e `application/problem+json`.

## 8. Telemetria

Aggiungere l'operation `kinlist.items_page` con outcome finiti `success`, `empty`, `cursor_invalid`, `denied`, `dependency_failure` e `technical_failure`.

Registrare durata, dimensione richiesta, dimensione effettiva e cardinalita tramite histogram. Usare soltanto tag a cardinalita finita: operation, outcome, direzione, presenza cursore e categoria errore.

Non registrare cursore, famiglia, user ID, item ID, nomi, categorie, owner, payload o SQL parametrizzato.

Aggiornare `docs/operations/observability.md` con verifiche e query aggregate.

## 9. Frontend e design system

Estendere `src/frontend/src/lib/api.ts` con tipi pagina/item/categoria/autore e `getKinListItems(familyId, pageSize, cursor?, signal?)`. Usare query encoding, token MSAL, correlation ID, `cache: "no-store"`, `credentials: "omit"` e `AbortSignal`. Il client non decodifica, ordina o filtra il cursore per sicurezza.

Trasformare `KinListAccessGate` in un confine che monta la lista soltanto dopo il `204`, passando `familyId` tramite prop/render function e non tramite DOM. Correggere retry bootstrap, pulizia del contesto dopo `401`/`403`, cleanup della barra e risposte obsolete.

Creare `KinListView` con stati:

| Stato | Comportamento |
|---|---|
| `initialLoading` | `StatePanel` busy, nessuna falsa empty |
| `empty` | Nessun item attivo visibile, senza microfono placeholder |
| `ready` | Pagina ricevuta nell'ordine server |
| `refreshing` | Pagina corrente leggibile, refresh bloccato |
| `navigating` | Pagina corrente leggibile, controlli disabilitati |
| `cursorInvalid` | Pagina preservata e azione `Torna all'inizio` |
| `error` | Pagina preservata se della stessa famiglia |
| `sessionExpired`/`forbidden`/`offline` | Item e cursori cancellati |

Ogni request usa `AbortController`, generazione per ignorare risultati stale e lock pending contro doppio invio. Il refresh usa sempre `cursor = null`.

Evolvere `Pagination` a `hasPrevious`, `hasNext`, `busy`, `onPrevious` e `onNext`, senza numero pagina o totale.

Evolvere `Avatar` per supportare nome accessibile, display name opzionale e fallback visuale `?`.

Rendere `KinListItem` passivo: nome, massimo tre `Badge`, `+N`, avatar autore e nessun pulsante o checkbox in questa feature.

Usare `ul/li`, un solo `h1` da `PageScaffold`, heading level 2 per gli stati, live region, `aria-busy`, alert per errori, focus prevedibile dopo navigazione e focus visibile.

Aggiornare CSS per mobile, wrap, safe area, pagination responsive, temi light/dark/system e reduced motion. La barra contestuale contiene soltanto `Aggiorna`.

Poiche cambiano primitive ufficiali, aggiornare test, catalogo e skill frontend e rigenerare il registry.

## 10. Test

### Domain e Business

Verificare factory `CreateShared`, invarianti owner/visibility/ordine, enum chiusi, revisione, normalizzazione categorie, clamp limite, configurazione invalida, disponibilita KinList, codec invalido, DTO senza dati sensibili e propagazione cancellation.

### Codec

Verificare round trip, tampering, expiry, scope famiglia/utente, ordine/versione/direzione/dimensione incompatibili, formato sconosciuto, assenza di valori personali in chiaro e key ring condiviso.

### PostgreSQL

Estrarre l'harness Testcontainers esistente in una utility condivisa e renderlo eseguibile nella CI Linux. Preparare dataset con due famiglie, membri multipli, Shared, Personal, stati inattivi/completati, timestamp uguali, posizioni, ID e categorie.

Verificare migration, FK/check/indici, isolamento, predicato, categorie senza leak, ordine, avanti/indietro, `Take(n + 1)`, inserimenti tra pagine, eliminazione dell'anchor, modifica senza riordino, cursori e piani query.

### Functions e pipeline

Verificare metadata `Family`, ID applicativo nella feature, parametri, risposte `200/400/401/403/500/503`, Problem Details, correlation, cache, cancellazione, OpenAPI/verbo/security e telemetria redatta.

### Frontend

Verificare gate prima del `204`, assenza di `familyId` nel DOM, loading, empty, righe, categorie, avatar fallback, previous/next, refresh, errore, cursore invalido, abort, risposte stale, `401`/`403`/offline, focus, temi, mobile e assenza dei controlli fuori scope.

## 11. Documentazione e rollout

Aggiornare:

- `docs/user-guide/it/kinlist.md`;
- `docs/user-guide/en/kinlist.md`;
- help e testi pagina `it`/`en`;
- `docs/operations/database-migrations.md`;
- `docs/operations/observability.md`;
- `docs/architecture/http-functions.md`;
- documentazione e skill del design system;
- change fragment bilingue di tipo `added`.

La guida deve descrivere lista, ordine, autore, categorie, refresh, avanti/indietro, empty, cursore invalido, accesso negato, requisito online e assenza intenzionale di creazione, filtro, completamento e microfono.

Rigenerare indice documentazione, release notes/JSON, skill registry, migration designer/snapshot e output Bicep. Non creare una nuova route e non modificare Workbox se le API restano `NetworkOnly`.

Aggiornare il workflow di deployment con grant runtime e default privileges anche sullo schema `kinlist`; verificare quoting SQL e permessi effettivi dopo il deploy.

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
npm run --prefix src/frontend design-system:validate
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

Completare manualmente prima/ultima pagina, navigazione avanti/indietro, inserimenti concorrenti, item Shared/Personal, cursori alterati/scaduti, refresh, `403`, cambio account, offline, Chrome desktop/Android PWA/Edge, italiano/inglese, temi, tastiera, zoom e screen reader.

In ambiente distribuito verificare migration history, schema `kinlist`, grant runtime, key ring condiviso, `/health/live`, `/health/ready`, `/api/version`, API autenticata, assenza cache API e telemetria priva di contenuti sensibili.

## Sequenza di rilascio

1. Portare FEAT-003 da `Open` a `In progress` all'avvio dell'implementazione.
2. Congelare CP-002 e TECH-003 secondo questo piano.
3. Estendere il contesto Family con l'ID applicativo verificato.
4. Implementare dominio, repository contract e opzioni.
5. Implementare e testare codec cursori e key ring condiviso.
6. Aggiungere schema `kinlist`, migration, grant e test PostgreSQL.
7. Implementare query keyset, predicato, Business, endpoint, OpenAPI e telemetria.
8. Integrare client API, gate, lista e primitive del design system.
9. Aggiornare traduzioni, guide, help, runbook, skill e fragment.
10. Eseguire build, test, publish, package e validatori completi.
11. Applicare migration e grant prima del nuovo backend; distribuire backend e frontend.
12. Verificare stato live, accesso autenticato, key ring e telemetria.
13. Portare la feature a `In review`, senza contrassegnarla autonomamente `Completed`.
14. Creare commit su `dev`, push e pull request verso `main`.
15. Attendere tutte le GitHub Actions dell'ultimo SHA con esito `success`, senza eseguire merge.

Il rollback applicativo usa backend e frontend N-1 lasciando lo schema additivo in posizione. Il `Down` e consentito soltanto prima di scritture o su database disposable; dopo dati KinList reali si usa una migration correttiva compatibile preceduta da backup e verifica esplicita.
