• Checkpoint

Aggiornamento 2026-06-30

Questo file contiene anche un checkpoint piu' recente relativo al piano "Kin List, Identity broker e rimozione MCP". Se una nuova sessione deve ripartire da qui, usare prima questa sezione e solo dopo il resto del file.

Checkpoint Piano Kin List / Identity broker / rimozione MCP

Stato completato in questa tranche

- [done] Rimossa la superficie MCP di runtime da `Kin.KinHub.Shared.Api`:
  - package `ModelContextProtocol.AspNetCore` rimosso
  - `MapMcp(...)` rimosso
  - policy/options/tool MCP rimossi
  - file rimossi sotto `src/Presentations/Kin.KinHub.Shared.Api/Common/Mcp`
  - file rimossi sotto `src/Presentations/Kin.KinHub.Shared.Api/McpFeature/Services/Tools`
- [done] Rinominata e separata la configurazione OAuth da MCP:
  - nuovo file `src/Presentations/Kin.KinHub.Shared.Api/Common/Configuration/OAuthServerOptions.cs`
  - `appsettings.json` aggiornato da sezione `Mcp` a sezione `OAuth`
  - scope ora:
    - `kinhub.api`
    - `kinhub.api.write`
    - `kinhub.api.admin`
- [done] Mantenuto e riallineato il flow OAuth Authorization Code + PKCE in:
  - `src/Presentations/Kin.KinHub.Shared.Api/AuthenticationFeature/Controllers/OAuthController.cs`
  - `src/Presentations/Kin.KinHub.Shared.Api/AuthenticationFeature/Controllers/OAuthMetadataController.cs`
- [done] Introdotto family context request-scoped risolto dal backend:
  - `X-Member-Id` non e' piu' usato come input autoritativo nel middleware
  - `JwtAuthenticationMiddleware` risolve la famiglia corrente via `IFamilyOwnershipService`
  - `ICurrentUser` / `CurrentUser` ora espongono `FamilyId` e `HasFamilyContext`
- [done] Aggiunto endpoint `GET /api/access/family-context` in:
  - `src/Presentations/Kin.KinHub.Shared.Api/AccessFeature/Controllers/AccessController.cs`
  - comportamento:
    - `200` con `familyId` se il contesto famiglia esiste
    - `403` con `code = family_required` se manca la famiglia
- [done] Introdotto `ProblemDetails` con `code` e `correlationId`:
  - nuovo file `src/Presentations/Kin.KinHub.Shared.Api/Common/ApiProblemDetails.cs`
  - `HttpResultMapper` aggiornato per poter restituire errori strutturati
- [done] Aggiornati i project file linkati di `Identity.Api` e `KinRecipe.Api` per includere i nuovi file condivisi necessari.
- [done] Sostituito il vecchio test MCP con test focalizzati su OAuth e family context:
  - rimosso `src/Tests/Kin.KinHub.Core.Test/McpIntegrationTests.cs`
  - aggiunto `src/Tests/Kin.KinHub.Core.Test/OAuthAndAccessIntegrationTests.cs`

Verifiche eseguite

- [done] `dotnet build Kin.KinHub.Core.slnx`
- [done] `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj`
- [done] `dotnet build src/Presentations/Kin.KinHub.Identity.Api/Kin.KinHub.Identity.Api.csproj`
- [done] `dotnet build src/Presentations/Kin.KinHub.Shared.Api/Kin.KinHub.Shared.Api.csproj`
- [done] `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj` dopo l'introduzione della sessione broker
- [done] `dotnet build src/Presentations/Kin.KinHub.Identity.Api/Kin.KinHub.Identity.Api.csproj` dopo l'allineamento `ProblemDetails`
- [done] `dotnet build src/Presentations/Kin.KinHub.Shared.Api/Kin.KinHub.Shared.Api.csproj` dopo l'allineamento `ProblemDetails`
- [done] `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj` con copertura aggiuntiva su auth `ProblemDetails`

Stato non ancora fatto del piano

