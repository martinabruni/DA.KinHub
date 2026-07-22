---
type: fixed
area: backend
breaking: false
issue: none
---

## it
Corretta la registrazione OpenTelemetry della Function App per non inizializzare l'exporter Azure Monitor quando `APPLICATIONINSIGHTS_CONNECTION_STRING` non e configurata, evitando il crash dell'host locale e coprendo il caso con un test di regressione.

## en
Fixed the Function App OpenTelemetry registration to avoid initializing the Azure Monitor exporter when `APPLICATIONINSIGHTS_CONNECTION_STRING` is not configured, preventing local host startup crashes and covering the case with a regression test.
