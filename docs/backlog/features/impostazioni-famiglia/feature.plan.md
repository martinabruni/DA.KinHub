# Piano di implementazione - FEAT-004

## Obiettivo

Consentire a un membro con membership attiva di raggiungere le Impostazioni dalla shell KinHub e consultare la route ricostruibile `/settings/family`, mostrando il nome della famiglia, pagine limitate di membri e pagine limitate di inviti attivi senza esporre il codice o la relativa impronta.

La slice estende le Impostazioni esistenti senza rimuovere lingua, tema, tutorial o PWA, usa soltanto il design system condiviso e prepara contratti stabili per FEAT-005 e FEAT-006 senza mostrare azioni non ancora funzionanti.

## Contratti esecutivi

- FEAT-002 e FEAT-014 sono prerequisiti integrati; CP-001 e il contratto di paginazione condiviso con FEAT-003 devono essere congelati prima di modificare aree comuni.
- La floating navigation resta una sola ed e posseduta dalla shell: non si monta un secondo ingranaggio o una seconda barra dentro `KinListPage` o `KinListView`.
- Il link Settings della shell e il punto di accesso autorevole. Su KinList deve risultare riconoscibile, dentro la safe area e separato da barra contestuale, futuro microfono e snackbar; se il layout corrente non soddisfa FR-034, si corregge lo slot della shell senza duplicare il controllo.
- `/settings` conserva tutte le sezioni attuali e aggiunge la voce Famiglia soltanto quando il bootstrap ha prodotto un contesto famiglia attivo; la voce apre `/settings/family`.
- `/settings/family` usa `ProtectedRoute`, `PageScaffold`, route registry, help repository e guida bilingue. Non reintroduce l'accordion help inline rimossa dalla CR FEAT-014: la guida resta raggiungibile dal percorso globale approvato.
- API di dettaglio: `GET /api/kinhub/families/details?familyId=<uuid>`.
- API membri: `GET /api/kinhub/families/members?familyId=<uuid>&pageSize=<positive>&cursor=<opaque?>`.
- API inviti attivi: `GET /api/kinhub/families/invitations?familyId=<uuid>&pageSize=<positive>&cursor=<opaque?>`.
- Tutti e tre gli endpoint usano `AuthorizationLevel.Anonymous`, autenticazione `ApiAccess`, marker `[RequiresFamilyAccess]`, policy esattamente `Family` e scope `familyId` ripetuto nel Business e nel repository.
- Il dettaglio restituisce soltanto `{ "name": "<nome famiglia>" }`; non restituisce creatore, conteggi, ruoli, codice o impostazioni amministrative.
- La pagina membri restituisce solo proiezioni di presentazione minime. Un nome disponibile produce `displayName` e `initials`; in assenza di un nome approvato entrambi restano null e il frontend mostra `Membro`/`Member` e `?`.
- Non si ricavano nomi da email, username, `preferred_username`, claim non approvati o token. FEAT-004 non aggiunge una nuova persistenza PII per chiudere ASM-004.
- La pagina inviti espone ID tecnico necessario alle future azioni, creatore con la stessa proiezione minima, `createdAt`, `expiresAt` e stato chiuso `active`; non espone mai codice, formato del codice, HMAC, versione chiave o motivo interno di validita.
- Le due collezioni usano pagine indipendenti, cursori indipendenti e nessun `totalCount`, numero pagina, offset o percorso `GetAll`.
- Dimensione frontend iniziale: 50, senza selettore. Il server usa `effectivePageSize = min(requestedPageSize, Pagination:ReadMax)`; il limite configurato iniziale e assoluto resta 5000.
- Valori `pageSize` assenti, malformati o non positivi restituiscono `400 pagination.pageSizeInvalid`; valori superiori sono limitati, non rifiutati.
- Un cursore alterato, scaduto, cross-family, di altra collezione, direzione, ordine o dimensione restituisce `400 pagination.cursorInvalid` senza righe.
- Una pagina senza risultati con cursore in ingresso e trattata come cursore stale e permette di ripartire dalla prima pagina; soltanto la prima pagina inviti vuota produce lo stato empty approvato.
- Una famiglia accessibile con zero membership attive e uno stato dati incoerente e restituisce `409 family.stateInconsistent`, non una pagina membri vuota.
- Errori applicabili: `400 pagination.pageSizeInvalid`, `400 pagination.cursorInvalid`, `401` auth esistenti, `403 family.accessDenied`, `409 family.stateInconsistent`, `503 dependency.postgresqlUnavailable`, `503 dependency.storageUnavailable` e `500 internal.unexpected`.
- Risposte autenticate ed errori usano `Cache-Control: no-store, private`, correlation ID e Problem Details centralizzati.
- Dati, pagine e cursori restano solo in memoria e non entrano in `localStorage`, `sessionStorage`, IndexedDB, Cache API, URL frontend o service worker.

