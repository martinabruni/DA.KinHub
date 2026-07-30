# Pipeline HTTP delle Azure Functions

## Scopo

Questa guida definisce il pattern generico per ogni HTTP Function KinHub. L'obiettivo e mantenere gli endpoint sottili, sicuri per default e coerenti senza simulare MVC dentro Azure Functions Isolated.

## Confini della piattaforma

`HttpTrigger.AuthorizationLevel` gestisce Function key (`Anonymous`, `Function`, `Admin`) e non valida token Entra, scope OAuth o membership applicative. Una SPA non deve ricevere Function key: le API bearer usano `AuthorizationLevel.Anonymous` e applicano JwtBearer e policy nel worker.

L'integrazione ASP.NET Core di Functions fornisce `HttpRequest`, `HttpContext` e `IActionResult`, ma non il normale routing/middleware MVC che interpreta automaticamente `[Authorize]`. KinHub usa middleware `IFunctionsWorkerMiddleware`, metadata espliciti e il framework standard `IAuthorizationService` con requirement e handler.

## Pipeline obbligatoria

```text
CorrelationIdMiddleware
    -> ExceptionHandlingMiddleware
    -> KinHubAuthorizationMiddleware
    -> Function endpoint
```

Il correlation middleware valida o genera `X-Correlation-ID`, imposta la risposta prima di proseguire, valorizza `HttpContext.TraceIdentifier` e apre uno scope di logging. Il trace distribuito usa `Activity.TraceId` e resta distinto dal correlation ID applicativo.

L'exception middleware converte eccezioni applicative note in Problem Details, nasconde dettagli tecnici, logga cause server-side e rilancia le cancellazioni richieste. Se la risposta e gia iniziata, non tenta una seconda serializzazione.

L'authorization middleware autentica JwtBearer, applica `ApiAccess`, risolve l'identita canonica e, quando richiesto, valida `familyId` e applica `Family`. Le risposte protette ricevono `Cache-Control: no-store, private` prima dell'endpoint.

## Default-deny e metadata

- Una HTTP Function senza marker richiede `ApiAccess`.
- `[AllowAnonymous]` e consentito solo per endpoint pubblici approvati, come health e metadata pubblici.
- `[RequiresFamilyAccess]` richiede prima `ApiAccess`, poi un singolo `familyId` UUID non vuoto e infine la policy `Family`.
- Trigger non HTTP non passano dalla pipeline HTTP.
- Un provider legge metadata da `FunctionDefinition.EntryPoint`, li memorizza in cache e rifiuta combinazioni incoerenti.
- Un test repository-wide deve classificare tutte le HTTP Function per impedire pubblicazione accidentale.

`AuthorizationLevel.Function` non deve essere usato come seconda autenticazione della SPA: obbligherebbe a esporre una chiave condivisa nel client e non sostituirebbe le policy utente.

## Autenticazione e autorizzazione

Il flusso protetto e:

1. Verificare la configurazione Entra senza bypass quando disabilitata.
2. Autenticare tramite lo schema JwtBearer.
3. Valutare `ApiAccess` e lo scope configurato.
4. Risolvere `(iss, oid)` senza fallback su email o nome.
5. Per Family, validare `familyId` dopo `ApiAccess`.
6. Valutare `FamilyAuthorizationRequirement` con resource tipizzata e handler scoped asincrono.
7. Esporre i valori verificati in una feature HTTP tipizzata limitata all'Application layer, inclusi `familyId` e l'eventuale `applicationUserId` applicativo gia risolto dalla policy.
8. Passare esplicitamente identita e `familyId` al caso d'uso e alla persistenza.

La policy protegge l'ingresso ma non sostituisce lo scope dati: query e scritture continuano a includere `familyId` e gli altri predicati autorevoli.

## Contratti e magic string

Centralizzare soltanto valori condivisi:

- policy `ApiAccess` e `Family`;
- claim `scp` e `oid`, con `JwtRegisteredClaimNames.Iss` per issuer;
- query parameter `familyId`;
- route usate anche dall'OpenAPI;
- codici errore riutilizzati;
- operation name telemetrici;
- tag health readiness.

I nomi Function usati una sola volta, i dettagli locali e i testi non condivisi restano vicino al punto d'uso. Non creare un catalogo globale privo di responsabilita.

## Errori e risposte

Una factory unica produce `application/problem+json` con status, `code`, `traceId`, correlation ID e `instance`. Gli errori tecnici espongono un dettaglio pubblico fisso; messaggio interno e stack sono loggati senza PII.

Mapping minimo:

| Categoria | Risposta |
|---|---|
| input o regola applicativa invalida | `400` |
| autenticazione assente o token invalido | `401` |
| scope o accesso risorsa negato | `403` |
| dipendenza indisponibile | `503` |
| eccezione inattesa | `500 internal.unexpected` |
| cancellazione richiesta | nessuna conversione in `500` |

Gli endpoint non duplicano `try/catch` trasversali. Non introdurre base class Function, generic executor o result wrapper solo per nascondere la pipeline.

## Cache e dati sensibili

- API protette ed errori: `no-store, private`.
- Health, status e version: `no-store`.
- OpenAPI e contenuti pubblici: policy esplicita, non ereditata da un blocco globale.
- Token, claim completi, issuer, oid, familyId, nomi e payload non entrano in log, metriche o trace.

## Configurazione e avvio

Entra, database, storage ed exporter osservabilita usano options tipizzate e `ValidateOnStart`. La validazione puo essere condizionata per ambiente, ma disabilitare Entra non crea utenti fittizi e gli endpoint protetti falliscono chiusi.

L'avvio non esegue scansioni arbitrarie o chiamate remote. La cache dei metadata Function e deterministica e limitata agli entry point noti.

## OpenAPI

Route e documento OpenAPI condividono costanti. Ogni operation dichiara security, parametri, risposte, media type e Problem Details applicabili. Un test confronta tutte le route HTTP con il documento e fallisce se un endpoint e assente o documentato con accesso errato.

## Osservabilita

Usare un'unica pipeline Azure Monitor OpenTelemetry. Gli operation scope applicativi usano tempo monotono, registrano una sola durata e un solo outcome e impostano status/tag sull'activity. Le dimensioni devono essere finite e a bassa cardinalita.

Non mantenere contemporaneamente SDK Application Insights classica ed exporter OpenTelemetry. La procedura di configurazione e verifica e in `docs/operations/observability.md`.

## Test richiesti

- Ordine, short circuit e risposta gia iniziata dei middleware.
- Default `ApiAccess`, `[AllowAnonymous]`, `[RequiresFamilyAccess]` e cache metadata.
- Token, scope, claim, `familyId` e membership validi e invalidi.
- Correlation ID e no-store anche sui fallimenti.
- Problem Details senza dettagli sensibili per `400`/`401`/`403`/`500`/`503`.
- Cancellazione non convertita in errore interno.
- Validator di configurazione.
- Parita route/OpenAPI.
- Emissione singola di trace e metriche.
- Almeno uno smoke test attraverso il worker Functions reale.
