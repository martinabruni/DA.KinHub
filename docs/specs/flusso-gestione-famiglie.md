# Descrizione generale

La macro feature **Gestione Famiglie** modella il concetto centrale di KinHub: ogni utente possiede **una** famiglia, la famiglia ha dei **membri** (profili, non account) e un insieme di **servizi KinHub** attivabili/disattivabili (es. KinList). Oltre alle operazioni CRUD sulla famiglia, questa feature fornisce un meccanismo trasversale fondamentale: la **risoluzione del contesto famiglia**, cioè come il sistema stabilisce a quale famiglia appartiene l'utente autenticato per autorizzare le richieste negli altri servizi.

Cosa fa:

- Crea una famiglia per l'utente (con il profilo del proprietario e membri aggiuntivi), attivando di default tutti i servizi del catalogo.
- Aggiunge/aggiorna/rimuove membri.
- Aggiorna i dati della famiglia e la elimina.
- Attiva/disattiva i servizi KinHub per la famiglia (`ServicesController`).
- Espone il contesto famiglia agli altri host tramite `GET /api/access/family-context`.

Perché esiste: la famiglia è l'**unità di autorizzazione** del sistema. Ricette e liste non appartengono a un singolo utente ma alla famiglia; quindi serve un punto unico (Identity) che sappia mappare `userId → familyId`.

Parti coinvolte:

- **Presentation** — `FamilyController`, `ServicesController` (`Kin.KinHub.Identity.Api/FamilyFeature`), i validator (`CreateFamilyRequestValidator`, `AddFamilyMemberRequestValidator`, …), `IdentityFamilyContextResolver` (`Common`), e l'autorizzazione condivisa in `Kin.KinHub.Shared.Api/Common/Authorization`.
- **Business** — `KinHubFamilyService` (facciata), gli handler in `Core.Business/FamilyFeature/Commands` e `Queries`, `FamilyOwnershipService`, `KinHubServiceService`.
- **Domain** — entità `Family`, `FamilyMember`, `FamilyService`, `KinHubService`; interfacce `IFamilyRepository` (con `FindByUserIdAsync`), `IFamilyMemberRepository`, `IFamilyServiceRepository`, `IKinHubServiceRepository`; enum `KinHubServiceType`.
- **Infrastructure** — repository in `Kin.KinHub.Core.PostgreSql/FamilyFeature`.

Dati ricevuti: `CreateFamilyRequest` (nome famiglia, nome profilo proprietario, membri aggiuntivi), `AddFamilyMemberRequest`, `UpdateFamilyRequest`, `UpdateFamilyMemberRequest`, `ToggleFamilyServiceRequest`. Dati prodotti: `CreateFamilyResponse` (familyId, ownerMemberId), `FamilyDetailResponse`, `AddFamilyMemberResponse`, DTO dei servizi.

Dipendenze: EF Core/Npgsql per la persistenza, FluentValidation, l'autorizzazione ASP.NET Core (policy + handler custom).

# Casi d'uso

- **Creazione famiglia** — *Obiettivo*: creare l'unica famiglia dell'utente. *Attore*: utente autenticato senza famiglia. *Input*: `CreateFamilyRequest`. *Output*: 201 con `CreateFamilyResponse`. *Condizione/errore*: se l'utente ha già una famiglia → 409 Conflict ("A family already exists for this user.").
- **Lettura della propria famiglia** — *Obiettivo*: ottenere famiglia + membri + servizi. *Input*: solo il token. *Output*: `FamilyDetailResponse`. *Errore*: nessuna famiglia → 404.
- **Aggiunta/aggiornamento/rimozione membro** — *Attore*: proprietario della famiglia. *Input*: `familyId` in route + payload. *Output*: id membro creato o esito. *Condizione*: richiede la policy `FamilyContext` **e** la verifica di proprietà (`EnsureOwnershipAsync`). *Errore*: famiglia non posseduta → 403.
- **Aggiornamento/eliminazione famiglia** — *Attore*: proprietario. *Condizione*: policy `FamilyContext` + ownership. *Output*: esito.
- **Attivazione/disattivazione servizio** — *Obiettivo*: abilitare o meno un servizio KinHub (es. KinList) per la famiglia. *Input*: `ToggleFamilyServiceRequest`. *Gestito da*: `ServicesController` + `KinHubServiceService`.
- **Risoluzione contesto famiglia** — *Obiettivo (trasversale)*: dire "a quale famiglia appartiene l'utente". *Attore*: la pipeline di ogni API (via middleware) e gli altri host (via HTTP su `/api/access/family-context`).

# Flusso implementativo

## 1. Punto di ingresso

