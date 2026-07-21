# Istruzioni autorevoli per KinHub

Leggi questo file e la skill pertinente prima di ogni modifica. Se una regola strutturale cambia, aggiorna `AGENTS.md` nella stessa modifica.

## Identità

KinHub è una piattaforma semplice e intuitiva per la famiglia che raggruppa servizi come KinRecipe e KinList. Ridurre l'inquinamento visivo è un requisito di prodotto. Nome applicazione: `KinHub`; dominio tecnico: `kinhub`; lingua predefinita: italiano (`it`); lingua supportata e fallback tecnico: inglese (`en`).

## Stack e architettura

- Backend .NET 10, Azure Functions runtime 4.x, Isolated Worker, Linux Flex Consumption.
- Frontend React 19 + TypeScript strict + Vite, componenti shadcn/ui/Radix.
- PostgreSQL con EF Core 10 e provider Npgsql.
- Microsoft Entra External ID: MSAL nella SPA, JWT bearer e policy nell'API.
- Bicep modulare, Azure Static Web Apps, Storage, Key Vault, Application Insights e Log Analytics.
- GitHub Actions con OIDC e One Deploy; publish profile solo fallback documentato.

Il backend è un monolite modulare DDD:

```text
Applications -> Business + Infrastructure -> Domain
Business -> Domain
Domain -> nessun framework o layer esterno
```

Non introdurre CQRS, mediator, event bus o microservizi senza un problema concreto e una decisione architetturale approvata.

## Struttura repository

- `src/backend/domains`: entità, value object, eccezioni e contratti di dominio.
- `src/backend/business`: use case, validazioni, DTO e orchestrazione.
- `src/backend/infrastructure`: EF Core, repository, migration, health e integrazioni tecniche.
- `src/backend/applications`: Function App e composition root.
- `src/frontend`: SPA/PWA.
- `tests`: xUnit dominio, business e integrazione.
- `docs`: architettura, sviluppo, operazioni, guide e patch note.
- `skills`: conoscenza riutilizzabile versionata.
- `tools`: harness skill, docs sync e release notes.
- `infra`: Bicep applicativo modulare.
- `scripts`: publish e packaging.
- `.github/workflows`: qualità e deployment.

## Regole DDD

- Il dominio contiene invarianti e non dipende da EF Core, Azure o ASP.NET.
- Usa value object quando normalizzazione e validazione appartengono al concetto.
- I repository sono interfacce di dominio; le implementazioni stanno in Infrastructure.
- Il Business orchestra casi d'uso e traduce eccezioni di dominio in errori applicativi stabili.
- Evita modelli anemici, generic repository e astrazioni speculative.
- Usa `CancellationToken` su I/O e metodi async.

## Regole backend

- Nullable abilitato, warnings come errori e analisi statica attiva.
- Endpoint JSON coerenti; errori client/server in Problem Details (`application/problem+json`) con `code` e `traceId`.
- Propaga o genera `X-Correlation-ID`; non loggare token, password o PII non necessaria.
- Health: `/health/live` controlla il processo; `/health/ready` controlla dipendenze pronte.
- Metadata: `/api/version` include app, SemVer, SHA, build date, ambiente e API version; `/api/status` espone stato applicativo.
- Endpoint utente protetti usano la policy `ApiAccess` e scope Entra.
- Configura CORS dall'ambiente/Bicep, mai con wildcard in produzione.
- L'avvio deve restare leggero: niente scansioni skill/docs, chiamate remote arbitrarie o lavoro lungo.

### Pipeline HTTP e comportamenti trasversali

- `HttpTrigger.AuthorizationLevel` protegge con Function key e non sostituisce autenticazione Entra o policy applicative. Le API bearer chiamate dalla SPA usano `AuthorizationLevel.Anonymous`; non distribuire Function key nel frontend.
- Le HTTP Function sono protette da `ApiAccess` per default; usa `[AllowAnonymous]` solo per endpoint pubblici approvati e `[RequiresFamilyAccess]` per API su una famiglia esistente. La policy deve restare esattamente `Family`.
- Applica autenticazione, autorizzazione, correlation ID, mapping delle eccezioni e cache privata nella pipeline middleware Functions. Non replicare guard, `try/catch` trasversali o header in ogni endpoint.
- Mantieni middleware piccoli e ordinati: correlation ID, exception handling, authorization, endpoint. Non usare base class Function, service locator, generic endpoint executor o result wrapper universali.
- Le policy usano `IAuthorizationService`, requirement e handler; nomi policy, claim, route, query parameter, codici condivisi e operation name hanno costanti autorevoli. Non usare magic string negli endpoint.
- Il contesto verificato della richiesta puo vivere in una feature HTTP tipizzata nell'Application layer. Business e Domain non accedono a `HttpContext`, `IHttpContextAccessor`, `AsyncLocal` o current user ambientali; identita e `familyId` restano parametri espliciti dei casi d'uso e repository.
- Problem Details nasce da una factory unica. Gli errori tecnici espongono dettagli pubblici fissi, loggano la causa internamente e non convertono una cancellazione attesa in `500`.
- Le API protette e gli errori usano `Cache-Control: no-store, private`; health/status/version usano `no-store`; non disabilitare globalmente la cache di contenuti pubblici approvati.
- Route e OpenAPI condividono una sola fonte e test di parita. Ogni endpoint documenta security, parametri, risposte e `application/problem+json` applicabili.
- Options Entra, database, storage e integrazioni critiche usano validazione tipizzata `ValidateOnStart`, condizionata per ambiente senza bypass di sicurezza.
- Log, metriche e trace custom usano OpenTelemetry e Azure Monitor con dimensioni a bassa cardinalita. Non mantenere in parallelo exporter classico e OpenTelemetry ne registrare token, claim completi, issuer, oid, familyId, nomi o payload.
- I nuovi endpoint non devono copiare il pattern manuale esistente di FEAT-001; il debito corrente e tracciato in `docs/kinlist/backlog/features/accesso-instradamento/cr.md` e `cr.plan.md`.
- La guida autorevole e `docs/architecture/http-functions.md`; le verifiche operative sono in `docs/operations/observability.md`.