- [partial] Broker Identity backend avviato:
  - client OAuth stabili configurabili via `OAuth:Clients`
  - sessione broker server-side con cookie `HttpOnly`
  - `GET /authorize` riusa la sessione esistente e salta il reinserimento credenziali
  - `POST /logout` centralizzato invalida cookie/sessione broker
  - token exchange authorization-code non restituisce piu' `refresh_token` al browser
  - login page OAuth aggiornata con link verso la registrazione Identity via `OAuth:RegistrationUiUrl`
  - resta da completare la policy `Secure`/cross-site per gli ambienti reali
- [done] Eliminazione del relay frontend cross-app basato su token in fragment/localStorage.
- [done] Client OAuth comune tra Core, Identity e KinRecipe:
  - nuovo modulo condiviso `src/Presentations/Kin.KinHub.Frontend.Shared/oauth`
  - flow Authorization Code + PKCE centralizzato per le tre SPA
  - callback dedicate `/oauth/callback` in `Core.React`, `Identity.React`, `KinRecipe.React`
  - access token mantenuto solo in memoria lato browser
  - rimosso uso browser-side di `refresh_token`
- [partial] Standardizzazione dei controller su RFC 9457 `ProblemDetails` avviata:
  - i controller Core/Family/Recipe condivisi ora passano dal mapper con `ProblemDetails`
  - aggiunto mapping `503 service_unavailable` in `HttpResultMapper`
  - riallineati anche `AuthController` e gli early-return inline (`401`, body nullo, validation) dei controller Shared.Api toccati in questa tranche
  - i payload OAuth su `/authorize`, `/token` e registrazione client restano volutamente nel formato OAuth standard, non `ProblemDetails`
- [done] `family-context` remoto tra servizi con fail-closed `503` quando Core non e' raggiungibile:
  - nuovo `RemoteFamilyOwnershipService` in `src/Presentations/Kin.KinHub.KinRecipe.Api/Common/RemoteFamilyOwnershipService.cs`
  - `KinRecipe.Api` propaga il bearer token a Core su `GET /api/access/family-context`
  - timeout/errori di rete/status inattesi verso Core diventano `503`
  - ownership mismatch continua a restituire `403`
  - configurazione backend aggiunta via `CoreApi:BaseUrl` e `CoreApi:TimeoutSeconds`