- CRUD famiglia: `FamilyController` su `api/families` (`CreateAsync`, `GetAsync`, `AddMemberAsync`, `UpdateMemberAsync`, `DeleteMemberAsync`, `UpdateFamilyAsync`, `DeleteFamilyAsync`). Il proprietario/utente arriva da `ICurrentUser.UserId` (mai dal body).
- Servizi: `ServicesController`.
- Contesto famiglia: `AccessController.GetFamilyContext` (`api/access/family-context`) e, a livello di pipeline, `JwtAuthenticationMiddleware` che invoca `IFamilyContextResolver`.

## 2. Validazione iniziale

- Null-check del body → `ApiProblemDetails.InvalidRequestBody`.
- Validazione FluentValidation via `IRequestValidator<T>` (es. `CreateFamilyRequestValidator`) → 400 con errori.
- **Autorizzazione a due livelli**:
  - `[Authorize]` semplice per creare/leggere la famiglia (basta un utente autenticato con scope `read`; qui l'utente potrebbe non avere ancora una famiglia).
  - `[Authorize(Policy = FamilyContextRequirement.PolicyName)]` per le operazioni sui membri/famiglia esistente: richiede che il contesto famiglia sia stato risolto.
- **Verifica di proprietà** applicativa: gli handler di mutazione chiamano `IFamilyOwnershipService.EnsureOwnershipAsync(familyId, userId)`.

## 3. Orchestrazione applicativa

- `FamilyController` → `IFamilyService` (`KinHubFamilyService`, facciata) → handler specifico.
- **Creazione** (`CreateFamilyHandler`): verifica assenza di famiglia esistente (`FindByUserIdAsync`), crea `Family`, crea il `FamilyMember` proprietario, crea i membri aggiuntivi, poi legge **tutti** i `KinHubService` del catalogo e per ciascuno crea un `FamilyService` con `IsActive = true`. Restituisce `CreateFamilyResponse`.
- **Aggiunta membro** (`AddFamilyMemberHandler`): prima `EnsureOwnershipAsync`; se ok, crea il `FamilyMember`.
- **Lettura** (`GetFamilyHandler`): recupera famiglia + membri + servizi e li mappa in `FamilyDetailResponse`.
- La conversione tra entità e DTO avviene negli handler/service (nessun mapper centralizzato per questa feature).

## 4. Logica di dominio

- Invariante chiave: **una famiglia per utente**. Applicata in `CreateFamilyHandler` tramite il controllo su `FindByUserIdAsync` (più eventuale `DuplicateEntityException` dal repository).
- Regola di **ownership**: `FamilyOwnershipService.EnsureOwnershipAsync` recupera la famiglia dell'utente (`GetCurrentFamilyAsync`) e verifica che `family.Id == familyId` richiesto; altrimenti `FamilyAccessResult.Unauthorized` (loggato come warning). Questo impedisce che un utente operi su una famiglia non sua anche se ne indovina l'id.
- Le entità (`Family : BaseDeletableEntity<Guid>`) sono essenzialmente contenitori di dati (Anemic Domain Model): la logica è negli handler/service.

## 5. Accesso ai dati

- Repository in `Core.PostgreSql/FamilyFeature`: `FamilyRepository` (`FindByUserIdAsync`, `CreateAsync`), `FamilyMemberRepository`, `FamilyServiceRepository`, `KinHubServiceRepository` (`GetAllAsync`). Persistenza su `CoreDbContext`.
- Scritture: in creazione famiglia si eseguono **più `CreateAsync` separati** (famiglia, ogni membro, ogni FamilyService) — ognuno con il proprio `SaveChanges` sottostante. **Non c'è una transazione esplicita** che avvolga l'intera creazione (vedi Anti-pattern).
- Il repository base `PostgreSqlRepository<TEntity,TDomain,TKey>` usa **Mapster** per mappare dominio↔entità e lancia `EntityNotFoundException` se l'update/delete non trova la riga.

## 6. Integrazioni esterne

- Per la **stessa** `Identity.Api` non ci sono integrazioni esterne: la risoluzione del contesto famiglia è locale via `IdentityFamilyContextResolver` (interroga il repository).
- Per gli **altri host** (KinRecipe/KinList) il contesto famiglia è un'integrazione HTTP: `RemoteFamilyOwnershipService`/`RemoteFamilyContextResolver` chiamano `GET api/access/family-context` sull'Identity.Api con l'header `Authorization` inoltrato. Vedi anche [flusso-gestione-ricette.md](./flusso-gestione-ricette.md) e [flusso-kinlist.md](./flusso-kinlist.md).

## 7. Gestione errori

- Il flusso di risoluzione contesto è **fail-closed**: `JwtAuthenticationMiddleware.TrySetFamilyContextAsync` salva l'esito (`FamilyContextOutcome`) in `HttpContext.Items`; se la policy famiglia fallisce, `FamilyAuthorizationMiddlewareResultHandler` distingue:
  - non autenticato → 401 `authentication_required`;
  - contesto non risolvibile perché a monte indisponibile → 503 `family_context_unavailable`;
  - utente senza famiglia → 403 `family_required`.
- Gli handler traducono `DuplicateEntityException` → Conflict, `EntityNotFoundException` → NotFound, `DomainException` → UnexpectedError; il mapping HTTP finale è in `HttpResultMapper` (Unauthorized→403, NotFound→404, Conflict→409, …).

## 8. Output finale

- Creazione: `Family` + `FamilyMember` (proprietario e aggiuntivi) + N `FamilyService` persistiti; 201 con `CreateFamilyResponse`.
- Mutazioni: entità membro/famiglia aggiornate o marcate eliminate; risposta 200/201.
- Side effect trasversale: una volta creata la famiglia, il `family-context` inizia a risolversi con successo, sbloccando l'accesso a ricette e liste.

# Pattern correttamente implementati

- **Repository Pattern** — interfacce nel Domain (`IFamilyRepository`, ecc.) implementate in `Core.PostgreSql`; il Business non conosce EF Core. *Correttezza*: separazione netta persistenza/logica, con un metodo di dominio significativo (`FindByUserIdAsync`) anziché solo CRUD generico.

- **Domain Service (ownership)** — `FamilyOwnershipService` incapsula la regola "l'utente possiede questa famiglia" ed espone un risultato ricco (`FamilyAccessResult` con `ToResult<T>()`). *Perché corretto*: la regola è riusata da più handler (`AddFamilyMemberHandler`, update/delete) senza duplicazione ed è testabile in isolamento.

- **Policy-based Authorization + Requirement/Handler** — `FamilyContextRequirement` + `FamilyContextAuthorizationHandler` + `FamilyAuthorizationMiddlewareResultHandler`. *Correttezza*: usa l'infrastruttura standard di ASP.NET Core; l'handler di risultato mappa i fallimenti in problem detail semanticamente corretti (401/403/503) invece di un generico 403.

- **Application Service / Facade** — `KinHubFamilyService` come punto unico per il controller. *Correttezza*: firma stabile, deleghe pulite agli handler.

- **Result Pattern** — `Result<T>` + `FamilyAccessResult` per esiti tipizzati senza eccezioni di controllo di flusso.

# Anti-pattern

- **Mancanza di transazione nella creazione famiglia** — *File*: `CreateFamilyHandler.HandleAsync`. La famiglia, i membri e gli N `FamilyService` sono creati con `CreateAsync` separati, ciascuno con il proprio `SaveChanges`, senza una transazione unica. *Problema*: un errore a metà (es. dopo aver creato la famiglia ma prima dei servizi) lascia dati **parzialmente inseriti** e incoerenti. *Impatto*: integrità dei dati. *Gravità*: media/alta. *Direzione*: avvolgere l'intera creazione in una transazione (come già fatto in KinList con `IKinListTransactionExecutor`).

- **N+1 query nella creazione dei servizi** — *File*: `CreateFamilyHandler` (loop `foreach (service in allServices) CreateAsync(...)`) e loop analoghi sui membri. *Problema*: una `INSERT` (con round-trip) per ogni servizio/membro invece di un inserimento batch. *Impatto*: performance sulla creazione. *Gravità*: bassa (operazione poco frequente). *Direzione*: inserimento in blocco / `AddRange` + un solo `SaveChanges`.

- **Anemic Domain Model** — *File*: `Family.cs`, `FamilyMember.cs`, ecc. Le entità sono soli contenitori di proprietà; tutte le regole (una-famiglia-per-utente, ownership) vivono negli handler/service. *Problema*: il dominio non protegge da sé le proprie invarianti. *Impatto*: la coerenza dipende dalla disciplina del layer applicativo. *Gravità*: bassa (scelta stilistica diffusa e coerente in tutto il progetto). *Direzione*: eventuali metodi/factory di dominio per incapsulare le invarianti.

- **Duplicazione della logica di ownership** — la verifica "family.Id != familyId → Unauthorized" esiste sia in `FamilyOwnershipService.EnsureOwnershipAsync` sia, in forma remota, in `RemoteFamilyOwnershipService.EnsureOwnershipAsync`. *Problema*: due implementazioni della stessa regola in host diversi. *Impatto*: rischio di divergenza. *Gravità*: bassa (giustificata dalla separazione local/remote). *Direzione*: test di caratterizzazione condivisi per mantenerle allineate (in parte già presenti nei test).
