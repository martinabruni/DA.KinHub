# Piano di implementazione - CR-FEAT-001-001

## Obiettivo

Centralizzare la pipeline HTTP della Function App introdotta da FEAT-001, rendere autenticazione e autorizzazione sicure per default, uniformare correlation ID, errori, cache e logging, completare configurazione/OpenAPI e migrare la telemetria custom ad Azure Monitor OpenTelemetry senza modificare i contratti prodotto.

## Decisioni esecutive

- Azure Functions Flex Consumption resta l'hosting.
- Gli endpoint bearer mantengono `AuthorizationLevel.Anonymous`: non si distribuiscono Function key alla SPA.
- Le HTTP Function sono `ApiAccess` per default; `[AllowAnonymous]` marca le eccezioni pubbliche.
- `[RequiresFamilyAccess]` aggiunge la verifica `Family` senza stringhe di policy negli endpoint.
- Business e Domain non accedono a `HttpContext` o a un current user ambientale.
- Application Insights classico viene sostituito da Azure Monitor OpenTelemetry; non sono ammesse due pipeline permanenti.
- Non si introducono base class Function, generic endpoint executor, generic repository, mediator o result wrapper universali.

## 1. Test di caratterizzazione

Prima di modificare la pipeline, coprire i contratti correnti:

- correlation ID ricevuto, generato e restituito;
- `401 auth.required` per token assente o invalido;
- `403 auth.scopeRequired` per scope assente;
- `401 auth.requiredClaims` per `iss`/`oid` mancanti;
- `400 family.idRequired` e `400 family.idInvalid`;
- `403 family.accessDenied` per membership non attiva;
- `503 dependency.postgresqlUnavailable`;
- Problem Details con status, `code`, `traceId`, `instance` e `application/problem+json`;
- `Cache-Control: no-store, private` per risposte autenticate ed errori;
- propagazione della cancellazione;
- liveness/readiness e registrazione DI.

I test devono distinguere unit test dei componenti di pipeline da almeno uno smoke test hosted che attraversi il worker Functions reale.

## 2. Correlation ID middleware

Creare `Middleware/CorrelationIdMiddleware.cs` e registrarlo come primo middleware HTTP.

Il middleware deve:

- accettare un solo `X-Correlation-ID` non vuoto entro un limite documentato;
- generarne uno quando assente o non valido;
- impostare l'header prima di invocare il middleware successivo;
- valorizzare `HttpContext.TraceIdentifier`;
- aprire uno scope `ILogger` con `CorrelationId`;
- mantenere separato `Activity.Current.TraceId` dal correlation ID applicativo.

Rimuovere tutte le chiamate manuali a `ApiResults.ApplyCorrelationId` e testare anche errori e short circuit.

## 3. Problem Details ed exception middleware

Creare:

- `Http/ApiProblemDetailsFactory.cs` come unica factory della forma RFC 7807;
- `Middleware/ExceptionHandlingMiddleware.cs` per il mapping globale;
- costanti condivise per i soli codici riutilizzati.

Mapping richiesto:

| Eccezione | Risposta |
|---|---|
| `BusinessValidationException` | `400` con codice stabile |
| `BusinessAccessDeniedException` | `403` con dettaglio pubblico generico |
| `BusinessDependencyException` | `503` con dettaglio pubblico fisso e causa loggata |
| eccezione inattesa | `500 internal.unexpected` senza dettagli interni |
| `OperationCanceledException` con cancellazione richiesta | rilancio senza conversione in `500` |

La factory applica `application/problem+json`, `code`, trace ID, correlation ID e `no-store`. Se la risposta e gia iniziata, il middleware logga e rilancia.

Rimuovere i `try/catch` trasversali dalle Function e non usare `exception.Message` come dettaglio pubblico per errori tecnici.

## 4. Metadata di accesso e default-deny

Creare:

- `Security/RequiresFamilyAccessAttribute.cs`;
- `Security/FunctionAccessMetadataProvider.cs`;
- `Security/KinHubAuthorizationMiddleware.cs`;
- `Security/KinHubAuthorizationFeature.cs`.

Il provider legge una sola volta gli attributi dal metodo indicato da `FunctionDefinition.EntryPoint` e memorizza il descriptor in cache.

Convenzioni:

- HTTP Function senza marker: `ApiAccess`;
- `[AllowAnonymous]`: endpoint pubblico;
- `[RequiresFamilyAccess]`: `ApiAccess` seguito da `Family`;
- combinazioni incompatibili: errore rilevato da test e fail-fast quando possibile;
- trigger non HTTP: esclusi dal middleware.

Aggiungere un test repository-wide che classifichi ogni HTTP Function e impedisca endpoint accidentalmente pubblici.

## 5. Pipeline di autenticazione e autorizzazione

Nel middleware, per una richiesta protetta:

