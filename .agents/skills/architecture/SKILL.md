---
id: kinhub-architecture
name: KinHub architecture rules
version: 0.2.0
area: architecture
description: Confini DDD, decisioni serverless e procedura per nuove convenzioni strutturali.
references: docs/architecture/overview.md, docs/architecture/http-functions.md
---

# KinHub architecture

## Scopo

Proteggere dipendenze, semplicità operativa e coerenza Azure.

## Quando usare

Per nuovi layer, servizi Azure, dipendenze trasversali e decisioni con impatto strutturale.

## Quando non usare

Non serve per modifiche locali che seguono già una convenzione documentata.

## Componenti e servizi disponibili

Monolite modulare DDD, SPA/PWA, API serverless, PostgreSQL e tool deterministici. Le HTTP Function usano una pipeline middleware corta e responsabilita esplicite invece di base class o executor generici.

## API e interfacce

Dipendenze: Applications → Business/Infrastructure → Domain; Business → Domain; Domain → nessun framework.

## Esempi

Vedi `docs/architecture/overview.md` e `templates/adr.md`.

## Dipendenze

.NET, React, PostgreSQL, Azure Functions, Static Web Apps e Bicep.

## Vincoli

No CQRS/mediator senza motivazione; no codice dinamico dalle skill; un piano Flex per Function App. Comportamenti trasversali HTTP centralizzati senza rendere ambientali identita o scope nel Business.

## Test richiesti

Build completa, test dei confini interessati e validazione Bicep/tool.

## Checklist di aggiornamento

Registra decisione, aggiorna AGENTS, diagrammi/documenti, skill, test e change fragment.

## Changelog

0.2.0: documentata la pipeline HTTP Functions centralizzata e sicura per default.

0.1.0: architettura bootstrap iniziale.
