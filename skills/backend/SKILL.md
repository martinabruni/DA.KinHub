---
id: kinhub-backend
name: KinHub backend patterns
version: 0.1.0
area: backend
description: Servizi business, contratti DDD, Function endpoint e pattern infrastrutturali riutilizzabili.
catalog: catalog.json
---

# KinHub backend

## Scopo

Mantenere servizi e contratti .NET riutilizzabili con dipendenze orientate verso il dominio.

## Quando usare

Per entità, value object, use case, repository, endpoint Function, Problem Details e build metadata.

## Quando non usare

Non usare per testo UI, Bicep o workflow di release.

## Componenti e servizi disponibili

`ProjectService`, `BuildInfoProvider`, `ApiResults`, repository EF, migration initializer locale e `IDocumentStorage` con adapter Azure Blob/Azurite.

## API e interfacce

I servizi business e storage espongono interfacce async con `CancellationToken`. `IDocumentStorage` salva contenuti tramite chiavi opache e non espone credenziali; gli endpoint REST restituiscono JSON o RFC 7807 e propagano `X-Correlation-ID`.

## Esempi

Vedi `examples/ProjectService.example.cs`, `examples/DocumentStorage.example.cs` e i test business/integration.

## Dipendenze

.NET 10, Azure Functions Isolated 4.x, EF Core 10, Npgsql, Azure Blob Storage e Application Insights Worker.

## Vincoli

Il dominio non dipende da EF o Azure. Niente migration di produzione al cold start. Niente log di token, password o dati sensibili.

## Test richiesti

Regola di dominio, validazione business, DI, configurazione critica, endpoint metadata e Problem Details.

## Checklist di aggiornamento

Implementa nel layer corretto, aggiungi test/esempio, aggiorna catalogo e documentazione, crea fragment e rigenera registry.

## Changelog

0.1.0: servizi iniziali progetto, metadata e storage documentale; dettagli in `docs/patch-notes`.