1. Verificare che Entra sia abilitato; in caso contrario fallire chiusi.
2. Autenticare con JwtBearer e impostare `HttpContext.User`.
3. Valutare `ApiAccess` tramite `IAuthorizationService`.
4. Risolvere una sola volta l'identita canonica `(iss, oid)`.
5. Per `[RequiresFamilyAccess]`, accettare esattamente un `familyId` UUID non vuoto.
6. Costruire `FamilyAuthorizationResource` con identita, family e cancellation token.
7. Valutare la policy esattamente `Family` tramite il relativo handler scoped.
8. Pubblicare `KinHubAuthorizationFeature` sulla richiesta e invocare la Function.

L'ordine `ApiAccess` prima di `familyId` preserva il contratto fail-closed di FEAT-001.

La feature tipizzata contiene solo dati della richiesta gia verificati. Le Function passano esplicitamente `ExternalIdentity` e `familyId` ai casi d'uso; nessun layer inferiore legge la feature.

## 6. Registrazione sicurezza e magic string

Creare `Security/SecurityServiceCollectionExtensions.cs` e ridurre `Program.cs` a registrazioni di area.

Centralizzare:

- policy `ApiAccess` e `Family`;
- claim `scp` e `oid`, usando `JwtRegisteredClaimNames.Iss` per issuer;
- query parameter `familyId`;
- codici errore condivisi;
- nomi operazione telemetrici;
- route condivise con OpenAPI;
- tag health readiness.

Usare un requirement/handler dedicato allo scope se evita assertion e parsing duplicati. Non creare un catalogo globale di testi usati una sola volta.

Dopo la migrazione eliminare:

- `Configuration/ApiAuthorization.cs`;
- `Security/ApiAuthorizationOutcome.cs`;
- `Security/AuthorizedRequest.cs`.

Mantenere resolver identita, requirement/handler/resource Family e `FamilyAccessService`, che hanno responsabilita distinte.

## 7. Cache policy

Applicare:

- `no-store, private` prima dell'esecuzione di ogni endpoint protetto;
- `no-store, private` a ogni Problem Details;
- `no-store` a health, status e version;
- una policy pubblica distinta per OpenAPI e futuri contenuti cacheabili.

Non introdurre un middleware globale che disabiliti la cache per ogni risposta.

## 8. Configurazione fail-fast

Centralizzare la registrazione Functions e rimuovere il doppio binding di `EntraOptions`.

Quando `Entra:Enabled=true`, validare all'avvio:

- instance/authority HTTPS assoluta;
- tenant, audience e scope presenti e non placeholder;
- configurazione JwtBearer coerente e `MapInboundClaims=false`.

Quando Entra e disabilitato, consentire l'avvio locale ma mantenere gli endpoint protetti fail-closed.

Aggiungere `ValidateOnStart` per database e Blob Storage:

- modalita e credenziali coerenti con l'ambiente;
- valori richiesti non placeholder;
- timeout positivi e limitati;
- SSL richiesto in Azure;
- container Blob valido;
- connection string oppure endpoint assoluto HTTPS, non entrambi in modo incoerente.

Tenere ogni validator vicino alle proprie options e coprire combinazioni valide/invalide con test.

## 9. Route e OpenAPI

Creare:

- `Http/ApiRoutes.cs`;
- `OpenApi/OpenApiDocumentProvider.cs`.

Usare le costanti route sia negli attributi `HttpTrigger` sia nel provider. Il documento deve includere:

- tutte le route reali, incluso status e OpenAPI;
- bearer security scheme;
- security requirement sugli endpoint protetti;
- parametro query `familyId` per accesso Family;
- risposte `400`, `401`, `403`, `500`, `503` applicabili;
- schema e media type Problem Details;
- risposte e cache behavior degli endpoint pubblici.

Aggiungere test di parita route/OpenAPI e non introdurre un generatore pesante finche la superficie resta limitata.

## 10. Migrazione Azure Monitor OpenTelemetry

Rimuovere i package e la registrazione della SDK Application Insights classica. Aggiungere versioni stabili e compatibili di:

- `Microsoft.Azure.Functions.Worker.OpenTelemetry`;
- OpenTelemetry hosting;
- Azure Monitor OpenTelemetry exporter.

Configurare `UseFunctionsWorkerDefaults`, Azure Monitor exporter, `ActivitySource` e `Meter` KinList. Aggiornare `host.json` alla modalita OpenTelemetry supportata dal runtime in uso.

Mantenere `APPLICATIONINSIGHTS_CONNECTION_STRING` e usare managed identity. Verificare il ruolo `Monitoring Metrics Publisher` gia assegnato dal Bicep e decidere in modo esplicito la configurazione credential dell'exporter. Rimuovere setting classici non piu necessari.

