# Frontend Design System

## Scopo

Questa guida definisce il contratto UI condiviso di KinHub per shell, route correnti e feature future. L'obiettivo e evitare una seconda libreria parallela, duplicazioni visive e classi legacy locali di pagina.

## Primitive ufficiali

- `src/frontend/src/components/ui/core.tsx`: azioni, superfici, field, tabs, paginazione, avatar e badge.
- `src/frontend/src/components/ui/controls.tsx`: select, checkbox, switch e campi specializzati sopra Radix.
- `src/frontend/src/components/ui/feedback.tsx`: `StatePanel`, dialog, drawer, tooltip e snackbar.
- `src/frontend/src/components/ui/accordion.tsx`: accordion help e disclosure accessibili.

## Pattern promossi

- `PageScaffold` resta obbligatorio per titolo, focus iniziale e help contestuale.
- `FloatingBars` possiede una sola floating navigation globale della shell e una barra contestuale opzionale registrata tramite `ShellBarContext`.
- `KinPatterns` contiene wrapper sottili specifici KinHub o KinList costruiti sopra le primitive ufficiali.

## Regole di riuso

- Primitive prima dei wrapper specifici.
- Un wrapper specifico e ammesso solo se riduce davvero duplicazione o semplifica un pattern ripetuto.
- Non usare route demo prodotto, classi `.button`, `.state-card`, `.settings-card`, `.feature-card`, `.card-grid`, `.control` o namespace `.ds-*`.
- Non introdurre librerie UI parallele fuori dai touchpoint approvati in `components/ui`.

## Accessibilita e semantica

- Le azioni che navigano usano `ButtonLink` o `Link`, non nesting improprio di `button` e `a`.
- `StatePanel` espone ruolo, live region, busy state e heading level configurabili per loading, empty, offline, forbidden ed error.
- `Tabs`, dialog, drawer e snackbar devono preservare focus, tastiera e reduced motion.

## Tema, safe area e shell

- La barra flottante e fissata al viewport e non dipende dallo scroll della pagina.
- Il contenuto lascia spazio persistente in basso per safe area, snackbar e floating navigation.
- `ThemeProvider`, `index.html`, manifest e icona condividono la stessa palette finale per evitare flash o mismatch.

## Localizzazione e validazione

- Tutte le stringhe visibili stanno in i18n `it` ed `en`.
- Ogni modifica frontend rilevante deve passare `npm run --prefix src/frontend design-system:validate` oltre a test, lint, typecheck, i18n e route validation.
