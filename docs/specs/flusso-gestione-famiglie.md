# Descrizione generale

La macro feature **Gestione Famiglie** modella il concetto centrale di KinHub: ogni utente possiede **una** famiglia, la famiglia ha dei **membri** (profili, non account) e un insieme di **servizi KinHub** attivabili/disattivabili (es. KinList). Oltre alle operazioni CRUD sulla famiglia, questa feature fornisce un meccanismo trasversale fondamentale: la **risoluzione del contesto famiglia**, cioè come il sistema stabilisce a quale famiglia appartiene l'utente autenticato per autorizzare le richieste negli altri servizi.

Cosa fa:

- Crea una famiglia per l'utente (con il profilo del proprietario e membri aggiuntivi), attivando di default tutti i servizi del catalogo.
- Aggiunge/aggiorna/rimuove membri.
- Aggiorna i dati della famiglia e la elimina.
- Attiva/disattiva i servizi KinHub per la famiglia (`ServicesFunctions`).

Perché esiste: la famiglia è l'**unità di autorizzazione** del sistema. Ricette e liste non appartengono a un singolo utente ma alla famiglia; quindi serve un punto unico che sappia mappare `userId → familyId`.

**Nota architetturale (post-refactor):** questa feature appartiene al dominio **Core**, non a Identity. È ospitata come Azure Function HTTP-trigger dentro `Kin.KinHub.App.Functions` (route `api/families`, `api/services`), non più dentro `Kin.KinHub.Identity.Api`. Identity.Api non referenzia `Core.Business`/`Core.PostgreSql` e non conosce lo schema `core`. App.Functions possiede direttamente `CoreDbContext` (via `AddKinHubCorePostgreSqlInfrastructure` + `AddKinHubFamilyBusiness`, cablati una sola volta in `AddKinHubAppFunctions`).

Parti coinvolte:

- **Presentation** — `FamilyFunctions`, `ServicesFunctions` (`Kin.KinHub.App.Functions/FamilyFeature`), i validator (`CreateFamilyRequestValidator`, `AddFamilyMemberRequestValidator`, …, nella stessa cartella), `CoreFamilyContextResolver` (`App.Functions/Common`) che implementa `IFamilyContextResolver` risolvendo il contesto famiglia **in-process** tramite `IFamilyOwnershipService`, e `FunctionsAuthorizationService` (`App.Functions/Common`) che sostituisce il vecchio middleware/policy ASP.NET con `EnsureAuthenticatedAsync`/`EnsureFamilyContextAsync` invocati esplicitamente da ogni Function.
- **Business** — `KinHubFamilyService` (facciata), gli handler in `Core.Business/FamilyFeature/Interfaces` e le rispettive implementazioni in `Commands`/`Queries`, `FamilyOwnershipService`, `KinHubServiceService`.
- **Domain** — entità `Family`, `FamilyMember`, `FamilyService`, `KinHubService`; interfacce `IFamilyRepository` (con `FindByUserIdAsync`), `IFamilyMemberRepository`, `IFamilyServiceRepository`, `IKinHubServiceRepository`; enum `KinHubServiceType`.
- **Infrastructure** — repository in `Kin.KinHub.Core.PostgreSql/FamilyFeature`.

Dati ricevuti: `CreateFamilyRequest` (nome famiglia, nome profilo proprietario, membri aggiuntivi), `AddFamilyMemberRequest`, `UpdateFamilyRequest`, `UpdateFamilyMemberRequest`, `ToggleFamilyServiceRequest`. Dati prodotti: `CreateFamilyResponse` (familyId, ownerMemberId), `FamilyDetailResponse`, `AddFamilyMemberResponse`, DTO dei servizi.

Dipendenze: EF Core/Npgsql per la persistenza, FluentValidation, l'autorizzazione ASP.NET Core (policy + handler custom).

# Casi d'uso

- **Creazione famiglia** — _Obiettivo_: creare l'unica famiglia dell'utente. _Attore_: utente autenticato senza famiglia. _Input_: `CreateFamilyRequest`. _Output_: 201 con `CreateFamilyResponse`. _Condizione/errore_: se l'utente ha già una famiglia → 409 Conflict ("A family already exists for this user.").
- **Lettura della propria famiglia** — _Obiettivo_: ottenere famiglia + membri + servizi. _Input_: solo il token. _Output_: `FamilyDetailResponse`. _Errore_: nessuna famiglia → 404.
- **Aggiunta/aggiornamento/rimozione membro** — _Attore_: proprietario della famiglia. _Input_: `familyId` in route + payload. _Output_: id membro creato o esito. _Condizione_: richiede la policy `FamilyContext` **e** la verifica di proprietà (`EnsureOwnershipAsync`). _Errore_: famiglia non posseduta → 403.
- **Aggiornamento/eliminazione famiglia** — _Attore_: proprietario. _Condizione_: policy `FamilyContext` + ownership. _Output_: esito.
- **Attivazione/disattivazione servizio** — _Obiettivo_: abilitare o meno un servizio KinHub (es. KinList) per la famiglia. _Input_: `ToggleFamilyServiceRequest`. _Gestito da_: `ServicesController` + `KinHubServiceService`.
- **Risoluzione contesto famiglia** — _Obiettivo (trasversale)_: dire "a quale famiglia appartiene l'utente". _Attore_: la pipeline di ogni API (via middleware) e gli altri host (via HTTP su `/api/access/family-context`).

