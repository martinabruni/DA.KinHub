---
id: kinhub-release
name: KinHub release rules
version: 0.1.0
area: release
description: Semantic Versioning, change fragment, patch note e metadati di build condivisi.
---

# KinHub release

## Scopo

Produrre versioni tracciabili e note bilingui da dati versionati.

## Quando usare

Per ogni modifica significativa, release e packaging backend/frontend.

## Quando non usare

Non incrementare versioni in file duplicati o durante esperimenti non pubblicati.

## Componenti e servizi disponibili

`VERSION`, `CHANGELOG.md`, `changes/`, release tool, `/api/version` e pagina Versione.

## API e interfacce

I fragment dichiarano type, area, breaking, issue e sezioni `## it`/`## en`.

## Esempi

Vedi `templates/change-fragment.md` e `changes/README.md`.

## Dipendenze

Git, Node.js, MSBuild, GitHub Actions e vite define.

## Vincoli

SemVer proviene solo da `VERSION`; ZIP backend contiene versione e SHA; nessun secret nei metadata.

## Test richiesti

Validazione fragment, generazione note/JSON e verifica endpoint/pagina versione.

## Checklist di aggiornamento

Aggiungi fragment, aggiorna versione una volta, genera note, verifica artefatti e changelog.

## Changelog

0.1.0: sistema release iniziale.