- [partial] Nuovo bounded context Kin List backend avviato:
  - nuovi progetti separati:
    - `src/Domains/Kin.KinHub.KinList.Domain`
    - `src/Businesses/Kin.KinHub.KinList.Business`
    - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql`
    - `src/Presentations/Kin.KinHub.KinList.Api`
  - nuovo host ASP.NET `KinList.Api` collegato alla soluzione
  - integrazione auth/family-context remota allineata a `KinRecipe.Api`
  - primo slice CRUD backend presente:
    - `GET /api/lists`
    - `GET /api/lists/{id}`
    - `POST /api/lists`
    - `PATCH /api/lists/{id}`
    - `DELETE /api/lists/{id}`
    - `POST /api/lists/{id}/restore`
    - mutazioni item principali sotto `/api/lists/{id}/items`
  - introdotti `ETag` / `If-Match` per le mutazioni del primo slice
  - introdotta `Idempotency-Key` per `POST /api/lists`
  - aggiunto endpoint bulk confirm `POST /api/lists/{id}/items/confirm`
  - aggiunti endpoint audio draft:
    - `POST /api/list-drafts/from-audio`
    - `POST /api/lists/{id}/item-drafts/from-audio`
  - limiti backend centralizzati via sezione config `KinList`
  - soft-delete / restore server-side presenti per liste e item
  - ordinamento base implementato:
    - item attivi prima dei completati
    - item riattivati tornano in cima
    - liste ordinate per `LastModifiedAt`
  - introdotto coordinamento transazionale server-side per le mutazioni multi-step di `KinList`
  - hardening idempotenza avviato con cleanup dei record scaduti prima del riuso della stessa chiave
  - `422 no_items_detected` supportato per i draft audio vuoti
  - test di business aggiunti in `src/Tests/Kin.KinHub.Core.Test/KinListServiceTests.cs`
  - migration EF reali generate:
    - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Migrations/20260630140545_InitialKinList.cs`
    - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Migrations/KinListDbContextModelSnapshot.cs`
  - pipeline audio draft Speech/OpenAI reale ora cablata in un'infrastruttura dedicata attivabile via config:
    - nuovo progetto `src/Infrastructures/Kin.KinHub.KinList.Ai`
    - trascrizione via Azure Speech Transcription
    - strutturazione via Azure OpenAI `gpt-4o-mini` con prompt versionato `kinlist-audio-v1`
    - audio/transcript/title/items non loggati dal nuovo layer
    - fallback fail-closed ancora mantenuto quando `Speech`/`OpenAi` non sono configurati
  - retry/transient fault handling esplicito aggiunto per il layer audio:
    - timeout configurabile
    - massimo 3 tentativi con exponential backoff + jitter su timeout/429/5xx
  - bootstrap/fallback della pipeline audio riallineato:
    - `KinList.Api` non prova piu' a registrare Azure Speech/OpenAI quando endpoint/key non sono configurati
    - configurazioni parziali `Speech`/`OpenAi` falliscono esplicitamente all'avvio
  - cleanup persistente dei record di idempotenza schedulato:
    - nuovo hosted service `IdempotencyRecordCleanupService`
    - purge globale degli expired via repository dedicato
    - intervallo configurabile via `KinList:IdempotencyCleanupIntervalMinutes`
  - restano da completare:
    - conferma completa delle proposte audio nel flusso finale SPA/backend
- [not_started] Nuova SPA Kin List.
- [not_started] Migrazioni dati, IaC e pipeline del piano esteso.

Prossimo step consigliato

1. Completare il backend `Kin List` oltre il primo slice: contratti audio mancanti, hardening di `ETag`/retry/idempotenza e transazioni infrastrutturali.
2. Collegare il flusso finale di conferma delle proposte audio tra SPA e backend `KinList`.
3. Creare la nuova SPA `Kin List` contro i contratti backend appena introdotti.
4. Rifinire il cleanup legacy `ShoppingList` da `KinRecipe/Core` solo dopo che il nuovo backend/frontend Kin List sono davvero sostitutivi.

Di seguito lo stato reale del repo a questo checkpoint.

- [done] Esplorazione iniziale completata: split points identificati tra Shared.Api, React app, EF/PostgreSQL e IaC.
- [done] Soluzione aggiornata con i nuovi host backend in Kin.KinHub.Core.slnx.

Backend

- [done] Creato host Identity in src/Presentations/Kin.KinHub.Identity.Api
- [done] File presenti:
  - src/Presentations/Kin.KinHub.Identity.Api/Kin.KinHub.Identity.Api.csproj
  - src/Presentations/Kin.KinHub.Identity.Api/Program.cs
  - src/Presentations/Kin.KinHub.Identity.Api/GlobalUsings.cs
  - src/Presentations/Kin.KinHub.Identity.Api/ServiceCollectionExtensions.cs
  - src/Presentations/Kin.KinHub.Identity.Api/WebApplicationExtensions.cs
  - src/Presentations/Kin.KinHub.Identity.Api/appsettings.json

- [done] Creato host KinRecipe in src/Presentations/Kin.KinHub.KinRecipe.Api
- [done] File presenti:
  - src/Presentations/Kin.KinHub.KinRecipe.Api/Kin.KinHub.KinRecipe.Api.csproj
  - src/Presentations/Kin.KinHub.KinRecipe.Api/Program.cs
  - src/Presentations/Kin.KinHub.KinRecipe.Api/GlobalUsings.cs
  - src/Presentations/Kin.KinHub.KinRecipe.Api/ServiceCollectionExtensions.cs
  - src/Presentations/Kin.KinHub.KinRecipe.Api/WebApplicationExtensions.cs
  - src/Presentations/Kin.KinHub.KinRecipe.Api/appsettings.json

- [done] Creato host KinList in `src/Presentations/Kin.KinHub.KinList.Api`
- [done] File presenti:
  - `src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj`
  - `src/Presentations/Kin.KinHub.KinList.Api/Program.cs`
  - `src/Presentations/Kin.KinHub.KinList.Api/GlobalUsings.cs`
  - `src/Presentations/Kin.KinHub.KinList.Api/ServiceCollectionExtensions.cs`
  - `src/Presentations/Kin.KinHub.KinList.Api/WebApplicationExtensions.cs`
  - `src/Presentations/Kin.KinHub.KinList.Api/appsettings.json`
- [done] Creati anche i nuovi progetti backend Kin List:
  - `src/Domains/Kin.KinHub.KinList.Domain/Kin.KinHub.KinList.Domain.csproj`
  - `src/Businesses/Kin.KinHub.KinList.Business/Kin.KinHub.KinList.Business.csproj`
  - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Kin.KinHub.KinList.PostgreSql.csproj`

