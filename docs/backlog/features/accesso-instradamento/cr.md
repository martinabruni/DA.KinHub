# CR-FEAT-001-001 - Centralizzare la pipeline HTTP del backend

- **Feature interessata**: FEAT-001 `accesso-instradamento`
- **Tipo**: refactoring architetturale e correzione tecnica
- **Stato**: implementata
- **Breaking change prodotto**: no
- **Piano**: `cr.plan.md`
- **Piano originario**: `feature.plan.md`

## Motivazione

FEAT-001 ha introdotto correttamente i contratti `ApiAccess` e `Family`, la verifica della membership e la telemetria KinHub, ma l'implementazione distribuisce responsabilita trasversali fra Function, helper, policy e composition root. Ogni nuovo endpoint dovrebbe oggi ricordare manualmente autenticazione, autorizzazione, correlation ID, cache privata, mapping delle eccezioni e misurazione dell'operazione.

La CR rende questi comportamenti sicuri per default e riutilizzabili, senza cambiare il comportamento prodotto di bootstrap, onboarding o membership.

## Comportamento attuale

- `AuthorizationLevel.Anonymous` e correttamente usato per non richiedere Function key alla SPA, ma l'autenticazione JWT viene avviata dagli endpoint tramite `ApiAuthorization`.
- Le Function controllano manualmente l'esito di `ApiAccess` o `Family` e costruiscono la risposta di errore.
- La policy `Family` usa un handler scoped, ma la composizione con `ApiAccess`, la risoluzione dei claim e la validazione di `familyId` sono orchestrate da un helper chiamato esplicitamente.
- Correlation ID, `no-store`, mapping di `BusinessDependencyException` e timing telemetrico richiedono chiamate ripetute.
- Policy, claim, route, codici errore e operation name non hanno una sola fonte completa.
- `KinHubTelemetry` espone `ActivitySource` e `Meter`, ma non esiste un exporter OpenTelemetry configurato per le sorgenti custom.
- L'OpenAPI manuale non e una rappresentazione completa degli endpoint e dei relativi requisiti di sicurezza.

## Comportamento desiderato

- Tutte le HTTP Function sono protette da `ApiAccess` per default; solo quelle marcate `[AllowAnonymous]` sono pubbliche.
- `[RequiresFamilyAccess]` applica in modo dichiarativo la verifica `Family` dopo `ApiAccess` e la validazione del singolo `familyId`.
- Il middleware Functions autentica una sola volta, risolve una sola volta `(iss, oid)` e interrompe la pipeline con Problem Details coerenti.
- Le Function ricevono una feature HTTP tipizzata e passano esplicitamente identita e `familyId` ai casi d'uso; Business e Domain non dipendono da `HttpContext`.
- Correlation ID, mapping delle eccezioni, logging tecnico e cache privata sono applicati dalla pipeline, non replicati negli endpoint.
- Configurazioni Entra, database e Blob Storage falliscono all'avvio quando incoerenti.
- Route e OpenAPI condividono costanti e test di parita.
- Log, metriche e trace custom usano Azure Monitor OpenTelemetry con managed identity e senza doppia pipeline Application Insights.

## Contratti invariati

- `GET /api/kinhub/bootstrap` resta protetto da `ApiAccess`.
- Le API su una famiglia esistente restano protette dalla policy esattamente `Family`.
- `familyId` resta un UUID obbligatorio in query e viene propagato esplicitamente fino alla persistenza.
- Restano validi i codici `auth.required`, `auth.scopeRequired`, `auth.requiredClaims`, `family.idRequired`, `family.idInvalid`, `family.accessDenied` e `dependency.postgresqlUnavailable`.
- Le risposte autenticate restano `Cache-Control: no-store, private`.
- `401`, `403`, onboarding ed errore tecnico restano semanticamente distinti.
- Nessun token, issuer, oid, familyId, nome o payload entra in log, metriche o trace.

## Scope

