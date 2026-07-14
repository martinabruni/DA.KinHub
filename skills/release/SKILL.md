---
id: release
name: release
area: release
description: Versioning, change fragment, patch note e promozione verificata
version: 1.0.0
---

# Release KinHub

## Scopo

Definire un flusso auditabile da change fragment a artifact distribuito, usando `VERSION` una sola volta e pubblicando metadata coerenti tra API, frontend, container e patch note.

## Quando usare

Per ogni modifica significativa, preparazione release, modifica di VERSION, pipeline, immagini, metadata build, changelog o patch note.

## Quando non usare

Non usarla per nascondere controlli falliti o modificare infrastruttura via workflow code-only. Non creare note direttamente dal titolo commit quando esiste un fragment bilingue.

## Componenti e servizi disponibili

Il [catalogo release](catalog.json) descrive fonti e maturità. `VERSION` esiste; i fragment correnti usano due formati incompatibili (front matter delimitato e righe semplici); `tools/release-notes/index.mjs` controlla soltanto la presenza testuale di `type:`/`area:` e non aggrega nulla. Changelog, patch note JSON e metadata non sono ancora una pipeline completa.

## API e interfacce

`VERSION` contiene una SemVer valida senza duplicazioni manuali. Formato target fragment: front matter YAML con `type` in `added|changed|deprecated|removed|fixed|security`, `area`, boolean `breaking`, `it`, `en` e issue/PR opzionale. Il generatore ordina deterministicamente, rifiuta campi ignoti/duplicati, produce changelog, note it/en e JSON con versione, commit, build date e ambiente.

## Esempi

Un fix harness usa `type: fixed`, `area: skills`, descrizioni orientate all'utente/contributor e `breaking: false`. Una release `0.2.0` consuma i fragment soltanto dopo test, scrive sezioni Keep a Changelog e conserva tracciabilità. Non segnare come “Added” un requisito soltanto pianificato.

## Dipendenze

Tool release-notes, `VERSION`, change fragment, GitHub Actions, Docker/Bicep e frontend/API che espongono metadata.

## Vincoli

Patch note utente bilingui, categorie Keep a Changelog e breaking change evidente. Immagini taggate con versione e SHA. Deploy completo infra solo da tag `infra-*`; `main` aggiorna codice. OIDC preferito, secret mai stampati. Non dichiarare eseguito un controllo non realmente lanciato.

## Test richiesti

Parser testato con schema valido, campo mancante, enum errato, boolean errato e duplicato. Snapshot/output deterministico. Prima della promozione: backend build/test, frontend ci/lint/build, skill/docs/i18n/routes/fragments, Bicep e Docker se disponibile; smoke health/version dopo deploy.

## Checklist di aggiornamento

1. Aggiungere un fragment valido durante la modifica.
2. Completare DoD e registrare limitazioni reali.
3. Scegliere bump SemVer da impatto, aggiornando solo VERSION.
4. Generare e verificare changelog, patch note it/en e JSON.
5. Verificare metadata backend/frontend/container e tag SHA.
6. Eseguire pipeline appropriata e smoke check.
7. Archiviare/consumare fragment secondo la strategia documentata.

## Changelog

La cronologia pubblica è in `CHANGELOG.md` e nelle patch note localizzate; la skill cambia versione se mutano schema o procedura di release.