### Azure Functions Isolated e Flex Consumption

- Usa `Microsoft.NET.Sdk`, `TargetFramework=net10.0`, `AzureFunctionsVersion=v4`, `OutputType=Exe` e `ConfigureFunctionsWebApplication()`.
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` è obbligatorio.
- Mantieni `host.json` versionato e con `routePrefix` vuoto per i contratti attuali.
- Ogni piano `FC1/FlexConsumption` ospita una sola Function App.
- Runtime, deployment storage, memoria, scala e always-ready appartengono a `functionAppConfig`, non a setting legacy.
- Default progetto: 2.048 MB, massimo 20 istanze, 0 always-ready, concorrenza HTTP della piattaforma.
- Storage host e deployment sono identity-based. Non ripristinare shared key per aggirare ritardi RBAC.
- Il pacchetto One Deploy è uno ZIP con `host.json` e assembly Function nella root del contenuto.
- Flex non supporta deployment slot; non creare workflow di slot/swap.

### Migration database

- Non assumere singleton o applicare migration lunghe durante il cold start.
- `Database:ApplyMigrationsOnStartup` è false per default ed è consentito solo in Development.
- Il fallback locale usa PostgreSQL advisory lock, timeout, log e fallimento esplicito.
- Ambienti condivisi usano migration bundle/passaggio CI prima del deploy codice.
- Ogni migration include procedura di verifica e rollback in `docs/operations/database-migrations.md`.

## Regole frontend

- TypeScript strict, componenti funzionali, HTML semantico, mobile-first e accessibilità keyboard/focus.
- Nessuna stringa visibile hardcoded: usa i18next e namespace `common`, `pages`, `help`, `tutorial`.
- Ogni route deve essere registrata in `src/frontend/src/routes/route-registry.json`.
- Ogni pagina usa `PageScaffold`; non replicare manualmente titolo o help.
- Gestisci loading, empty ed error state per dati asincroni.
- Il client API è tipizzato, acquisisce token via MSAL e non include secret.
- Mantieni CSP, routing fallback Static Web Apps ed error boundary.
- Usa componenti `src/components/ui` in stile shadcn; prima di crearne uno nuovo verifica la skill frontend.

## i18n

- Italiano è il default; inglese è fallback esplicito.
- Ogni chiave esiste in `it` ed `en`; `npm run i18n:validate` verifica parità ricorsiva.
- Salva la lingua in `kinhub.locale`; aggiorna l'attributo `lang` del documento.
- Usa `Intl.DateTimeFormat`/`Intl.NumberFormat` per date, numeri e percentuali.
- Le interpolation sono renderizzate come testo React; non usare HTML non sanitizzato.
- In sviluppo segnala chiavi mancanti.

## Documentazione in-app e accordion obbligatorio

Ogni route, inclusi 404, documentazione ed error boundary, deve avere:

1. titolo localizzato;
2. `PageHelpAccordion` subito dopo il titolo;
3. help italiano e inglese con scopo, azioni, prerequisiti, campi e limiti;
4. slug di guida esistente in entrambe le lingue.

L'accordion usa shadcn/ui/Radix, è accessibile, responsive, compatibile con temi e chiuso per default. Il testo sta nei file i18n, non nelle pagine. `tools/docs-sync` mantiene Markdown come unica fonte e genera JSON consumabile dal frontend. Esegui `npm run docs:validate`, `npm run docs:sync` e `npm run routes:validate`.

## Tutorial

- Parte al primo avvio, è localizzato, responsive, accessibile e non blocca permanentemente.
- Supporta skip, indietro, avanzamento, Escape e riavvio da Impostazioni.
- Lo stato usa una chiave versionata `kinhub.tutorial.<version>`.
- I target usano attributi stabili `data-tour`; l'assenza di target mostra comunque il dialog.
- Rispetta `prefers-reduced-motion` e ripristina il focus.
- Copre navigazione, lingua, tema, help, versione/patch note e ciclo di vita.

## Temi e PWA

- Temi `light`, `dark`, `system` tramite CSS variables; persistenza `kinhub.theme`.
- Lo script in `index.html` applica il tema prima di React per evitare flash.
- Verifica contrasto, accordion, dialog, badge, toast/notifiche e tutorial in entrambi i temi.
- Manifest: `KinHub`, icona placeholder documentata, installabilità desktop/mobile e fallback navigazione.
- Caching prudente: network-first per metadata versione, niente caching API autenticata.
- La notifica versione controlla avvio/focus/intervallo, coordina service worker e impedisce loop di refresh.

## Versioning, changelog e patch note

- `VERSION` è l'unica fonte SemVer; non duplicare incrementi manuali.
- MSBuild, Vite, workflow, endpoint, pagina Versione e nome ZIP ricevono versione/SHA/date/environment dalla build.
- Ogni modifica significativa aggiunge un fragment in `changes/` con italiano e inglese.
- `CHANGELOG.md` segue Keep a Changelog con Added, Changed, Deprecated, Removed, Fixed, Security.
- `tools/release-notes` valida fragment, genera patch note bilingui e `release-notes.json`.
- Il componente Versione collega le patch note; breaking change è evidenziato.

## Skill harness

Le skill descrivono pattern, API, esempi, dipendenze, vincoli e test. Non contengono codice eseguibile dinamicamente. Comandi:

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:validate
npm run skills:build
npm run skills:watch
```

