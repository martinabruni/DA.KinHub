# Descrizione generale

Il backend documentato appartiene alla soluzione **Kin.KinHub** (file di soluzione `Kin.KinHub.Core.slnx`). Si tratta del lato server di una piattaforma familiare ("KinHub") che permette a un nucleo familiare di gestire in modo condiviso alcune aree funzionali: l'identità/account degli utenti, la definizione della famiglia e dei suoi membri, un ricettario con assistente basato su intelligenza artificiale, e delle liste condivise (KinList) che possono essere popolate anche dettando un messaggio vocale.

Il backend **non è un singolo servizio monolitico**, ma un **monolite modulare** organizzato per *bounded context* (contesti applicativi separati) ed esposto attraverso più host eseguibili. Ogni host è un progetto ASP.NET Core distinto:

- **`Kin.KinHub.Identity.Api`** — espone autenticazione, account utente, OAuth 2.0 (Authorization Code + PKCE) e la gestione delle famiglie. È l'unico servizio che possiede la "verità" sulla famiglia dell'utente.
- **`Kin.KinHub.KinRecipe.Api`** — espone il ricettario (libri, ricette, ingredienti, passi, frigorifero) e l'assistente ricette AI.
- **`Kin.KinHub.KinList.Api`** — espone le liste condivise (KinList) e il ciclo di vita delle operazioni audio.
- **`Kin.KinHub.KinList.AudioWorker`** — un *worker* di background (non un'API HTTP) che consuma una coda Azure e trasforma gli audio caricati in bozze di lista.
- **`Kin.KinHub.Migrations.Runner`** — un eseguibile una-tantum che applica le migrazioni EF Core di tutti i database prima del deploy delle API.

Concettualmente ogni contesto (Core, Identity, KinList) è organizzato secondo una **Clean Architecture a quattro anelli**: `Domain` (entità e contratti di repository), `Business` (casi d'uso applicativi, cioè handler e service), `Infrastructure` (implementazioni concrete: PostgreSQL, JWT, OpenAI, Azure Storage) e `Presentation` (le API e i worker). Le dipendenze puntano sempre verso l'interno: la Presentation conosce il Business, il Business conosce il Domain, e l'Infrastructure implementa le interfacce definite dal Domain/Business. Il progetto trasversale **`Kin.KinHub.Shared.Api`** contiene i mattoni condivisi da tutte le API (middleware JWT, autorizzazione per famiglia, mapping degli errori HTTP, validazione). Il progetto **`Kin.KinHub.Shared.Kernel`** contiene i tipi fondamentali condivisi a livello di kernel (interfacce `IResult`, enum `ResultStatus`, eccezioni di dominio base).

Il ruolo complessivo del backend è quindi quello di:

1. Autenticare gli utenti ed emettere token di accesso JWT firmati (contesto Identity).
2. Autorizzare ogni richiesta non solo per utente autenticato ma anche per **contesto famiglia** (una policy trasversale che risolve a quale famiglia appartiene l'utente).
3. Offrire operazioni CRUD e flussi assistiti da AI sui dati di dominio (ricette, liste) esponendo risposte JSON e problemi in formato RFC 9457 (`application/problem+json`).
4. Integrare servizi cloud esterni (Azure OpenAI, Azure AI Speech, Azure Blob/Queue Storage, Azure Monitor).

# Flussi

Di seguito le macro feature backend individuate analizzando il codice. Il dettaglio completo di ciascuna è nei documenti dedicati.

- **Autenticazione e Identità** — Registrazione, login via OAuth 2.0 Authorization Code + PKCE, emissione/refresh dei token JWT, gestione profilo utente e provider di identità collegabili. Coinvolge `Kin.KinHub.Identity.*` e l'infrastruttura JWT.
  [flusso-autenticazione.md](./flusso-autenticazione.md)

- **Gestione Famiglie** — Creazione della famiglia, gestione dei membri, attivazione/disattivazione dei servizi KinHub per la famiglia, e risoluzione del "contesto famiglia" usato come autorizzazione trasversale. Coinvolge `Core.Business/FamilyFeature`, `Identity.Api/FamilyFeature` e l'autorizzazione condivisa.
  [flusso-gestione-famiglie.md](./flusso-gestione-famiglie.md)

- **Gestione Ricette** — CRUD su libri di ricette, ricette, ingredienti, passi e frigorifero, con controllo di accesso basato sulla famiglia proprietaria. Coinvolge `Core.Business/RecipeFeature` e `KinRecipe.Api/RecipeFeature`.
  [flusso-gestione-ricette.md](./flusso-gestione-ricette.md)

- **Assistente Ricette AI** — Parsing di ricette da testo libero, suggerimento di nuove ricette a partire dal frigorifero, adattamento di una ricetta a vincoli (allergie, diete), e calcolo degli ingredienti mancanti tramite embedding. Coinvolge `Core.Business/RecipeAssistantFeature` e `Core.OpenAi`.
  [flusso-assistente-ricette-ai.md](./flusso-assistente-ricette-ai.md)

- **KinList (Liste condivise e pipeline audio)** — Liste condivise per famiglia con controllo di concorrenza ottimistica (ETag/If-Match), idempotenza sulle creazioni, e una pipeline asincrona che trascrive un audio con Azure AI Speech e ne estrae gli item con Azure OpenAI. Coinvolge `KinList.*` (Business, Domain, PostgreSql, Ai, AzureStorage), `KinList.Api` e `KinList.AudioWorker`.
  [flusso-kinlist.md](./flusso-kinlist.md)

# Architettura

## Struttura dei layer, progetti e cartelle

Il codice sorgente vive sotto `src/` ed è raggruppato in sei famiglie di progetti, ciascuna corrispondente a un anello architetturale o a un livello trasversale:

- **`src/Domains/`** — il cuore del dominio, senza dipendenze da framework di infrastruttura.
  - `Kin.KinHub.Core.Domain` (Family, Recipe, RecipeAssistant)
  - `Kin.KinHub.Identity.Domain` (Authentication)
  - `Kin.KinHub.KinList.Domain` (KinList)
  - Contiene entità (es. `Family`, `Recipe`, `KinUser`, `KinList`, `AudioProcessingOperation`), interfacce di repository (`IFamilyRepository`, `IKinListRepository`, …), interfacce di dominio (es. `IIdentityProvider`, `IRecipeAssistantService`, `ITokenGenerator`) ed eccezioni di dominio specifiche per contesto (`DomainException`, `DomainValidationException`, `EntityNotFoundException`, `DuplicateEntityException`).

- **`src/Businesses/`** — il livello applicativo (use case).
  - `Kin.KinHub.Core.Business`, `Kin.KinHub.Identity.Business`, `Kin.KinHub.KinList.Business`
  - Contiene handler di comando/query (es. `CreateFamilyHandler`, `RegisterUserHandler`), *application service* facciata (es. `KinHubFamilyService`, `KinHubAuthenticationService`, `KinListService`), i modelli di richiesta/risposta (DTO) e il tipo `Result<T>` con l'enum `ResultStatus` che rappresenta l'esito applicativo senza usare eccezioni per il controllo di flusso. I tipi base (`IResult`, `ResultStatus`) risiedono nel kernel condiviso (`Shared.Kernel`) e sono specializzati in ogni Business.

- **`src/Infrastructures/`** — le implementazioni concrete delle interfacce di Domain/Business.
  - Persistenza: `Kin.KinHub.Core.PostgreSql`, `Kin.KinHub.Identity.PostgreSql`, `Kin.KinHub.KinList.PostgreSql` (EF Core + Npgsql, con `DbContext`, entità di persistenza separate dalle entità di dominio, e migrazioni).
  - Sicurezza: `Kin.KinHub.Identity.Jwt` (generazione/validazione token, `CurrentUser`).
  - AI: `Kin.KinHub.Core.OpenAi` (embedding e assistente ricette), `Kin.KinHub.KinList.Ai` (trascrizione vocale + interpretazione).
  - Storage: `Kin.KinHub.KinList.AzureStorage` (Blob + Queue).

- **`src/Presentations/`** — gli host eseguibili. Le API sono *controller-based* (non Minimal API): `Identity.Api`, `KinRecipe.Api`, `KinList.Api`; più il worker `KinList.AudioWorker`, il runner `Migrations.Runner` e il progetto condiviso `Shared.Api` (che contiene `SharedHttpResultMapper`, middleware JWT, autorizzazione per famiglia, validazione). (I progetti `*.React` sono frontend e sono esclusi da questa analisi.)

- **`src/Shared/`** — il kernel condiviso trasversale a tutti i contesti.
  - `Kin.KinHub.Shared.Kernel`: contratto `IResult<T>`, enum `ResultStatus`, eccezioni di dominio base (`SharedDomainException`, `SharedDomainValidationException`). I domain specifici definiscono le proprie sottoclassi: ad esempio `Identity.Domain` ha `DomainException : SharedDomainException` e `DomainValidationException : DomainException` (la catena di ereditarietà è `DomainValidationException → DomainException → SharedDomainException`). Nessuna dipendenza da framework applicativi.

- **`src/Tests/Kin.KinHub.Core.Test`** — un unico progetto di test xUnit che referenzia tutte le API e infrastrutture (test di integrazione con `WebApplicationFactory` e test unitari).

## Direzione delle dipendenze

La regola delle dipendenze della Clean Architecture è rispettata: `Presentation → Business → Domain`, con `Infrastructure → Domain/Business` (l'infrastruttura implementa interfacce definite più all'interno). Ciò è verificabile nei `.csproj`: ad esempio `Kin.KinHub.Core.PostgreSql` referenzia solo `Kin.KinHub.Core.Domain`; `Kin.KinHub.KinRecipe.Api` referenzia Business, Domain, le infrastrutture e `Shared.Api`. Il Domain non referenzia mai EF Core, ASP.NET o gli SDK Azure. `Shared.Kernel` è la dipendenza più bassa: può essere referenziato da tutti gli altri layer senza inversioni.

Un punto architetturale importante: i tre contesti sono **fisicamente separati anche a livello di database logico** (schemi/DbContext distinti) ma **condividono un'unica stringa di connessione** `KinHub`. La `KinRecipe.Api` e la `KinList.Api` **non accedono direttamente ai dati di famiglia**: li ottengono via HTTP dalla `Identity.Api` (vedi `RemoteFamilyOwnershipService` e `RemoteFamilyContextResolver`). Questo mantiene Identity come unica sorgente di verità sulla famiglia.

## Pattern architetturali e applicativi usati

- **Clean/Layered Architecture** con separazione Domain/Business/Infrastructure/Presentation (presente in modo netto e coerente).
- **Repository Pattern** — interfacce nel Domain (`IRepository<TModel,TKey>` e repository specifici), implementazioni in `*.PostgreSql`. Nel Core esiste una base generica `PostgreSqlRepository<TEntity,TDomain,TKey>` che usa Mapster per mappare entità↔dominio.
- **Application Service / Facade** — service come `KinHubFamilyService`, `KinHubAuthenticationService`, `KinListService` fanno da facciata verso un insieme di handler.
- **Command/Query Handler** — organizzazione per feature con cartelle `Commands/` e `Queries/` e handler dedicati (es. `CreateRecipeHandler`, `GetFamilyHandler`). **Non** è presente un vero *Mediator* (nessun MediatR): gli handler sono invocati direttamente dai service facciata, quindi il pattern CQRS/Mediator è solo parzialmente presente (separazione dei comandi ma senza dispatcher).
- **Result Pattern** — `Result<T>` + `ResultStatus` (da `Shared.Kernel`) per veicolare successo/errore applicativo, poi tradotto in HTTP da `SharedHttpResultMapper` (centralizza il mapping comune) con specializzazioni per contesto (`HttpResultMapper`, `IdentityHttpResultMapper`, `KinListHttpResultMapper`).
- **Transaction Executor** — `ICoreTransactionExecutor`/`EfCoreTransactionExecutor` (Core) e `IKinListTransactionExecutor`/`EfKinListTransactionExecutor` (KinList) avvolgono le scritture multi-entità in transazioni EF con execution strategy per retry automatico sui transitori di database.
- **Options Pattern** — classi `*Options`/`*Settings` (es. `JwtOptions`, `KinListOptions`, `OAuthServerOptions`) popolate dalla configurazione con validazione esplicita (`Validate()`).
- **Strategy/Registry** — `IIdentityProvider` + `IdentityProviderRegistry` permettono di aggiungere provider di identità (oggi solo password).
- **Adapter** — `RemoteFamilyOwnershipService` adatta l'API Identity a `IFamilyOwnershipService`; gli SDK Azure sono incapsulati dietro interfacce (`IKinListSpeechTranscriber`, `IAudioProcessingBlobStorage`).
- **Pipeline/Middleware** — middleware ASP.NET (`JwtAuthenticationMiddleware`) + handler di autorizzazione custom.
- **Background Worker** — `AudioProcessingWorkerService` (BackgroundService che consuma una coda) e `IdempotencyRecordCleanupService` (pulizia periodica).
- **Null Object** — implementazioni "Unavailable" (`UnavailableAudioProcessingQueue`, `UnavailableKinListAudioDraftGenerator`, `NoOpKinListTransactionExecutor`) registrate come default quando l'infrastruttura reale non è presente.

## Configurazione, DI, logging, validazione, error handling, autorizzazione, persistenza, integrazioni

- **Configurazione** — ogni `Program.cs` aggiunge le variabili d'ambiente con prefisso `KINHUB_` e le API leggono sezioni tipizzate (`Jwt`, `Cors`, `OAuth`, `OpenAi`, `FamilyContextApi`, `KinList`, `SpeechToText`, `AudioStorage`). In produzione la validazione è *fail-fast*: `ValidateProductionSecurity` in `ServiceCollectionExtensions` blocca l'avvio se mancano segreto JWT ≥ 32 caratteri, issuer, audience, origini CORS esplicite o HTTPS.
- **Dependency Injection** — il container nativo di .NET; ogni progetto espone metodi di estensione `AddKinHub…` che registrano i propri servizi (es. `AddKinHubFamilyBusiness`, `AddKinHubKinListBusiness`). I servizi di caso d'uso sono in genere `Scoped`.
- **Logging & Telemetria** — `ILogger<T>` diffuso; OpenTelemetry verso Azure Monitor abilitato solo se è configurata la connection string di Application Insights. La pipeline audio ha telemetria dedicata (`KinListAudioTelemetry`, `ActivitySource`) e propagazione del `correlationId`.
- **Validazione** — FluentValidation: validator per ogni request (es. `CreateFamilyRequestValidator`), invocati tramite l'astrazione `IRequestValidator<T>` (`FluentRequestValidator<T>`) nei controller, prima di chiamare il Business.
- **Error handling** — nessun controllo di flusso via eccezioni verso l'esterno: gli handler catturano le eccezioni di dominio e ritornano `Result<T>` con lo stato adeguato; i controller traducono il `Result` in `IActionResult` con corpo `ProblemDetails` (RFC 9457) tramite `ApiProblemDetails`/`SharedHttpResultMapper` (e le sue specializzazioni).
- **Autenticazione/Autorizzazione** — JWT Bearer con parametri di validazione severi (`ClockSkew = 0`); una `DefaultPolicy` che richiede utente autenticato **e** scope OAuth `read`; una policy `FamilyContext` che richiede la risoluzione del contesto famiglia. I controller applicano `[Authorize]` o `[Authorize(Policy = FamilyContextRequirement.PolicyName)]` a livello di classe, senza guardie inline per azione. La risoluzione avviene nel `JwtAuthenticationMiddleware`, che popola `CurrentUser` e, in caso di fallimento, distingue 401/403/503 tramite `FamilyAuthorizationMiddlewareResultHandler` (*fail-closed*).
- **Persistenza** — EF Core + Npgsql; entità di persistenza separate dalle entità di dominio, mapping manuale (KinList/Identity) o via Mapster (Core). Le migrazioni sono applicate dal `Migrations.Runner`. Sia Core che KinList usano transazioni esplicite: `EfCoreTransactionExecutor` (Core) e `EfKinListTransactionExecutor` (KinList); le creazioni aggregate (famiglia, ricetta) sono avvolte in `ICoreTransactionExecutor.ExecuteAsync` con inserimento batch (`CreateRangeAsync`/`AddRangeAsync`). KinList usa inoltre una `UPDATE … WHERE Status = Queued` per il claim atomico delle operazioni.
- **Integrazioni esterne** — Azure OpenAI (chat + embedding), Azure AI Speech (trascrizione), Azure Blob Storage (upload audio con SAS via User Delegation Key), Azure Queue Storage (coda di processing + poison queue), Azure Monitor.

## Punti tecnici da conoscere per non "rompere" il backend

- **Il contesto famiglia non arriva mai dal client**: `FamilyId` è impostato su `CurrentUser` solo dal server dopo una risoluzione via repository (Identity) o via HTTP (Recipe/KinList). Non leggerlo da JWT, route o body.
- **Result Pattern e `ResultStatus` unificati**: i tipi base (`IResult`, `ResultStatus`) vivono in `Shared.Kernel`; aggiungere un nuovo `ResultStatus` richiede di aggiornare `SharedHttpResultMapper` e tutte le sue specializzazioni (`HttpResultMapper`, `IdentityHttpResultMapper`, `KinListHttpResultMapper`), altrimenti si finisce nel ramo di default (500).
- **Transazioni nelle creazioni aggregate Core**: `CreateFamilyHandler` e `CreateRecipeHandler` avvolgono l'intera creazione (entità principale + figli + servizi collegati) in `ICoreTransactionExecutor.ExecuteAsync` con inserimento batch; non separare queste scritture dalla transazione.
- **Idempotenza ed ETag in KinList**: le mutazioni di lista richiedono l'header `If-Match` e la creazione richiede `Idempotency-Key`; rimuovere questi controlli rompe il contratto di concorrenza.
- **Separazione dei grafi DI**: `Identity.Api` registra solo `AddKinHubFamilyBusiness` (non l'intero Core), quindi ricette/assistente non sono presenti lì; non assumere che tutti i service siano disponibili in ogni host.
- **Fallback "Unavailable"**: se un'infrastruttura opzionale (coda, storage, generatore audio) non viene registrata, il default è un'implementazione che fallisce in modo controllato; registrare l'implementazione reale è responsabilità del `Program.cs` dell'host.

# Stack tecnologico

- **.NET 10 / C#** — tutti i progetti hanno `TargetFramework net10.0`, con `Nullable` e `ImplicitUsings` abilitati. È lo stack runtime e linguaggio di base.
- **ASP.NET Core (Web API controller-based)** — framework web per le tre API; usa `Microsoft.AspNetCore.OpenApi` per la generazione OpenAPI in sviluppo.
- **Microsoft.Extensions.Hosting (Worker/BackgroundService)** — per `KinList.AudioWorker` e per i servizi di background (`IdempotencyRecordCleanupService`).
- **Entity Framework Core 10 + Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`)** — ORM e provider PostgreSQL. Ogni contesto ha il proprio `DbContext` e le proprie migrazioni.
- **PostgreSQL** — database relazionale (unica connection string `KinHub`, schemi separati per contesto).
- **Pgvector** — estensione/pacchetto per memorizzare embedding vettoriali degli ingredienti (usato dall'assistente ricette per la similarità).
- **Mapster** — libreria di mapping oggetto-oggetto, usata nel repository base del Core (`PostgreSqlRepository`) e in alcune conversioni DTO dell'assistente.
- **FluentValidation (11.11)** — validazione dichiarativa delle request; integrata in DI con `AddValidatorsFromAssemblyContaining`.
- **JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`)** — autenticazione basata su token firmati HMAC-SHA256; server OAuth 2.0 custom (Authorization Code + PKCE S256) implementato in `OAuthController` + helper (`OAuthRequestValidator`, `OAuthSessionManager`, `OAuthTokenIssuer`, `OAuthLoginPageRenderer`).
- **Azure.AI.OpenAI (2.1.0)** — client per Azure OpenAI: chat completion (parsing/suggerimento/adattamento ricette e interpretazione audio) ed embedding.
- **Azure.AI.Speech.Transcription (1.0.0)** — trascrizione vocale (speech-to-text) per la pipeline audio di KinList.
- **Azure.Storage.Blobs / Azure.Storage.Queues / Azure.Identity** — upload degli audio su Blob con SAS (User Delegation Key), coda di processing e poison queue, autenticazione tramite `DefaultAzureCredential` (Managed Identity) o chiave.
- **Azure.Monitor.OpenTelemetry.AspNetCore** — telemetria/tracing verso Application Insights (attivata solo se configurata).
- **AspNetCore.HealthChecks.Npgsql** — health check `/health` e `/health/ready` con verifica della connessione al database.
- **xUnit + Microsoft.AspNetCore.Mvc.Testing + Xunit.SkippableFact + coverlet** — framework di test (unitari e di integrazione end-to-end via `WebApplicationFactory`) e coverage.
- **Rate limiting nativo (`Microsoft.AspNetCore.RateLimiting`)** — applicato agli endpoint OAuth.

> Nota: dove un dettaglio non è deducibile con certezza dal codice (ad esempio la topologia di deploy cloud o i valori di configurazione runtime), va considerato **non deducibile con certezza dalla codebase analizzata** e verificato sugli ambienti reali.