### Contratto pagina membri

```json
{
  "items": [
    {
      "displayName": null,
      "initials": null
    }
  ],
  "effectivePageSize": 50,
  "maxPageSize": 5000,
  "previousCursor": null,
  "nextCursor": "opaque"
}
```

### Contratto pagina inviti

```json
{
  "items": [
    {
      "id": "uuid",
      "creator": {
        "displayName": null,
        "initials": null
      },
      "createdAt": "2026-07-30T10:00:00Z",
      "expiresAt": "2026-08-06T10:00:00Z",
      "status": "active"
    }
  ],
  "effectivePageSize": 50,
  "maxPageSize": 5000,
  "previousCursor": null,
  "nextCursor": "opaque"
}
```

## 1. Verificare prerequisiti e baseline

Confermare nel codice corrente:

- bootstrap `GET /api/kinhub/bootstrap`, contesto `familyId` solo in memoria e pulizia su account/sessione;
- famiglia e membership attive create da FEAT-002;
- `KinHubAuthorizationFeature` con `FamilyId` e `ApplicationUserId` verificati;
- policy `Family`, query parameter `familyId` e middleware HTTP condivisi;
- schema `shared` con `application_users`, `families` e `family_memberships`;
- `Pagination:ReadMax`, Data Protection e key ring persistito introdotti o congelati con FEAT-003;
- `PageScaffold`, `StatePanel`, `Avatar`, `Pagination`, `MemberRow`, `InviteRow` e floating navigation di FEAT-014;
- `SettingsPage` con lingua, tema, tutorial e PWA ancora presenti;
- API autenticate escluse dalla cache PWA e fallback SPA gia valido per route client-side.

Congelare con FEAT-003:

- forma comune della pagina e nomi `effectivePageSize`, `maxPageSize`, `previousCursor`, `nextCursor`;
- codici `pagination.pageSizeInvalid` e `pagination.cursorInvalid`;
- configurazione `Pagination:ReadMax` e ceiling 5000;
- registrazione Data Protection, key ring e mapping di `dependency.storageUnavailable`;
- convenzioni `AbortSignal`, `cache: no-store`, correlation ID e pulizia risultati stale nel client API.

TECH-008 richiede una verifica sul layout reale della shell. Misurare barra globale, barra contestuale KinList, viewport mobile, safe area, zoom 200%, tastiera e spazio riservato alle snackbar. Se l'ingranaggio esistente soddisfa FR-034, riusarlo senza un secondo controllo; altrimenti correggere il layout posseduto da `Layout`/`FloatingBars` mantenendo una sola barra.

## 2. Confine dati degli inviti

FEAT-004 deve poter leggere una collezione reale e consegnare a FEAT-005 un contratto stabile. Introdurre quindi nello schema shared il modello persistente completo necessario a rappresentare un invito, senza endpoint o casi d'uso di generazione, revoca o consumo. Il contratto deriva da FR-037-FR-040, DEC-020/021, ADR-012 e `docs/research/tasks/family-invite-code/research.md`; FEAT-004 non dipende da una decisione futura di FEAT-005 per completare la propria migration.