- [done] I nuovi host riusano file del vecchio Shared.Api via Compile Include linkato.
- [done] Build dei nuovi host eseguita:
  - `dotnet build src/Presentations/Kin.KinHub.Identity.Api/Kin.KinHub.Identity.Api.csproj`
  - `dotnet build src/Presentations/Kin.KinHub.KinRecipe.Api/Kin.KinHub.KinRecipe.Api.csproj`
  - `dotnet build src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj`
- [done] Fix completati emersi dalla build:
  - aggiunto `IdentityDbContextFactory`
  - separato il mapping HTTP `Core` vs `Identity` per evitare dipendenze non necessarie in `KinRecipe.Api`
  - aggiunto il global using identity mancante in `KinRecipe.Api`
  - aggiunti i `global using` necessari al nuovo host `KinList.Api`
- [partial] Primo slice backend `Kin List` implementato:
  - modelli dominio per lista, item e record di idempotenza
  - repository PostgreSQL dedicati con `KinListDbContext`
  - service business con mapping response/ordering/soft-delete
  - controller API con `ProblemDetails`, `ETag`, `Idempotency-Key`, bulk confirm item e draft audio `multipart/form-data`
  - limiti configurabili registrati via `KinListOptions`, inclusi MIME/size audio
  - coordinamento transazionale EF introdotto per le mutazioni multi-step
  - cleanup dei record di idempotenza scaduti sul riuso della stessa chiave
  - cleanup persistente schedulato dei record di idempotenza scaduti via hosted service
  - test service-level per idempotent replay, record scaduti, riattivazione item, bulk confirm e draft audio/duplicati
  - provider audio di default fail-closed: i nuovi endpoint rispondono `503 audio_processing_unavailable` finche' `Speech`/`OpenAi` non sono configurati
- [not_started] Nessun Dockerfile/containerization aggiunto.
- [not_started] Nessuna conversione ad Azure Functions vera e propria; al momento i nuovi host sono ASP.NET separati, non function
  apps.

Frontend

- [done] Clonata la frontend app corrente in:
  - src/Presentations/Kin.KinHub.Identity.React
  - src/Presentations/Kin.KinHub.KinRecipe.React

- [done] `Kin.KinHub.Core.React` convertita in hub statico con cards e link verso le app split.
- [in_progress] `Kin.KinHub.Identity.React` ridotta verso il ruolo identity:
  - routing recipe rimosso
  - default route spostata su `/services`
  - service cards recipe-domain instradate verso `KinRecipe`
  - login/register/select-member aggiornati per propagare `returnTo`
  - callback OAuth dedicata aggiunta su `/oauth/callback`
- [in_progress] `Kin.KinHub.KinRecipe.React` ridotta verso il ruolo recipe:
  - routing family/services/login locali rimosso
  - redirect verso Identity su assenza auth
  - nav desktop/mobile riallineata alle sole feature recipe
  - callback OAuth dedicata aggiunta su `/oauth/callback`
- [done] Introdotti `VITE_IDENTITY_URL` / `VITE_KINRECIPE_URL` negli `.env.example` rilevanti.
- [done] Client OAuth comune introdotto tra `Core.React`, `Identity.React` e `KinRecipe.React`:
  - nuovo alias condiviso `@shared` verso `src/Presentations/Kin.KinHub.Frontend.Shared`
  - token store in memoria
  - `apiClient` riallineati al bearer in-memory
