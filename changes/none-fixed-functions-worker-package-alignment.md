---
type: fixed
area: backend
breaking: false
issue: none
---

## it
Allineate le librerie Azure Functions Worker e gRPC alla versione richiesta dall'integrazione OpenTelemetry, rimossa una configurazione runtime non supportata, resa univoca la configurazione dello storage host con managed identity su Flex Consumption e neutralizzati i placeholder applicativi che causavano crash di bootstrap quando mancavano override ambientali.

## en
Aligned the Azure Functions Worker and gRPC libraries with the version required by the OpenTelemetry integration, removed an unsupported runtime setting, made the managed-identity host storage configuration unambiguous on Flex Consumption, and neutralized application placeholders that caused bootstrap crashes when environment overrides were missing.
