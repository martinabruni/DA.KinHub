---
id: frontend
name: frontend
area: frontend
description: Pattern React condivisi, routing documentato, i18n e accessibilità
version: 1.0.0
---

# Frontend KinHub

## Scopo

Guidare modifiche al frontend React senza perdere i vincoli KinHub: interfaccia essenziale, mobile-first, localizzata, accessibile e documentata. La skill distingue sempre ciò che esiste da ciò che è soltanto il pattern desiderato.

## Quando usare

Leggere questa skill prima di creare o modificare route, layout, componenti condivisi, traduzioni, tema, onboarding, gestione PWA o accesso API/MSAL. Consultare `catalog.json` prima di introdurre un nuovo componente: riusare un asset `stable`, consolidare uno `scaffold`, implementare un `planned` prima di importarlo.

## Quando non usare

Non usarla per regole di dominio o persistenza. Non copiare esempi della skill in produzione senza build e controlli. Le descrizioni non sono moduli caricabili e il catalogo non rende reale un componente assente.

## Componenti e servizi disponibili

Il [catalogo frontend](catalog.json) è l'inventario autorevole. Oggi `src/frontend/src/main.tsx` contiene un unico scaffold con `App`, `Page`, routing, dizionario locale e selettori lingua/tema. Non esistono ancora componenti shadcn/ui, un vero `PageHelpAccordion`, un route registry documentale, provider i18next/MSAL o onboarding: sono gap espliciti, non API disponibili.

Stati ammessi nel catalogo:

- `stable`: riusabile, testato e documentato;
- `scaffold`: presente ma non ancora conforme al contratto target;
- `planned`: contratto deciso, implementazione assente; vietato importarlo.

## API e interfacce

Il contratto target di una route è un record immutabile con `id`, `path`, `titleKey`, `helpKey`, `guideSlug` ed `element`. Ogni chiave deve esistere in `it` ed `en`. Il componente pagina rende nell'ordine `h1`, `PageHelpAccordion`, contenuto. Anche 404 ed error boundary seguono il contratto. Finché il registry non è implementato, non aggiungere tuple isolate all'array `routes`: prima promuovere lo scaffold al contratto target.

Il contratto target dell'help riceve l'id route, è chiuso di default, usa primitive Accordion compatibili shadcn/ui, espone tastiera/ARIA e legge contenuto localizzato (scopo, azioni, prerequisiti, campi, limiti, link guida). Non accetta testo visibile passato inline.

## Esempi

Flusso per una nuova pagina: aggiungere prima guida `docs/user-guide/{it,en}/<slug>.md`; aggiungere chiavi parallele; registrare la route completa; renderizzare il layout condiviso; validare route/help e traduzioni. Un selettore deve localizzare anche le opzioni (`Sistema`, `Chiaro`, `Scuro`), non soltanto l'`aria-label`.

Flusso di promozione UI: verificare almeno due consumatori reali; spostare il componente nella cartella shared corretta; definire props minime; eliminare stringhe inline; aggiungere esempio e controllo statico; aggiornare catalogo con path/export/stato; aggiornare skill, docs e fragment; eseguire `npm run skills:build`.

## Dipendenze

React, React Router, TypeScript strict, i18next/react-i18next, MSAL, Vite PWA e primitive shadcn/ui-compatible. Verificare `src/frontend/package.json`: la presenza di un pacchetto non prova che sia configurato o usato.

## Vincoli

Nessuna stringa visibile hardcoded, incluse option, errori, empty state e testi ARIA. Italiano default, inglese fallback documentato, chiavi in parità. Persistenza locale robusta a valori invalidi. Tema `system` deve seguire `prefers-color-scheme` senza flash. Focus visibile, navigazione tastiera, dialog con focus management e animazioni disattivate con reduced motion. Nessun client secret nel bundle.

## Test richiesti

Eseguire da `src/frontend`: installazione riproducibile con `npm ci`, `npm run lint`, `npm run build`, validazione i18n e route/help. Per un asset promosso aggiungere almeno un esempio verificabile; se introduce logica non banale, aggiungere il test minimo anche se il bootstrap non richiede una suite UI completa.

## Checklist di aggiornamento

1. Verificare lo stato reale nel catalogo e nel file sorgente.
2. Implementare nel layer feature/shared appropriato senza ampliare l'API inutilmente.
3. Aggiungere traduzioni it/en e guida/help route.
4. Verificare mobile, tastiera, temi e reduced motion.
5. Aggiornare esempio, catalogo (path/export/status) e questa skill.
6. Aggiungere change fragment e rigenerare il registry.
7. Eseguire tutti i controlli richiesti e registrare quelli non eseguibili.

## Changelog

Le variazioni sono tracciate nei change fragment e nelle patch note KinHub. Modificare `version` nel front matter quando cambia il contratto operativo della skill, non per semplici correzioni ortografiche.