- Middleware Functions per correlation ID, eccezioni e autorizzazione.
- Sicurezza default-deny con `[AllowAnonymous]` e `[RequiresFamilyAccess]`.
- Registrazione DI centralizzata e costanti di contratto condivise.
- Problem Details, cache policy e logging tecnico uniformi.
- Validazione fail-fast di Entra, PostgreSQL e Blob Storage.
- Route/OpenAPI da fonti condivise e test di copertura.
- Migrazione dalla SDK Application Insights classica ad Azure Monitor OpenTelemetry.
- Operation scope monotono per metriche e trace KinHub.
- Aggiornamento di test, documentazione, skill e harness.

## Fuori scope

- Cambio di hosting da Azure Functions ad ASP.NET Core su App Service o Container Apps.
- Uso di Function key dalla SPA o sostituzione di Entra con `AuthorizationLevel`.
- Modifica dei flussi frontend, dei dati famiglia o delle regole di membership.
- Generic repository, mediator, endpoint executor, base class delle Function o result wrapper universale.
- Introduzione di un framework di validazione per il solo parametro `familyId`.
- Cache della membership o autorizzazione basata su dati forniti dal client.

## Sicurezza e privacy

- Il default-deny riduce il rischio di pubblicare accidentalmente un nuovo endpoint.
- La validazione `ApiAccess` precede la validazione `familyId`, evitando di esporre dettagli contrattuali a chiamanti non autorizzati.
- Gli errori tecnici espongono dettagli pubblici fissi; messaggio interno e stack restano solo nei log protetti.
- Il contesto autorizzativo e limitato alla richiesta e non diventa stato ambientale nel Business.
- OpenTelemetry usa dimensioni a bassa cardinalita e managed identity gia autorizzata con ruolo `Monitoring Metrics Publisher`.

## Dati e compatibilita

- Nessuna migration o modifica dello schema PostgreSQL.
- Nessuna modifica ai payload JSON di successo.
- Nessuna modifica richiesta alla PWA o all'acquisizione token MSAL.
- Correlation ID e trace ID vengono distinti, ma entrambi restano disponibili per diagnosi.
- La migrazione telemetrica deve evitare duplicati fra host Functions e worker.

## Rischi

- Un errore nell'ordine dei middleware puo produrre risposte senza correlation ID o applicare la policy sbagliata.
- La reflection usata per leggere metadata delle Function deve essere memorizzata in cache e coperta da test.
- La scrittura di una risposta dal middleware deve essere verificata con l'integrazione ASP.NET Core reale, non solo invocando direttamente le classi Function.
- La migrazione OpenTelemetry puo modificare nomi, tabelle o sampling della telemetria osservata.
- Validator troppo rigidi possono impedire l'avvio locale quando Entra e esplicitamente disabilitato.

## Rollback

- Il rollback applicativo usa lo ZIP backend N-1.
- La CR non modifica dati, quindi non richiede rollback database.
- La configurazione OpenTelemetry e i relativi package devono essere distribuiti nello stesso rilascio; non mantenere due exporter come fallback permanente.
- In caso di problemi di ingestione si ripristina l'intero pacchetto precedente, non una configurazione ibrida non testata.

## Criteri di accettazione

- Nessuna Function protetta chiama direttamente `AuthenticateAsync`, `AuthorizeAsync` o `ApiAuthorization`.
- Ogni HTTP Function e default `ApiAccess`, `[AllowAnonymous]` o `[RequiresFamilyAccess]`, con test che impedisce combinazioni mancanti o incoerenti.
- Correlation ID, Problem Details, error mapping e `no-store` sono applicati anche ai fallimenti prima dell'endpoint.
- Le Function non contengono `catch` per eccezioni trasversali ne timing con `DateTime.UtcNow`.
- Policy, claim, route, codici condivisi e operation name non sono duplicati come magic string.
- Entra, database e storage hanno validazione fail-fast coerente con l'ambiente.
- OpenAPI documenta tutte le route, security, parametri e risposte previste.
- Application Insights riceve log, metriche e trace custom correlati tramite OpenTelemetry, senza duplicati e senza dati sensibili.
- Build, test, publish, package, skill/docs/release validator e Bicep completano con successo.

## Tracciabilita

- Requisiti originari: `feature.md`.
- Piano di consegna originario: `feature.plan.md`.
- Piano correttivo approvato: `cr.plan.md`.
- Regole future generiche: `AGENTS.md` e `skills/backend/references/functions-http-pipeline.md`.