- [done] Relay cross-domain frontend rimosso:
  - rimossi `src/Presentations/Kin.KinHub.Core.React/src/lib/sessionRelay.ts`
  - rimossi `src/Presentations/Kin.KinHub.KinRecipe.React/src/lib/sessionRelay.ts`
  - rimosso il passaggio di token via `appendSessionToUrl(...)`
- [done] Build frontend eseguite:
  - `Core.React`: `npm run build` riuscita
  - `KinRecipe.React`: `npm run build` riuscita
- [done] `Identity.React` riportata verde:
  - fixato l'errore TypeScript residuo in `src/components/Sidebar.tsx`
  - build `npm run build` riuscita
- [done] Build frontend rieseguite dopo la migrazione OAuth comune:
  - `Core.React`: `npm run build` riuscita
  - `Identity.React`: `npm run build` riuscita
  - `KinRecipe.React`: `npm run build` riuscita
- [note] Per verificare localmente le due app clonate senza reinstallare dipendenze sono stati creati junction `node_modules` verso `src/Presentations/Kin.KinHub.Core.React/node_modules`.
- [note] Lo stato UI `activeMember` esiste ancora come stato locale di navigazione, ma non e' piu' coinvolto nel trasporto dei token e non e' input autoritativo lato backend.

EF / Database

- [done] Creati i design-time factory EF:
  - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/CoreDbContextFactory.cs
  - src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Models/IdentityDbContextFactory.cs
  - src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Models/KinListDbContextFactory.cs
- [done] Generate baseline migration code-first no-op:
  - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Migrations/20260626072328_CoreBaseline.cs
  - src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Migrations/20260626072350_IdentityBaseline.cs
  - snapshot EF generati per entrambi i DbContext
  - script verificati come metadata-only: creano solo `__EFMigrationsHistory` e registrano la migration
- [done] Generate migration EF reali del nuovo schema `kinlist`:
  - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Migrations/20260630140545_InitialKinList.cs`
  - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Migrations/20260630140545_InitialKinList.Designer.cs`
  - `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Migrations/KinListDbContextModelSnapshot.cs`
- [not_started] Nessuna pulizia del passaggio db-first -> code-first oltre al primo factory parziale

IaC / CI-CD

- [partial] `ops/iac/main.bicep` riscritto verso il target split:
  - 3 Static Web Apps (`Core`, `Identity`, `KinRecipe`)
  - 1 Container Apps environment Consumption
  - 2 container apps backend (`Identity`, `KinRecipe`) con immagini GHCR
  - PostgreSQL, Key Vault, Log Analytics, Application Insights, Azure OpenAI mantenuti nello stesso template
  - output separati per hostname frontend e URL backend
- [partial] Workflow backend aggiornata:
  - build/test soluzione .NET
  - packaging container immagini `Identity.Api` e `KinRecipe.Api`
  - deploy infra via Bicep con parametri per SWA multiple, Container Apps e GHCR
- [partial] Workflow frontend aggiornata:
  - build indipendente di `Core.React`, `Identity.React`, `KinRecipe.React`
  - deploy separati per ciascuna Static Web App
- [partial] Aggiunti Dockerfile per i backend split:
  - src/Presentations/Kin.KinHub.Identity.Api/Dockerfile
  - src/Presentations/Kin.KinHub.KinRecipe.Api/Dockerfile
- [note] Restano da valorizzare in GitHub Environments/Secrets le nuove variabili:
  - nomi SWA multipli, Container Apps environment, nomi container app, URL frontend/backend
  - token deploy SWA per app
  - credenziali GHCR usabili da Azure Container Apps

Stato Git