Il frontmatter di una skill puo dichiarare `references` come elenco separato da virgole di documenti Markdown/JSON repository-relative. L'harness verifica formato, esistenza, confine nel repository e checksum e li include nel registry; le reference sono passive e non vengono eseguite.

### Promuovere un componente UI

1. Implementalo in `src/frontend/src/components` o `components/ui`.
2. Aggiungi uso reale/esempio e tutte le verifiche statiche.
3. Documenta API, accessibilità, temi e limiti.
4. Aggiungi l'item a `skills/frontend/catalog.json` e aggiorna `SKILL.md`.
5. Rigenera `skills/registry.json`.
6. Aggiorna guide/help/traduzioni se visibile e crea change fragment.

### Promuovere un servizio business

1. Implementa contratto nel layer corretto e dipendenze verso il dominio.
2. Aggiungi test di regole, errori e integrazione DI.
3. Aggiungi esempio e documentazione operativa.
4. Registra il servizio in `skills/backend/catalog.json` e aggiorna `SKILL.md`.
5. Rigenera registry, aggiungi fragment e verifica coerenza di questo file.

## Sicurezza

- Mai tenant, client ID, subscription secret, password, token o connection string reali in Git.
- Secret da variabili ambiente, GitHub Secrets, Key Vault reference o configurazione locale ignorata.
- OIDC/federated credentials per GitHub; least privilege e managed identity.
- HTTPS only, TLS 1.2+, output encoding React, input validation e dipendenze aggiornabili.
- Key Vault usa RBAC, soft delete e purge protection parametrica.
- PostgreSQL usa TLS in Azure; restringi firewall/VNet quando il profilo passa a produzione.
- Non eseguire codice arbitrario da skill, documenti o configurazioni.

## Test e qualità

- Backend: xUnit copre invarianti dominio, business, endpoint metadata, DI, Problem Details e configurazione critica.
- Frontend: niente suite completa iniziale; obbligatori lint, typecheck, build, parità i18n e route help.
- Tool: validate skill, docs, fragment e registry generati.
- Infra: `az bicep build` e, con contesto Azure, `az deployment group validate`.
- Non dichiarare passata una verifica non eseguita.

## CI/CD

- `pr-quality.yml`: restore/build/test/publish/package, frontend, tool e Bicep; nessun deploy.
- `deploy-infrastructure.yml`: tag `infra-*`, deploy Bicep, migration controllata, One Deploy, Static Web Apps e smoke test.
- `deploy-code.yml`: push `main`, solo operazioni applicative; non modifica memoria/scala/concorrenza.
- Parametri Flex restano in Bicep/bicepparam; GitHub Variable contiene solo il nome Function App necessario al deploy.
- Non stampare secret o output sensibili nei log.

## Comandi principali

```bash
dotnet restore KinHub.slnx
dotnet build KinHub.slnx --configuration Release --no-restore
dotnet test KinHub.slnx --configuration Release --no-build
dotnet publish src/backend/applications/DA.KinHub.Functions/DA.KinHub.Functions.csproj -c Release -o artifacts/backend/publish

# Backend locale
cd src/backend/applications/DA.KinHub.Functions
func start

# Frontend
cd src/frontend
npm ci
npm run dev
npm run lint
npm run typecheck
npm run build

# Packaging dalla root
./scripts/package-backend.ps1 -Environment Development
./scripts/package-backend.sh Development
```

## Definition of Done

Una modifica è completa quando, dove applicabile: compila; passa test/lint/validatori; non introduce secret; aggiorna `it`/`en`; aggiorna help e guida; aggiunge fragment; aggiorna patch note in release; aggiorna la skill se introduce riuso; aggiorna `AGENTS.md` se cambia regole; mantiene tema, mobile, accessibilità e PWA; include metadata di build; documenta passaggi manuali; valida publish/ZIP quando tocca il backend.