# Flusso implementativo

## 1. Punto di ingresso

- CRUD famiglia: `FamilyFunctions` (Azure Function, `Kin.KinHub.App.Functions/FamilyFeature`) su `api/families` (`CreateAsync`, `GetAsync`, `AddMemberAsync`, `UpdateMemberAsync`, `DeleteMemberAsync`, `UpdateFamilyAsync`, `DeleteFamilyAsync`). Il proprietario/utente arriva da `FunctionsAuthorizationService.CurrentUser.UserId` (mai dal body).
- Servizi: `ServicesFunctions`.
- Contesto famiglia: risolto **in-process** da `CoreFamilyContextResolver` (implementa `IFamilyContextResolver` chiamando `IFamilyOwnershipService` direttamente, senza alcuna chiamata HTTP), invocato da `FunctionsAuthorizationService.EnsureFamilyContextAsync` all'inizio di ogni Function che lo richiede.

## 2. Validazione iniziale

- Null-check del body / esito della validazione → `ApiProblemDetails.InvalidRequestBody` / `ApiProblemDetails.Validation`, restituiti come `IActionResult` dalla Function.
- Validazione FluentValidation via `IRequestValidator<T>` (es. `CreateFamilyRequestValidator`), invocata tramite `FunctionsTriggerBase.ReadAndValidateAsync`.
- **Autorizzazione a due livelli**, applicata esplicitamente a inizio Function (non più via attributi ASP.NET Core):
  - `FunctionsAuthorizationService.EnsureAuthenticatedAsync` per creare/leggere la famiglia (basta un utente autenticato con scope `read`; qui l'utente potrebbe non avere ancora una famiglia).
  - `FunctionsAuthorizationService.EnsureFamilyContextAsync` per le operazioni sui membri/famiglia esistente: richiede che il contesto famiglia sia stato risolto con successo.
- **Verifica di proprietà** applicativa: gli handler di mutazione chiamano `IFamilyOwnershipService.EnsureOwnershipAsync(familyId, userId)`.

## 3. Orchestrazione applicativa

- `FamilyFunctions` → `IFamilyService` (`KinHubFamilyService`, facciata) → handler specifico.
- **Creazione** (`CreateFamilyHandler`): verifica assenza di famiglia esistente (`FindByUserIdAsync`), crea `Family`, poi avvolge l'intera creazione di membri e servizi in `ICoreTransactionExecutor.ExecuteAsync`. All'interno della transazione: crea il membro proprietario e i membri aggiuntivi in batch (`CreateRangeAsync`), poi legge **tutti** i `KinHubService` del catalogo e crea i `FamilyService` corrispondenti in batch (`CreateRangeAsync`) con `IsActive = true`. Restituisce `CreateFamilyResponse`.
- **Aggiunta membro** (`AddFamilyMemberHandler`): prima `EnsureOwnershipAsync`; se ok, crea il `FamilyMember`.
- **Lettura** (`GetFamilyHandler`): recupera famiglia + membri + servizi e li mappa in `FamilyDetailResponse`.
- La conversione tra entità e DTO avviene negli handler/service (nessun mapper centralizzato per questa feature).

## 4. Logica di dominio

- Invariante chiave: **una famiglia per utente**. Applicata in `CreateFamilyHandler` tramite il controllo su `FindByUserIdAsync` (più eventuale `DuplicateEntityException` dal repository).
- Regola di **ownership**: `FamilyOwnershipService.EnsureOwnershipAsync` recupera la famiglia dell'utente (`GetCurrentFamilyAsync`) e verifica che `family.Id == familyId` richiesto; altrimenti `FamilyAccessResult.Unauthorized` (loggato come warning). Questo impedisce che un utente operi su una famiglia non sua anche se ne indovina l'id.
- Le entità (`Family : BaseDeletableEntity<Guid>`) sono essenzialmente contenitori di dati (Anemic Domain Model): la logica è negli handler/service.

## 5. Accesso ai dati

- Repository in `Core.PostgreSql/FamilyFeature`: `FamilyRepository` (`FindByUserIdAsync`, `CreateAsync`, `CreateRangeAsync`), `FamilyMemberRepository`, `FamilyServiceRepository`, `KinHubServiceRepository` (`GetAllAsync`). Persistenza su `CoreDbContext`.
- Scritture: la creazione famiglia è eseguita in `ICoreTransactionExecutor.ExecuteAsync` (transazione EF con execution strategy per retry); i membri e i `FamilyService` sono creati con `CreateRangeAsync` (inserimento batch) dentro la stessa transazione, garantendo atomicità dell'intera operazione.
- Il repository base `PostgreSqlRepository<TEntity,TDomain,TKey>` usa **Mapster** per mappare dominio↔entità e lancia `EntityNotFoundException` se l'update/delete non trova la riga.

## 6. Integrazioni esterne

- Nessuna integrazione HTTP tra servizi per il contesto famiglia: da quando Family/Services vivono in `App.Functions`, la risoluzione è interamente **in-process** tramite `CoreFamilyContextResolver` → `IFamilyOwnershipService` → `CoreDbContext`. Non esistono più `RemoteFamilyOwnershipService`/`RemoteFamilyContextResolver` (erano l'integrazione HTTP verso Identity.Api, rimossi con questo refactor). Vedi anche [flusso-gestione-ricette.md](./flusso-gestione-ricette.md) e [flusso-kinlist.md](./flusso-kinlist.md), che nello stesso host risolvono il contesto famiglia allo stesso modo.

## 7. Gestione errori

- Il flusso di risoluzione contesto è **fail-closed**: `FunctionsAuthorizationService.EnsureFamilyContextAsync` chiama `CoreFamilyContextResolver.ResolveAsync` e distingue l'esito (`FamilyContextOutcome`):
  - non autenticato → 401 `authentication_required`;
  - contesto non risolvibile → 503 `family_context_unavailable`;
  - utente senza famiglia → 403 `family_required`.
- Gli handler traducono `DuplicateEntityException` → Conflict, `EntityNotFoundException` → NotFound, `DomainException` → UnexpectedError; il mapping HTTP finale è in `HttpResultMapper` (Unauthorized→403, NotFound→404, Conflict→409, …).

## 8. Output finale

- Creazione: `Family` + `FamilyMember` (proprietario e aggiuntivi) + N `FamilyService` persistiti in un'unica transazione; 201 con `CreateFamilyResponse`.
- Mutazioni: entità membro/famiglia aggiornate o marcate eliminate; risposta 200/201.
- Side effect trasversale: una volta creata la famiglia, il `family-context` inizia a risolversi con successo, sbloccando l'accesso a ricette e liste.

# Pattern correttamente implementati

- **Repository Pattern** — interfacce nel Domain (`IFamilyRepository`, ecc.) implementate in `Core.PostgreSql`; il Business non conosce EF Core. _Correttezza_: separazione netta persistenza/logica, con metodi orientati al dominio (`FindByUserIdAsync`, `CreateRangeAsync`) anziché solo CRUD generico.

- **Domain Service (ownership)** — `FamilyOwnershipService` incapsula la regola "l'utente possiede questa famiglia" ed espone un risultato ricco (`FamilyAccessResult` con `ToResult<T>()`). _Perché corretto_: la regola è riusata da più handler (`AddFamilyMemberHandler`, update/delete) senza duplicazione ed è testabile in isolamento.

- **Explicit Authorization Guard** — `FunctionsAuthorizationService.EnsureAuthenticatedAsync`/`EnsureFamilyContextAsync`, invocato a inizio di ogni Function (Azure Functions non ha pipeline di `[Authorize]` ASP.NET Core). _Correttezza_: centralizza la logica di autenticazione/family-context in un unico servizio riusato da tutte le Function (Family, KinList, Recipe), mappando i fallimenti in problem detail semanticamente corretti (401/403/503) invece di un generico 403.

- **Application Service / Facade** — `KinHubFamilyService` come punto unico per il controller. _Correttezza_: firma stabile, deleghe pulite agli handler.

- **Transaction Executor** — `ICoreTransactionExecutor`/`EfCoreTransactionExecutor` avvolgono la creazione famiglia in una transazione atomica con inserimento batch (`CreateRangeAsync`) per membri e servizi. _Correttezza_: elimina il rischio di inserimenti parziali e riduce i round-trip di database.

- **Result Pattern** — `Result<T>` + `FamilyAccessResult` per esiti tipizzati senza eccezioni di controllo di flusso.