Il modello deve rappresentare almeno:

- identificativo invito;
- famiglia;
- impronta HMAC binaria non vuota e relativa versione positiva, mai il codice in chiaro;
- creatore applicativo;
- creazione e scadenza UTC;
- eventuale revoca;
- eventuale consumo e utente applicativo che lo ha consumato;
- revisione o vincolo concorrente sufficiente a consentire a FEAT-005 un solo consumo.

Usare PostgreSQL `bytea` non vuoto per l'HMAC e una versione numerica positiva, con indice univoco `(hmac_key_version, code_hmac)` per la futura ricerca. FEAT-004 non fissa l'algoritmo crittografico ne calcola impronte: la lunghezza resta quella dei byte prodotti dal contratto FEAT-005 e il database verifica soltanto che l'impronta non sia vuota. Non introdurre codice in chiaro, cifratura reversibile, default sintetici o colonne nullable che permettano inviti incompleti in produzione.

FEAT-004 possiede:

- entita e configurazione EF sufficienti a persistere e leggere il record completo senza esporne il segreto;
- migration, foreign key, check e indici necessari alla lettura attiva;
- repository read-only paginato e projection metadata;
- factory dominio che accetta un'impronta gia calcolata e i metadati server-side, senza generare o conoscere il codice;
- fixture di integrazione che crea record validi tramite tale factory con impronte sintetiche non sensibili.

FEAT-005 possiede:

- generazione crittografica e formato Crockford Base32;
- calcolo e rotazione HMAC;
- limite di cinque inviti attivi;
- endpoint create/revoke/join, risposta one-time e conferme UI;
- consumo, riattivazione membership, concorrenza e rate limit.

FEAT-006 possiede leave e revoca atomica degli inviti creati dal membro. Nessun controllo `Invita`, `Revoca` o `Lascia famiglia` viene montato da FEAT-004.

## 3. Modello shared e migration

Estendere il Domain shared con l'invito familiare rispettando le regole gia approvate:

- GUID non vuoti per invito, famiglia e creatore;
- timestamp UTC coerenti;
- scadenza successiva alla creazione;
- stato attivo derivato da scadenza, revoca e consumo, non salvato come stringa modificabile;
- nessun ruolo o privilegio speciale per il creatore;
- nessun metodo pubblico di generazione, revoca o consumo in questa slice oltre a quanto serve a reidratare e testare uno stato valido.

Configurare EF nello schema `shared` con:

- PK UUID;
- FK alla famiglia e al creatore applicativo con `DeleteBehavior.Restrict`;
- FK opzionale all'utente che ha consumato l'invito, coerente con `consumed_at`;
- timestamp obbligatori e check coerenti;
- `code_hmac` binario obbligatorio, `hmac_key_version > 0` e indice univoco composto;
- token/revisione concorrente adatto al futuro consumo condizionato;
- indice keyset per inviti attivi per famiglia e ordine approvato;
- alternate key o FK composite soltanto se serve realmente a impedire associazioni cross-family.

Generare migration, designer e snapshot tramite il design-time factory autorevole. La migration deve essere additiva, non modificare dati esistenti e avere un `Down` che rimuove soltanto tabella, vincoli e indici introdotti da FEAT-004.

Verificare i grant runtime sullo schema `shared`; non aggiungere nuove risorse Azure o nuovi secret. Documentare verifica e rollback in `docs/operations/database-migrations.md`. Dopo l'esistenza di inviti reali, il rollback applicativo lascia lo schema additivo in posizione o usa una migration correttiva preceduta da backup/PITR, non un `Down` distruttivo.

## 4. Ordini e cursori specifici

Definire due ordini totali versionati:

```text
family-members-v1:
  MembershipCreatedAt ASC, MembershipId ASC

family-active-invitations-v1:
  CreatedAt DESC, InvitationId DESC
```

L'ordine membri non dipende dal nome, perche il nome puo essere assente o cambiare in futuro. L'ordine inviti rende prima visibili i piu recenti e non dipende da creatore o scadenza.

Creare codec specifici per membri e inviti, riusando registrazione e persistenza Data Protection senza trasformare il codec `active-items` in un framework universale. Ogni payload protetto contiene soltanto:

```text
formatVersion
collection
orderVersion
direction
effectivePageSize
anchor dell'ordine
expiresAt
```

Famiglia, collezione, ordine e dimensione sono legati al purpose protetto. Il cursore non contiene nomi, iniziali, codice, HMAC, claim, issuer o identificativi esterni leggibili.

Usare `TimeProvider` e la stessa durata tecnica di 30 minuti congelata con FEAT-003. Alterazione, scadenza, purpose errato o formato sconosciuto producono `pagination.cursorInvalid`; un guasto reale del key ring produce `dependency.storageUnavailable`.

Per gli inviti acquisire una volta `nowUtc` nel caso d'uso e applicare sempre il predicato corrente:

```text
FamilyId == familyId
AND RevokedAt IS NULL
AND ConsumedAt IS NULL
AND ExpiresAt > nowUtc
```

Revoca, consumo o scadenza concorrenti possono rendere stale una pagina; non devono mai far ricomparire un invito non piu attivo.

## 5. Repository keyset e proiezioni minime

Creare repository read-only mirati per dettaglio famiglia, pagina membri attivi e pagina inviti attivi. Non estendere i repository di scrittura con `GetAll`, query generiche o una unita di lavoro universale.

La lettura dettaglio deve:

- richiedere `familyId` e `CancellationToken`;
- filtrare la famiglia attiva;
- proiettare soltanto il nome;
- non usare il creatore come autorizzazione.

La lettura membri deve:

- filtrare `familyId`, membership attiva, utente applicativo attivo e famiglia attiva prima dell'ordine;
- applicare keyset avanti/indietro e leggere al massimo `effectivePageSize + 1`;
- non usare offset, numero pagina o conteggio totale;
- proiettare soltanto i dati necessari a nome e iniziali;
- restituire zero righe come stato incoerente dopo che la policy ha concesso la famiglia.

La lettura inviti deve:

- applicare il predicato attivo prima dell'ordine e della pagina;
- leggere al massimo `effectivePageSize + 1`;
- caricare il creatore senza N+1;
- escludere materialmente codice, HMAC, versione chiave e dettagli interni dalla projection;
- restituire una prima pagina vuota come empty valido.

Per la navigazione indietro invertire confronto e ordine nella query, rimuovere la sentinella e invertire in memoria soltanto la pagina per restituire sempre l'ordine visuale canonico.

Verificare gli indici con dataset rappresentativi e `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)`. Non fissare test fragili su un nodo specifico del planner; verificare invece scope, ordine, limite e assenza di scansioni patologiche rispetto ai dati preparati.

## 6. Business e gestione degli esiti

Creare servizi scoped di lettura oppure un servizio Family coeso con metodi distinti per:

```text
GetFamilyDetailsAsync(familyId, cancellationToken)
GetFamilyMembersPageAsync(familyId, requestedPageSize, opaqueCursor, cancellationToken)
GetActiveFamilyInvitationsPageAsync(familyId, requestedPageSize, opaqueCursor, cancellationToken)
```

Ogni metodo:

1. Riceve esplicitamente il `familyId` verificato dalla feature HTTP.
2. Valida `pageSize` quando applicabile e calcola il limite effettivo.
3. Decodifica il cursore prima di interrogare la collezione.
4. Invoca il repository scoped con `CancellationToken`.
5. Costruisce DTO e cursori di risposta senza dati sensibili.
6. Usa il fallback membro soltanto come dato di presentazione localizzato nel frontend, non come nome persistito.
7. Traduce zero membri in `family.stateInconsistent`.
8. Traduce soltanto guasti reali PostgreSQL/Storage nelle eccezioni di dipendenza.
9. Propaga cancellazioni e bug applicativi senza convertirli in `500` o indisponibilita.

Non leggere identita da `HttpContext`, `IHttpContextAccessor`, `AsyncLocal` o current-user ambientali. Non introdurre cache delle pagine o della membership.

Registrare servizi, repository e codec nei layer DI esistenti con lifetime coerenti. Se le opzioni `Pagination:ReadMax` e Data Protection sono gia registrate da FEAT-003, riusarle senza una seconda configurazione o app setting.

## 7. Endpoint, Problem Details e OpenAPI

Aggiungere tre Function sottili per dettaglio, membri e inviti:

- trigger `GET` con `AuthorizationLevel.Anonymous`;
- nessun `[AllowAnonymous]`;
- marker `[RequiresFamilyAccess]`;
- identita e famiglia lette esclusivamente da `KinHubAuthorizationFeature`;
- `familyId` soltanto in query string;
- `pageSize` e `cursor` soltanto per le collezioni;
- nessun mapping trasversale duplicato nell'endpoint.

Aggiornare nella stessa modifica:

- `Http/ApiRoutes.cs` con le route autorevoli;
- `BusinessErrorCodes` con i soli codici nuovi necessari;
- `OpenApi/OpenApiDocumentProvider.cs`;
- `openapi.yaml` statico;
- test di parita route, verbo, parametri, security, media type e risposte.

OpenAPI deve documentare:

- bearer security e policy applicativa;
- `familyId`, `pageSize` e `cursor` applicabili;
- schemi dettaglio, membro, creatore, invito e pagina;
- `200`, `400`, `401`, `403`, `409`, `500`, `503`;
- `application/problem+json` per ogni errore applicabile;
- assenza del codice invito e dell'HMAC da ogni schema pubblico.

Non riusare `family-context` per restituire dati: quell'endpoint resta un controllo `204` con responsabilita distinta.

## 8. Telemetria redatta

Aggiungere operation name KinHub a cardinalita finita:

```text
kinhub.family_details
kinhub.family_members_page
kinhub.family_invitations_page
```

Registrare:

- durata;
- esito `success`, `empty`, `cursor_invalid`, `denied`, `inconsistent`, `dependency_failure` o `technical_failure`;
- dimensione richiesta ed effettiva;
- direzione e presenza cursore;
- numero di righe restituito come misura, non come tag ad alta cardinalita.

Non registrare famiglia, user ID, membership ID, invitation ID, cursore, nome, iniziali, timestamp individuali, codice, HMAC, claim, payload o SQL con parametri personali.

Aggiornare `docs/operations/observability.md` con verifiche aggregate per latenza, errori cursore e stato incoerente. Mantenere una sola pipeline OpenTelemetry/Azure Monitor.

## 9. Client API e stato Family frontend

Estendere `src/frontend/src/lib/api.ts` con tipi e metodi distinti:

```text
getFamilyDetails(familyId, signal)
getFamilyMembers(familyId, pageSize, cursor, signal)
getFamilyInvitations(familyId, pageSize, cursor, signal)
```

Il client continua a usare token MSAL, correlation ID, `cache: no-store`, `credentials: omit`, query encoding, Problem Details e `AbortSignal`. Non decodifica, ordina o modifica cursori e dati restituiti.

Creare `FamilySettingsPage` con tre responsabilita visive:

- intestazione e nome famiglia;
- sezione membri paginata;
- sezione inviti attivi paginata.