- [modified] Kin.KinHub.Core.slnx
- [modified] src/Presentations/Kin.KinHub.Shared.Api/Common/HttpResultMapper.cs
- [modified] docs/checklist.md
- [untracked] docs/checklist.md
- [untracked] src/Presentations/Kin.KinHub.Identity.Api
- [untracked] src/Presentations/Kin.KinHub.KinRecipe.Api
- [untracked] src/Presentations/Kin.KinHub.KinList.Api
- [untracked] src/Presentations/Kin.KinHub.Identity.React
- [untracked] src/Presentations/Kin.KinHub.KinRecipe.React
- [untracked] src/Domains/Kin.KinHub.KinList.Domain
- [untracked] src/Businesses/Kin.KinHub.KinList.Business
- [untracked] src/Infrastructures/Kin.KinHub.KinList.PostgreSql
- [untracked] src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/CoreDbContextFactory.cs
- [untracked] src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Models/IdentityDbContextFactory.cs
- [untracked] src/Presentations/Kin.KinHub.Shared.Api/Common/IdentityHttpResultMapper.cs
- [untracked] src/Tests/Kin.KinHub.Core.Test/KinListServiceTests.cs

Suggested Resume Order

1. Collegare la pipeline reale Azure Speech + OpenAI ai nuovi endpoint audio draft di `KinList`.
2. Rafforzare ulteriormente retry/transient fault handling e cleanup persistente dell'idempotenza.
3. Aprire la nuova SPA `Kin List` sui contratti del nuovo host `KinList.Api`.
4. Solo dopo: rimuovere ownership e UI legacy delle shopping list da `KinRecipe/Core` e proseguire con IaC/pipeline estese.

Important Note

Il precedente stop era avvenuto durante un apply_patch interrotto. Quel primo blocco backend e' chiuso; il frontend split ora ha `Core.React`, `Identity.React` e
`KinRecipe.React` buildabili. Anche il primo step code-first e' ora in repo tramite baseline migrations no-op. Il blocco IaC/CI e' stato avviato in modo sostanziale,
ma va ancora validato con secret/vars reali prima di poterlo considerare chiuso.

Aggiornamento di questa sessione:

- il bounded context `Kin List` non e' piu' `not_started`: esiste un primo slice backend completo e buildabile, ma non e' ancora il contratto finale del piano
- il backend `Kin List` ora include anche `POST /api/lists/{id}/items/confirm` e limiti centralizzati via config `KinList`
- il backend `Kin List` ora espone anche i contratti audio draft:
  - `POST /api/list-drafts/from-audio`
  - `POST /api/lists/{id}/item-drafts/from-audio`
- aggiunto coordinamento transazionale per le mutazioni multi-step e cleanup dei record idempotenza scaduti sul riuso della stessa chiave
- collegata la pipeline reale Speech/OpenAI in un progetto infrastrutturale separato:
  - `src/Infrastructures/Kin.KinHub.KinList.Ai`
  - se `Speech` e `OpenAi` non sono configurati il provider di default resta comunque fail-closed con `503 audio_processing_unavailable`
- aggiunti parametri config `KinList` per timeout e retry transient del processing audio
- generate le migration EF iniziali reali per `KinListDbContext`
- build verificate in questa tranche:
  - `dotnet build src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj`
  - `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj --filter KinListServiceTests`
  - `dotnet build Kin.KinHub.Core.slnx`
  - `dotnet ef migrations add InitialKinList --project src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Kin.KinHub.KinList.PostgreSql.csproj --startup-project src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj --context Kin.KinHub.KinList.PostgreSql.Models.KinListDbContext --output-dir Migrations`
  - `dotnet build src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj` dopo l'introduzione dei contratti audio draft
  - `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj --filter KinListServiceTests` con copertura aggiuntiva su idempotenza scaduta, draft audio e duplicati
  - `dotnet build src/Presentations/Kin.KinHub.KinList.Api/Kin.KinHub.KinList.Api.csproj` dopo l'introduzione di `Kin.KinHub.KinList.Ai`
  - `dotnet test src/Tests/Kin.KinHub.Core.Test/Kin.KinHub.Core.Test.csproj --filter KinList`
  - `dotnet build Kin.KinHub.Core.slnx` dopo l'integrazione del layer Azure Speech + OpenAI
