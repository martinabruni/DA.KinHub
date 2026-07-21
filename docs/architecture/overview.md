# Architettura KinHub

KinHub usa un monolite modulare pragmatico: una SPA React/PWA comunica via HTTPS con una Function App .NET 10 Isolated. Il backend separa dominio, business, infrastruttura e applicazione. PostgreSQL è l'unico datastore iniziale.

Il dominio non dipende da framework. Il business orchestra use case e contratti. Infrastructure contiene EF Core e integrazioni tecniche. Applications espone trigger HTTP e composition root.

Azure usa una Function App per piano Flex Consumption, Static Web Apps per il frontend, PostgreSQL Flexible Server, Storage identity-based, Key Vault, Application Insights e Log Analytics. Il deploy del codice è separato dal deploy infrastrutturale.

## Decisioni

- Niente CQRS o mediator finché non esiste un bisogno misurato.
- Niente migration lunghe al cold start; in produzione si usa un migration bundle in pipeline.
- Le skill contengono conoscenza, non codice caricato dinamicamente.
- System-assigned managed identity riduce oggetti e credenziali in un progetto personale.
- Rete pubblica controllata per dev; VNet resta opzionale e disabilitata per default.
- Le HTTP Function mantengono endpoint sottili: middleware Functions centralizza correlation, errori e autorizzazione, mentre `IAuthorizationService` valuta requirement e handler.
- La pipeline HTTP applicativa e ordinata in `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `KinHubAuthorizationMiddleware` ed endpoint; Problem Details, route OpenAPI e cache policy nascono da componenti condivisi.
- `AuthorizationLevel` governa le Function key, non l'identita utente. Le API bearer usano `Anonymous`, default `ApiAccess`, eccezioni `[AllowAnonymous]` e marker `[RequiresFamilyAccess]` per la policy `Family`.
- Business e Domain ricevono identita e scope famiglia come parametri espliciti e non dipendono dal contesto HTTP.
- Azure Monitor OpenTelemetry e la pipeline unica per log, metriche e trace applicativi; route e OpenAPI condividono contratti versionati.

Dettagli e vincoli della pipeline sono in `docs/architecture/http-functions.md`; configurazione e verifica della telemetria sono in `docs/operations/observability.md`.
