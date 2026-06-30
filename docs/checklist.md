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

Stato non ancora fatto del piano

- [partial] Broker Identity backend avviato:
  - client OAuth stabili configurabili via `OAuth:Clients`
  - sessione broker server-side con cookie `HttpOnly`
  - `GET /authorize` riusa la sessione esistente e salta il reinserimento credenziali
  - `POST /logout` centralizzato invalida cookie/sessione broker
  - token exchange authorization-code non restituisce piu' `refresh_token` al browser
  - resta da migrare il frontend al client OAuth comune e completare la policy `Secure`/cross-site per gli ambienti reali
- [not_started] Eliminazione del relay frontend cross-app basato su token in fragment/localStorage.
- [not_started] Client OAuth comune tra Core, Identity e KinRecipe.
- [not_started] Standardizzazione completa di tutti i controller su RFC 9457 `ProblemDetails`.
- [not_started] `family-context` remoto tra servizi con fail-closed `503` quando Core non e' raggiungibile.
- [not_started] Nuovo bounded context Kin List backend.
- [not_started] Nuova SPA Kin List.
- [not_started] Migrazioni dati, IaC e pipeline del piano esteso.

Prossimo step consigliato

1. Migrare Core, Identity e KinRecipe a un client OAuth comune basato su Authorization Code + PKCE.
2. Rimuovere il relay frontend con token in fragment e tutto il codice `sessionRelay` / `appendSessionToUrl`.
3. Decidere come propagare o rimuovere il vecchio stato UI `activeMember` senza reintrodurre un canale autoritativo lato client.
4. Solo dopo, aprire il blocco Kin List backend/frontend del piano.

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

- [done] I nuovi host riusano file del vecchio Shared.Api via Compile Include linkato.
- [done] Build dei nuovi host eseguita:
  - `dotnet build src/Presentations/Kin.KinHub.Identity.Api/Kin.KinHub.Identity.Api.csproj`
  - `dotnet build src/Presentations/Kin.KinHub.KinRecipe.Api/Kin.KinHub.KinRecipe.Api.csproj`
- [done] Fix completati emersi dalla build:
  - aggiunto `IdentityDbContextFactory`
  - separato il mapping HTTP `Core` vs `Identity` per evitare dipendenze non necessarie in `KinRecipe.Api`
  - aggiunto il global using identity mancante in `KinRecipe.Api`
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
- [in_progress] `Kin.KinHub.KinRecipe.React` ridotta verso il ruolo recipe:
  - routing family/services/login locali rimosso
  - redirect verso Identity su assenza auth o member context
  - session relay implementato con fragment payload e fallback legacy query params
  - nav desktop/mobile riallineata alle sole feature recipe
- [done] Introdotti `VITE_IDENTITY_URL` / `VITE_KINRECIPE_URL` negli `.env.example` rilevanti.
- [partial] Handoff cross-domain implementato solo lato frontend via URL relay temporaneo; soluzione pragmatica ma non ancora hardenizzata lato backend/cookie.
- [done] Build frontend eseguite:
  - `Core.React`: `npm run build` riuscita
  - `KinRecipe.React`: `npm run build` riuscita
- [done] `Identity.React` riportata verde:
  - fixato l'errore TypeScript residuo in `src/components/Sidebar.tsx`
  - build `npm run build` riuscita
- [note] Per verificare localmente le due app clonate senza reinstallare dipendenze sono stati creati junction `node_modules` verso `src/Presentations/Kin.KinHub.Core.React/node_modules`.
- [partial] Relay cross-domain frontend rifinito:
  - trasferimento sessione spostato da querystring a fragment payload (`#relay=...`) per ridurre l'esposizione dei token
  - mantenuto fallback reader sui vecchi parametri query per compatibilita' temporanea

EF / Database

- [done] Creati i design-time factory EF:
  - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/CoreDbContextFactory.cs
  - src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Models/IdentityDbContextFactory.cs
- [done] Generate baseline migration code-first no-op:
  - src/Infrastructures/Kin.KinHub.Core.PostgreSql/Migrations/20260626072328_CoreBaseline.cs
  - src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Migrations/20260626072350_IdentityBaseline.cs
  - snapshot EF generati per entrambi i DbContext
  - script verificati come metadata-only: creano solo `__EFMigrationsHistory` e registrano la migration
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
- [untracked] docs/checklist.md
- [untracked] src/Presentations/Kin.KinHub.Identity.Api
- [untracked] src/Presentations/Kin.KinHub.KinRecipe.Api
- [untracked] src/Presentations/Kin.KinHub.Identity.React
- [untracked] src/Presentations/Kin.KinHub.KinRecipe.React
- [untracked] src/Infrastructures/Kin.KinHub.Core.PostgreSql/Models/CoreDbContextFactory.cs
- [untracked] src/Infrastructures/Kin.KinHub.Identity.PostgreSql/Models/IdentityDbContextFactory.cs
- [untracked] src/Presentations/Kin.KinHub.Shared.Api/Common/IdentityHttpResultMapper.cs

Suggested Resume Order

1. Eseguire smoke check manuale dei link `Identity -> KinRecipe` e dei redirect `KinRecipe -> Identity` con env locali valorizzati.
2. Eseguire smoke check manuale del relay cross-app con env locali e URL split reali.
3. Rifinire e testare i nuovi workflow con il set completo di GitHub vars/secrets richiesto.
4. Solo dopo: conversione host separati verso Azure Functions/containerized functions.

Important Note

Il precedente stop era avvenuto durante un apply_patch interrotto. Quel primo blocco backend e' chiuso; il frontend split ora ha `Core.React`, `Identity.React` e
`KinRecipe.React` buildabili. Anche il primo step code-first e' ora in repo tramite baseline migrations no-op. Il blocco IaC/CI e' stato avviato in modo sostanziale,
ma va ancora validato con secret/vars reali prima di poterlo considerare chiuso.
