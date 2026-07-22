# Osservabilita backend

## Standard

KinHub usa OpenTelemetry nel worker .NET Isolated e Azure Monitor come exporter verso Application Insights. Log, metriche e trace applicativi condividono correlazione e configurazione; non affiancare una seconda pipeline Application Insights classica.

La Function App registra OpenTelemetry nel composition root con `UseFunctionsWorkerDefaults`, `UseAzureMonitorExporter`, `ActivitySource` `KinHub` e `Meter` `KinHub`. La credential dell'exporter e esplicita: `DefaultAzureCredential` in Development e managed identity system-assigned negli altri ambienti.

## Configurazione

- Registrare `UseFunctionsWorkerDefaults` e Azure Monitor exporter una sola volta nel composition root.
- Registrare esplicitamente ogni `ActivitySource` e `Meter` custom.
- Mantenere `APPLICATIONINSIGHTS_CONNECTION_STRING` fuori dal codice.
- In Azure usare la system-assigned managed identity e il ruolo least privilege `Monitoring Metrics Publisher` gia assegnato dal Bicep.
- Configurare `host.json` nella modalita OpenTelemetry supportata dalla versione Functions in uso.
- Validare all'avvio opzioni e combinazioni credential, senza fallback a secret versionati.
- Non usare `logging.applicationInsights` in `host.json` quando `telemetryMode` e `OpenTelemetry`.

## Strumentazione

- Usare `ILogger` per eventi discreti, errori e contesto operativo.
- Usare `ActivitySource` per operazioni correlate e dipendenze logiche.
- Usare `Meter`, counter e histogram per esiti e durate aggregate.
- Misurare durate con `Stopwatch` o `TimeProvider.GetTimestamp`, non con differenze di `DateTime.UtcNow`.
- Uno scope operazione registra esattamente un outcome e una durata, anche in caso di errore.
- Usare solo dimensioni finite come `operation`, `outcome` ed `errorCategory`.

Non registrare token, password, connection string, claim completi, issuer, oid, familyId, nomi, codici invito, payload o dati funzionali non necessari.

## Logging

Il correlation middleware apre uno scope con `CorrelationId`; il trace distribuito rimane in `Activity.TraceId`. Il middleware eccezioni logga la causa di `500` e `503`, mentre normali `400`, `401` e `403` non sono errori server.

Configurare livelli separati per namespace KinHub e framework. EF Core command logging resta almeno `Warning` fuori da troubleshooting controllato. Non usare logging ad alta cardinalita per sostituire metriche aggregate.

## Verifica locale

- Usare `ActivityListener` nei test per verificare nome, status e tag delle activity.
- Usare `MeterListener` per verificare counter, histogram, dimensioni ed emissione singola.
- Verificare la DI dell'exporter senza richiedere rete nei test unitari.
- Eseguire uno smoke test con Functions Core Tools per propagazione del trace e serializzazione degli errori.

## Verifica Azure

Dopo il deploy verificare in Application Insights:

1. request HTTP presenti e correlate;
2. dependency PostgreSQL/HTTP presenti;
3. trace applicativi collegati allo stesso trace;
4. metriche custom KinHub con sole dimensioni approvate;
5. eccezioni tecniche senza dettagli sensibili;
6. assenza di duplicati fra host Functions e worker;
7. sampling coerente con i volumi e request operative non perse.

Se l'ingestione fallisce, controllare connection string, modalita `host.json`, credential dell'exporter, ruolo della managed identity e diagnostica dell'exporter. Non riattivare una seconda SDK come fallback permanente: ripristinare il pacchetto N-1 oppure correggere la configurazione e ridistribuire.
