---
id: kinhub-backend
name: KinHub backend patterns
version: 0.3.0
area: backend
description: Servizi business, contratti DDD, Function endpoint e pattern infrastrutturali riutilizzabili.
catalog: catalog.json
references: docs/architecture/http-functions.md, docs/operations/observability.md
---

# KinHub backend

## Scopo

Mantenere servizi e contratti .NET riutilizzabili con dipendenze orientate verso il dominio.

## Quando usare

Per entità, value object, use case, repository, endpoint Function, Problem Details e build metadata.

## Quando non usare

Non usare per testo UI, Bicep o workflow di release.

## Componenti e servizi disponibili

`BuildInfoProvider`, `ApiResults`, `ApiProblemDetailsFactory`, middleware HTTP Functions per correlation/eccezioni/autorizzazione, repository EF per famiglie e membership, migration initializer locale e `IDocumentStorage` con adapter Azure Blob/Azurite. Il pattern approvato per la pipeline HTTP e descritto in `docs/architecture/http-functions.md`; il debito FEAT-001 non deve essere copiato da nuovi endpoint.

## API e interfacce

I servizi business e storage espongono interfacce async con `CancellationToken`. `IDocumentStorage` salva contenuti tramite chiavi opache e non espone credenziali. Gli endpoint REST restituiscono JSON o RFC 7807.

Le HTTP Function usano `AuthorizationLevel.Anonymous` per le API bearer: le Function key non sostituiscono Entra. `ApiAccess` e il default, `[AllowAnonymous]` marca solo endpoint pubblici approvati e `[RequiresFamilyAccess]` applica la policy esattamente `Family`.

La pipeline obbligatoria e correlation ID, exception handling, authorization, endpoint. Middleware e factory centralizzano autenticazione, autorizzazione, `X-Correlation-ID`, Problem Details, logging tecnico e cache privata. Le Function non replicano guard, `try/catch` trasversali o header.

Una feature HTTP tipizzata puo trasportare identita e `familyId` gia verificati nell'Application layer. Business e Domain ricevono tali valori come parametri espliciti e non accedono a `HttpContext` o current user ambientali.

## Esempi

Vedi `examples/DocumentStorage.example.cs`, `docs/architecture/http-functions.md`, `docs/operations/observability.md` e i test business/integration.

## Dipendenze

.NET 10, Azure Functions Isolated 4.x, EF Core 10, Npgsql, Azure Blob Storage, OpenTelemetry e Azure Monitor.

## Vincoli

Il dominio non dipende da EF o Azure. Niente migration di produzione al cold start. Niente log di token, password o dati sensibili.

KinHub possiede identita, profili, famiglie, membership, inviti, bootstrap post-login e policy `Family`. I KinService consumano questo contesto: contratti, route e telemetria condivisi usano nomi KinHub e non il namespace KinList o di altri servizi.

Policy, claim, route, query parameter, codici condivisi e operation name hanno una fonte autorevole. Route e OpenAPI hanno test di parita. Entra, database, storage ed exporter critici usano options tipizzate e `ValidateOnStart` senza bypass di sicurezza.

Problem Details nasce da una factory unica e gli errori tecnici non espongono cause interne. API protette ed errori sono `no-store, private`; health/status/version sono `no-store`. Non mantenere SDK Application Insights classica e OpenTelemetry in parallelo.

Non introdurre base class Function, service locator, generic endpoint executor, result wrapper universali, generic repository o framework di validazione prima di un bisogno ripetuto.

## Test richiesti

Regola di dominio, validazione business, DI, configurazione critica, endpoint metadata e Problem Details. Per HTTP aggiungere test di ordine/short circuit middleware, default-deny, `[AllowAnonymous]`, `[RequiresFamilyAccess]`, correlation ID, cache, cancellazione e parita route/OpenAPI. Per osservabilita verificare emissione singola tramite `ActivityListener` e `MeterListener` e almeno uno smoke test del worker reale.

## Checklist di aggiornamento

Implementa nel layer corretto, applica prima la pipeline condivisa, aggiungi test/esempio, aggiorna catalogo e documentazione, crea fragment e rigenera registry. Se una nuova esigenza trasversale induce codice identico in piu endpoint, valuta un middleware o una factory mirata prima di aggiungere helper locali; non generalizzare un solo caso.

## Changelog

0.3.0: aggiunti catalogo e riferimenti ai componenti riusabili della pipeline HTTP centralizzata e della factory Problem Details.

0.2.0: aggiunte regole per pipeline HTTP centralizzata, sicurezza default-deny, Problem Details, configurazione fail-fast e Azure Monitor OpenTelemetry.

0.1.0: servizi iniziali progetto, metadata e storage documentale; dettagli in `docs/patch-notes`.
