---
id: kinhub-documentation
name: KinHub documentation rules
version: 0.1.0
area: documentation
description: Guide bilingui, help contestuale e sincronizzazione Markdown nel frontend.
---

# KinHub documentation

## Scopo

Mantenere una sola fonte Markdown e documentazione contestuale completa.

## Quando usare

Per ogni route o funzionalità visibile all'utente.

## Quando non usare

La documentazione tecnica interna può restare in italiano se non è mostrata agli utenti.

## Componenti e servizi disponibili

Guide `docs/user-guide`, registry route, help i18n e `tools/docs-sync`.

## API e interfacce

Il frontmatter richiede `slug`, `locale`, `title`, `description`; gli slug devono avere coppia it/en.

## Esempi

Vedi `templates/user-guide.md` e le guide esistenti.

## Dipendenze

Node.js standard library, i18next e ReactMarkdown.

## Vincoli

Ogni route, 404 ed errore deve avere titolo, help it/en e guida collegata.

## Test richiesti

`npm run docs:validate`, `npm run docs:sync` e `npm run routes:validate`.

## Checklist di aggiornamento

Aggiorna entrambe le lingue, route registry/help, guida, fragment e rigenera contenuti.

## Changelog

0.1.0: pipeline documentale iniziale.
