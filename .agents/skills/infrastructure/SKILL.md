---
id: kinhub-infrastructure
name: KinHub infrastructure and delivery patterns
version: 0.1.0
area: infrastructure
description: Bicep, Azure adoption safety and GitHub Actions delivery for the single dev environment.
references: docs/backlog/features/riallineamento-infrastruttura-dev/infra-guidelines.md
---

# KinHub infrastructure

## Scopo

Mantenere un provisioning Azure esplicito, incrementale e verificabile prima dell'applicazione.

## Quando usare

Per Bicep, parameter file, risorse Azure, OIDC, workflow CI/CD, what-if, packaging e smoke test.

## Quando non usare

Non usare per contratti di dominio, componenti React o migrazioni applicative isolate.

## Componenti e servizi disponibili

`infra/main.bicep` e' l'entry point resource-group scoped. `infra/environments/dev.bicepparam` contiene i nomi confermati. I workflow ammessi sono `ci.yml`, `infrastructure.yml` e `release.yml`.

## API e interfacce

Il provisioning usa Bicep e deployment ARM incremental. La release legge gli output del deployment stabile `kinhub-dev-infrastructure`; non esegue discovery euristica dei nomi. I job fidati usano OIDC e gli artifact di release hanno retention di 30 giorni.

## Esempi

```bash
az bicep build --file infra/main.bicep
az bicep build-params --file infra/environments/dev.bicepparam
```

## Dipendenze

Azure CLI con Bicep, Azure Resource Manager, GitHub Actions, .NET SDK, Node.js e gli script di packaging versionati.

## Vincoli

- Non usare `uniqueString`, `namingPrefix` o nomi generati.
- Non usare `pull_request_target` o secret Azure nella CI delle pull request.
- Fissare le action esterne a SHA completi.
- Eseguire validate e what-if immediatamente prima del deploy.
- Bloccare delete, replacement e modifiche distruttive a PostgreSQL o rete.
- Usare una sola Function App per piano Flex e `/api` per il linked backend Static Web Apps.

## Test richiesti

Eseguire Bicep format/build/build-params, actionlint quando disponibile, validatori repository, build/test/package e, con credenziali, validate/what-if e smoke test live.

## Checklist di aggiornamento

Aggiornare template, parametri, workflow, documentazione operativa, fragment bilingue e registry generato. Verificare i nomi repository-wide prima di rimuovere un consumer.

## Changelog

0.1.0: introdotti entry point Bicep esplicito, adozione sicura e tre workflow non riusabili.