Non mantenere due exporter. Verificare in Application Insights request, dependency, trace, metriche custom ed eccezioni senza duplicati.

## 11. Operation scope e logging

Rifattorizzare `KinHubTelemetry` per esporre uno scope:

```csharp
using var operation = telemetry.Begin(KinHubOperations.Bootstrap);
var result = await service.GetBootstrapAsync(...);
operation.Complete(result.State);
```

Lo scope usa `Stopwatch` o `TimeProvider.GetTimestamp`, registra una sola durata e un solo outcome, imposta status/tag sull'activity e produce un outcome tecnico se non completato.

La versione del meter deriva dai build metadata oppure viene omessa. Sono consentite solo dimensioni a bassa cardinalita; sono vietati token, claim completi, issuer, oid, familyId, nomi e payload.

Il logging tecnico vive soprattutto nei middleware e usa lo scope correlation. Non loggare ogni risposta positiva o rifiuto ordinario. Configurare livelli distinti per namespace KinHub, framework ed EF Core.

Testare strumenti con `ActivityListener` e `MeterListener` e la registrazione exporter senza dipendere dalla rete.

## 12. Errori dipendenza e health

Centralizzare `dependency.postgresqlUnavailable` in `BusinessErrorCodes`.

Restringere i `try/catch` di bootstrap e family access alle operazioni I/O: non classificare qualsiasi bug applicativo come indisponibilita PostgreSQL e preservare sempre la cancellazione.

Centralizzare il tag readiness in `InfrastructureHealthChecks.ReadyTag`. Mantenere liveness come controllo processo e readiness come controllo delle dipendenze necessarie. Aggiungere Blob Storage alla readiness solo quando e indispensabile per le funzionalita attive.

## 13. Pulizia endpoint

Al termine ogni Function contiene soltanto binding, input funzionale, chiamata al caso d'uso, eventuale outcome funzionale della telemetria e risultato HTTP positivo.

Rimuovere dagli endpoint:

- autenticazione/autorizzazione esplicita;
- correlation ID manuale;
- costruzione ripetuta di Problem Details;
- catch delle eccezioni trasversali;
- estrazione di `iss`/`oid`;
- stringhe di policy;
- timing tramite `DateTime.UtcNow`.

Non introdurre base class, service locator, ambient user nel Business, generic endpoint executor o result wrapper universale.

## 14. Test finali

Aggiungere o completare test per:

- ordine e short circuit dei middleware;
- default `ApiAccess`, `[AllowAnonymous]` e `[RequiresFamilyAccess]`;
- metadata cache e classificazione di tutte le HTTP Function;
- token, scope, claim e familyId validi/invalidi/multipli;
- membership concessa, inattiva, assente o appartenente a famiglia diversa;
- mapping `400`/`401`/`403`/`500`/`503` senza dettagli sensibili;
- correlation ID e no-store anche sugli errori;
- cancellazione;
- validator Entra/database/storage;
- completezza OpenAPI;
- emissione singola di outcome/durata;
- registrazione DI completa;
- pipeline ospitata dal worker Functions.

## 15. Documentazione e conoscenza riutilizzabile

Aggiornare:

- `AGENTS.md` con pipeline, default-deny, OpenTelemetry e regole di centralizzazione;
- `docs/architecture/overview.md`;
- runbook Entra e osservabilita;
- documentazione OpenAPI e configurazione;
- skill backend, guida di riferimento, catalogo quando i componenti saranno implementati e registry generato;
- change fragment bilingue.

Non modificare `docs/bootstrap.prompt.md`, che resta storico.

## 16. Verifica e rilascio

Eseguire:

```text
dotnet restore KinHub.slnx
dotnet build KinHub.slnx --configuration Release --no-restore
dotnet test KinHub.slnx --configuration Release --no-build
dotnet publish src/backend/applications/DA.KinHub.Functions/DA.KinHub.Functions.csproj -c Release
./scripts/package-backend.ps1 -Environment Development

npm run skills:validate
npm run skills:build
npm run docs:validate
npm run docs:sync
npm run release:validate

az bicep build --file infra/app.bicep
```

Completare smoke test locale di health, status, version e OpenAPI, test autenticati `ApiAccess`/`Family` e verifica Application Insights di correlazione, custom metric/trace, dependency ed assenza di duplicati.

## Sequenza di consegna

1. Test di caratterizzazione.
2. Correlation ed exception middleware.
3. Authorization middleware e metadata.
4. Pulizia delle Function e cache policy.
5. Configurazione fail-fast.
6. Route e OpenAPI.
7. Migrazione OpenTelemetry.
8. Operation scope, logging, errori dipendenza e health.
9. Documentazione, skill, validator e packaging completo.

Il rollback applicativo usa lo ZIP backend N-1. La CR non modifica lo schema dati; non e previsto rollback database.
