---
id: kinhub-frontend
name: KinHub frontend patterns
version: 0.2.0
area: frontend
description: Pattern UI React riutilizzabili, accessibili, localizzati e compatibili con temi e PWA.
catalog: catalog.json
---

# KinHub frontend

## Scopo

Guidare implementazione e promozione di pagine e componenti React condivisi.

## Quando usare

Per route, layout, controlli, help contestuale, i18n, tema, onboarding e PWA.

## Quando non usare

Non contiene regole di dominio o contratti persistence.

## Componenti e servizi disponibili

`PageScaffold`, `PageHelpAccordion`, primitive `components/ui`, `FloatingBars`, `KinPatterns`, `ThemeProvider`, onboarding e client API.

## API e interfacce

Ogni pagina usa `<PageScaffold routeId="...">`. Ogni route è registrata in `route-registry.json` con titolo, help e slug guida. La shell monta una sola floating navigation globale e, quando serve, una barra contestuale registrata nel contratto condiviso. I componenti visibili ricevono testo esclusivamente da i18next.

## Esempi

Vedi `examples/PageScaffold.example.tsx`, `examples/ShellBar.example.tsx` e le pagine in `src/frontend/src/pages`.

## Dipendenze

React, react-router-dom, i18next/react-i18next, Radix Accordion (shadcn/ui), MSAL e vite-plugin-pwa.

## Vincoli

Primitive prima dei wrapper specifici. Niente route demo prodotto, classi legacy parallele o import di librerie UI aggiuntive fuori dai touchpoint approvati. Mobile-first, keyboard, focus visibile, reduced motion, light/dark/system e nessun secret nel bundle.

## Test richiesti

`npm run test`, `npm run lint`, `npm run typecheck`, `npm run i18n:validate`, `npm run routes:validate`, `npm run design-system:validate`, `npm run build`.

## Checklist di aggiornamento

Implementa, riusa primitive ufficiali, aggiungi traduzioni it/en, help, guida, esempio, catalogo, change fragment e rigenera registry/docs.

## Changelog

0.2.0: promuovo primitive ufficiali, floating navigation e validator del design system condiviso.

0.1.0: componenti fondamentali del bootstrap; dettagli in `docs/patch-notes`.
