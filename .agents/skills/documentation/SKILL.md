---
id: kinhub-documentation
name: KinHub documentation rules
version: 0.2.0
area: documentation
description: Guide bilingui, help contestuale e sincronizzazione Markdown nel frontend.
references: docs/CR/README.md
---

# KinHub documentation

## Scopo

Mantenere una sola fonte Markdown e documentazione contestuale completa.

## Quando usare

Per ogni route o funzionalità visibile all'utente.

## Quando non usare

La documentazione tecnica interna può restare in italiano se non è mostrata agli utenti. Una CR non riscrive retroattivamente requisiti o piano della feature consegnata.

## Componenti e servizi disponibili

Guide `docs/user-guide`, registry route, help i18n, `tools/docs-sync` e convenzione CR per preservare feature e piani originari.

## API e interfacce

Il frontmatter richiede `slug`, `locale`, `title`, `description`; gli slug devono avere coppia it/en.

## Esempi

Vedi `templates/user-guide.md`, `docs/CR/README.md` e le guide esistenti.

## Dipendenze

Node.js standard library, i18next e ReactMarkdown.

## Vincoli

Ogni route, 404 ed errore deve avere titolo, help it/en e guida collegata. Per modifiche a feature esistenti conserva `feature.md`/`feature.plan.md` e aggiungi `cr.md`/`cr.plan.md`.

## Test richiesti

`npm run docs:validate`, `npm run docs:sync` e `npm run routes:validate`.

## Checklist di aggiornamento

Aggiorna entrambe le lingue, route registry/help, guida, fragment e rigenera contenuti.

## Changelog

0.2.0: aggiunta la convenzione versionata per Change Request e piani correttivi.

0.1.0: pipeline documentale iniziale.
