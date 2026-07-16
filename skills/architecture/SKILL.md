---
id: kinhub-architecture
name: KinHub architecture rules
version: 0.1.0
area: architecture
description: Confini DDD, decisioni serverless e procedura per nuove convenzioni strutturali.
---

# KinHub architecture

## Scopo

Proteggere dipendenze, semplicità operativa e coerenza Azure.

## Quando usare

Per nuovi layer, servizi Azure, dipendenze trasversali e decisioni con impatto strutturale.

## Quando non usare

Non serve per modifiche locali che seguono già una convenzione documentata.

## Componenti e servizi disponibili

Monolite modulare DDD, SPA/PWA, API serverless, PostgreSQL e tool deterministici.

## API e interfacce

Dipendenze: Applications → Business/Infrastructure → Domain; Business → Domain; Domain → nessun framework.

## Esempi

Vedi `docs/architecture/overview.md` e `templates/adr.md`.

## Dipendenze

.NET, React, PostgreSQL, Azure Functions, Static Web Apps e Bicep.

## Vincoli

No CQRS/mediator senza motivazione; no codice dinamico dalle skill; un piano Flex per Function App.

## Test richiesti

Build completa, test dei confini interessati e validazione Bicep/tool.

## Checklist di aggiornamento

Registra decisione, aggiorna AGENTS, diagrammi/documenti, skill, test e change fragment.

## Changelog

0.1.0: architettura bootstrap iniziale.