Le due collezioni hanno stato e cursori indipendenti. Il caricamento iniziale puo partire in parallelo dopo che il bootstrap ha fornito `familyId`, ma la pagina non mostra dati di una famiglia precedente mentre cambia contesto.

Macchina a stati minima:

| Stato | Comportamento |
|---|---|
| Bootstrap/loading iniziale | `StatePanel` busy; nessun falso empty |
| Ready | Nome e sezioni valide della famiglia corrente |
| Inviti empty | Stato dedicato, senza pulsante Invita prima di FEAT-005 |
| Navigazione pagina | Pagina corrente leggibile con sezione `aria-busy` e controlli disabilitati |
| Cursor invalid | Rimuove i dati della sezione interessata e offre `Torna all'inizio` |
| Errore recuperabile | Rimuove i dati interessati e offre Riprova dalla prima pagina |
| Family inconsistent | Rimuove tutti i dati e mostra errore dedicato, non empty |
| `401`/`403` | Rimuove tutti i dati e cursori; mostra sessione/accesso negato senza dettagli famiglia |
| Offline | Rimuove dati e cursori; mostra requisito online senza usare cache |
| Cambio account/famiglia/unmount | Aborta richieste, incrementa la generazione e ignora risultati obsoleti |

Durante una request di paginazione la pagina corrente puo restare leggibile per evitare salti, ma al fallimento i dati della sezione vengono cancellati come richiesto da FEAT-004. Un refresh esplicito riparte dalla prima pagina di entrambe le collezioni.

Usare `AbortController`, una generazione di richiesta e lock pending per impedire risultati stale o navigazioni duplicate. Una risposta della famiglia precedente non deve mai aggiornare il DOM.

## 10. Routing, Settings e focus

Aggiornare `src/frontend/src/App.tsx` con `/settings/family` sotto `ProtectedRoute` e registrare la route in `route-registry.json` con:

- ID stabile `familySettings`;
- titolo localizzato;
- help key dedicata;
- guida `family` esistente in italiano e inglese.

Estendere `SettingsPage` senza ricostruirla:

- mantenere aspetto/lingua, tutorial e PWA;
- aggiungere una `Card` Famiglia costruita sulle primitive FEAT-014;
- mostrare un link semantico verso `/settings/family`, non un button annidato in un link;
- non mostrare controlli `Invita` o `Lascia famiglia`;
- non mostrare una affordance non funzionante agli utenti nello stato onboarding.

Per URL diretto e refresh, attendere il bootstrap memoria e poi caricare i dati con il `familyId` autorevole. Uno stato onboarding non chiama le API Family e offre il ritorno al percorso onboarding esistente.

Per il focus:

- ingresso normale o URL diretto: `PageScaffold` sposta il focus sul titolo;
- cambio pagina membri/inviti: al successo sposta il focus sul titolo della sezione o sul primo elemento secondo il pattern `Pagination` condiviso;
- cursor invalid/errore: focus sul relativo alert o titolo stato;
- ritorno da Settings a KinList: se la navigazione era partita dall'ingranaggio e il controllo esiste ancora, ripristina il focus sullo stesso controllo;
- refresh o history senza trigger precedente: non tenta focus su elementi inesistenti e mantiene il focus del `PageScaffold`.

Usare stato di navigazione tipizzato o un piccolo contratto shell per il ripristino focus. Non salvare il target nel browser storage e non introdurre un gestore di focus universale se non necessario.

## 11. Componenti, accessibilita, temi e responsive

Riusare `FamilyCard`, `MemberRow`, `InviteRow`, `Avatar`, `Pagination`, `StatePanel`, `Card` e link/button ufficiali soltanto dove i loro contratti corrispondono al comportamento.

Prima dell'uso correggere i wrapper specifici se espongono azioni non consentite. In particolare `InviteRow` non deve montare copia, revoca o codice in FEAT-004: renderlo passivo o separare la presentazione metadata dall'azione futura senza creare un componente parallelo equivalente.

La pagina deve:

- usare struttura semantica con heading ordinati e liste `ul/li`;
- avere nomi accessibili per avatar, sezioni e navigazione pagine;
- mostrare `Membro`/`Member` e `?` quando il nome non e disponibile;
- formattare creazione e scadenza con `Intl.DateTimeFormat` nella lingua corrente;
- non mostrare timestamp grezzi o timezone ambigue;
- annunciare loading/errori con live region appropriate senza messaggi ripetuti;
- mantenere focus visibile, target touch, zoom 200% e contrasto nei temi light/dark/system;
- rispettare `prefers-reduced-motion`;
- evitare sovrapposizioni con safe area, floating navigation, snackbar e future azioni contestuali.

Ogni stringa visibile e nome accessibile usa i18next con parita `it`/`en`. Non aggiungere librerie UI o classi legacy.

## 12. Test

### Domain e Business

Aggiungere test per:

- invarianti del record invito e stato attivo derivato;
- scadenza, revoca e consumo esclusi dalla query attiva;
- page size non positivo, clamp a 5000 e configurazione invalida;
- mapping dettaglio, membri e inviti senza dati sensibili;
- fallback con `displayName`/`initials` null senza derivazione da claim;
- zero membri tradotto in `family.stateInconsistent`;
- propagazione `CancellationToken` e distinzione PostgreSQL/Storage/bug.

### Codec

Verificare per entrambe le collezioni:

- round trip avanti/indietro;
- tampering e scadenza;
- scope cross-family e cross-collection;
- ordine, versione, direzione e dimensione incompatibili;
- formato sconosciuto;
- assenza di dati personali o segreti in chiaro;
- key ring condiviso tra istanze.

### PostgreSQL

Preparare dataset con almeno due famiglie, membri attivi/inattivi, utenti attivi/inattivi e inviti attivi/scaduti/revocati/consumati con timestamp e ID uguali sui prefissi d'ordine.

Verificare:

- migration su database vuoto e schema/foreign key/check/indici attesi;
- isolamento tra famiglie;
- esclusione di membership, utenti e famiglie inattive;
- ordine membri e inviti;
- navigazione keyset avanti/indietro e `Take(n + 1)`;
- cambi concorrenti tra pagine;
- prima pagina inviti vuota distinta da cursore stale;
- zero membri incoerente;
- projection SQL senza codice/HMAC;
- piano query rappresentativo e grant runtime.

### Functions e pipeline

Verificare:

- metadata `ApiAccess`/`Family`, query `familyId` e metodo `GET`;
- parametri validi, assenti e malformati;
- `200`, `400`, `401`, `403`, `409`, `500`, `503`;
- correlation ID, Problem Details e `no-store, private`;
- cancellazione non convertita in `500`;
- route/OpenAPI runtime/statico/security paritari;
- nessun segreto o dato personale in risposta o telemetria;
- almeno uno smoke test attraverso il worker Functions reale.

### Frontend

Verificare:

- Settings preserva le sezioni attuali e aggiunge Famiglia solo con contesto valido;
- route diretta, refresh, Indietro e Avanti;
- focus titolo, ritorno all'ingranaggio e focus dopo pagina/errore;
- dettaglio famiglia, membri nominati e fallback;
- inviti metadata senza codice, copia o revoca;
- paginazione indipendente membri/inviti;
- loading, empty inviti, cursor invalid, errore, incoerenza, offline, `401` e `403`;
- cancellazione dati e cursori dopo errori o cambio account/famiglia;
- abort e risposta obsoleta ignorata;
- date localizzate `it`/`en`;
- mobile, safe area, zoom, tastiera, screen reader, reduced motion e temi;
- assenza di `Invita` e `Lascia famiglia` prima di FEAT-005/006;
- una sola floating navigation e un solo link Settings.

## 13. Documentazione e artefatti

Aggiornare:

