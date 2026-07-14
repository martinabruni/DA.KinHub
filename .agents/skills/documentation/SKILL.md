---
id: documentation
name: documentation
area: documentation
description: Guide bilingui, help contestuale e sincronizzazione Markdown
version: 1.0.0
---

# Documentazione KinHub

## Scopo

Definire ownership e flusso dei contenuti KinHub affinché guide, help in-app e traduzioni restino bilingui, navigabili e verificabili senza duplicazioni.

## Quando usare

Per route o comportamento visibile, nuove guide, messaggi di errore, onboarding, patch note e modifiche al formato documentale o a docs-sync.

## Quando non usare

Non usarla come deposito del testo utente e non documentare una funzione non implementata come disponibile. Documentazione tecnica interna senza impatto utente può restare in italiano.

## Componenti e servizi disponibili

Il [catalogo documentazione](catalog.json) riporta maturità e fonti reali. Oggi esiste soltanto `getting-started.md` in entrambe le lingue e `tools/docs-sync/index.mjs` verifica appena le directory: non valida front matter, slug, parità pagine, route o traduzioni. I messaggi “passed” dello scaffold non equivalgono a copertura reale.

## API e interfacce

Contratto target guida: stesso path relativo in `it`/`en`, front matter con `slug`, `title`, `summary`, `routeId` e ordine; link locali validi. Contratto route: `titleKey`, `helpKey`, `guideSlug`; help con scopo, azioni, prerequisiti, campi/azioni, limiti e link guida. Il frontend consuma output generato da Markdown, non una seconda copia manuale.

## Esempi

Per `/settings`: creare `docs/user-guide/it/settings.md` ed `en/settings.md` con stesso slug; aggiungere chiavi `routes.settings.title` e `routes.settings.help.*`; registrare `guideSlug: settings`; verificare che `PageHelpAccordion` segua immediatamente `h1`. Per rinominare uno slug pubblicato, prevedere redirect o compatibilità invece di spezzare link.

## Dipendenze

Route registry, risorse i18next, `PageHelpAccordion`, docs-sync e build frontend. Al momento diversi elementi sono target non ancora implementati: controllare il catalogo frontend.

## Vincoli

Italiano e inglese hanno identica copertura semantica, non necessariamente traduzione letterale. Nessuna stringa visibile resta nel JSX. Ogni route, inclusi errori e 404, ha titolo/help/guida e accordion subito dopo il titolo. Markdown non contiene HTML/script eseguibile non sanitizzato. Link e heading sono accessibili e stabili.

## Test richiesti

Il validatore deve confrontare l'insieme dei file it/en, front matter e slug; controllare link locali; confrontare ricorsivamente le chiavi i18n; incrociare ogni route; verificare output generato non stale. Finché docs-sync è scaffold, una review manuale non autorizza a dichiarare questi controlli superati.

## Checklist di aggiornamento

1. Identificare pubblico, route e fonte autorevole.
2. Scrivere it/en con stessa struttura e significato.
3. Collegare chiavi help e slug nel registry route.
4. Eseguire docs-sync, i18n e route validation.
5. Provare link, rendering mobile e tastiera.
6. Aggiornare catalogo/skill solo se cambia un pattern riusabile.
7. Aggiungere fragment e rigenerare registry skill.

## Changelog

Le modifiche rivolte agli utenti confluiscono nelle patch note bilingui. La versione della skill cambia per nuovi formati, responsabilità o procedure di validazione.