- `docs/user-guide/it/settings.md` e `docs/user-guide/en/settings.md` con la voce Famiglia;
- nuove guide `docs/user-guide/it/family.md` e `docs/user-guide/en/family.md`;
- help `it`/`en` per scopo, prerequisiti, dati mostrati, pagine, fallback, empty, errori e requisito online;
- testi pagina `it`/`en` per sezioni, stati e azioni di recupero;
- `docs/operations/database-migrations.md` con verifica e rollback;
- `docs/operations/observability.md` con metriche e query aggregate;
- `docs/architecture/http-functions.md` se il nuovo pattern di pagine Family aggiunge un esempio utile senza duplicare le regole;
- OpenAPI runtime e `openapi.yaml`;
- skill/catalogo frontend soltanto se cambia l'API pubblica di una primitive promossa;
- change fragment bilingue di tipo `added`;
- patch note, docs index, skill registry, migration designer/snapshot e altri output generati dalle rispettive fonti.

La guida Family deve spiegare nome, membri, iniziali/fallback, inviti attivi, paginazione, assenza intenzionale del codice e requisito online. Non documentare generazione, revoca o leave finche FEAT-005/006 non li rendono raggiungibili.

## 14. Verifica finale

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

1. Aprire KinList su mobile e desktop e raggiungere Settings dall'unico ingranaggio.
2. Verificare che barra, contenuto, snackbar e safe area non si sovrappongano.
3. Verificare che lingua, tema, tutorial e PWA siano ancora presenti.
4. Aprire Famiglia da Settings, con URL diretto, refresh, Indietro e Avanti.
5. Navigare avanti/indietro su piu pagine membri e inviti.
6. Verificare membro senza nome e famiglia senza inviti.
7. Alterare/scadere un cursore e ripartire dalla prima pagina.
8. Revocare accesso o cambiare account durante una request e verificare assenza di dati stale.
9. Simulare zero membri e guasti PostgreSQL/Storage senza leak di dati.
10. Verificare Chrome desktop, Chrome Android/PWA ed Edge con italiano/inglese, temi, tastiera, zoom e screen reader.

In ambiente distribuito verificare inoltre migration history, schema e grant `shared`, key ring condiviso, `/health/live`, `/health/ready`, `/api/version`, API autenticate, fallback SPA e ingestione di trace/metriche senza nomi, identificativi, cursori o segreti.

## Sequenza di rilascio

1. Portare FEAT-004 da `Open` a `In progress` all'avvio dell'implementazione.
2. Verificare FEAT-002/014, CP-001 e chiudere TECH-008 per la shell corrente.
3. Congelare con FEAT-003 pagina, limite, Data Protection e codici cursore.
4. Congelare il modello persistente invito dalle fonti approvate e registrarlo come contratto in ingresso per FEAT-005, senza implementarne i flussi.
5. Implementare modello shared, migration, grant e test PostgreSQL.
6. Implementare codec, repository keyset e servizi Business.
7. Implementare endpoint, Problem Details, OpenAPI e telemetria.
8. Integrare client API, route, Settings, pagina Family e focus.
9. Aggiornare traduzioni, help, guide, runbook, osservabilita e change fragment.
10. Eseguire build, test, publish, package e validatori completi.
11. Applicare migration e grant prima del nuovo backend; distribuire backend prima del frontend.
12. Verificare stato live, route diretta, pagine, key ring e telemetria.
13. Portare la feature a `In review`, senza contrassegnarla autonomamente `Completed`.
14. Creare commit su `dev`, push e pull request verso `main`.
15. Attendere tutte le GitHub Actions dell'ultimo SHA con esito `success`, senza eseguire merge.

Il rollback applicativo usa backend e frontend N-1 lasciando lo schema additivo in posizione. Prima di dati invito reali il `Down` puo essere verificato su database disposable; dopo dati reali si usa una migration correttiva compatibile preceduta da backup e verifica esplicita.
