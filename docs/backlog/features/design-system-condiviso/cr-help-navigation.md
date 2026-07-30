# CR-FEAT-014-001 - Semplificare help e navigazione informativa

- **Feature interessata**: FEAT-014 `design-system-condiviso`
- **Tipo**: modifica UX e navigazione
- **Stato**: implementata
- **Breaking change prodotto**: no
- **Piano**: implementato nella stessa modifica
- **Piano originario**: `feature.md`

## Motivazione

L'help contestuale oggi viene renderizzato come accordion in ogni pagina. La navigazione informativa globale espone invece soltanto Note di rilascio e Versione, mentre la guida utente è raggiungibile solo dalle singole pagine. La CR propone una shell più essenziale e un accesso diretto al manuale utente.

## Comportamento attuale

- `PageScaffold` renderizza `PageHelpAccordion` immediatamente dopo il titolo per ogni route.
- Il menu `InformationMenu` contiene i link a `/release-notes` e `/about`.
- Il manuale è disponibile attraverso le route `/docs/:slug`, con la guida `getting-started` come punto di ingresso.

## Comportamento desiderato

- Le accordion di help contestuale non sono visualizzate nelle pagine.
- Il menu Informazioni mantiene Note di rilascio e Versione.
- Il menu Informazioni aggiunge una voce localizzata che apre la pagina della guida utente (`/docs/getting-started`).
- La rimozione dell'accordion non rimuove i contenuti bilingui, le guide Markdown, la registrazione delle route o l'accessibilità del menu.

## Scope

- Nascondere o disabilitare la resa delle `PageHelpAccordion` nella shell pagina in modo centralizzato.
- Estendere il contratto del menu informativo con etichetta e percorso della guida utente.
- Aggiornare `Layout`, `FloatingBars`, i18n `it`/`en`, test e documentazione/help pertinenti.
- Verificare responsive, tastiera, focus, temi e navigazione diretta alla guida.

## Fuori scope

- Eliminare le guide Markdown o i JSON generati dalla documentazione.
- Rimuovere il route registry o i requisiti di help delle route.
- Cambiare il contenuto funzionale delle guide.
- Aggiungere un nuovo sistema di navigazione o una nuova route manuale.

## Dipendenze

Nessuna. La CR modifica componenti già presenti nella shell condivisa e può essere sviluppata come correzione autonoma di FEAT-014.

## Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-001 | closed | `AGENTS.md` richiedeva una `PageHelpAccordion` visibile in ogni route; la regola è stata aggiornata per demandare l'help alla guida utente globale quando opportuno | `AGENTS.md` aggiornato e verifiche frontend verdi |
| TECH-001 | closed | Il manuale usa la route parametrica `/docs/:slug` e il link deve puntare alla guida di ingresso esistente | `npm run routes:validate` e test frontend dimostrano la navigazione a `/docs/getting-started` |

## Criteri di accettazione

- **AC-001**: ogni pagina non renderizza più il trigger o il contenuto dell'accordion help.
- **AC-002**: il menu Informazioni mostra Note di rilascio, Versione e Manuale utente in italiano e inglese.
- **AC-003**: selezionando Manuale utente si apre `/docs/getting-started` e il titolo della guida è localizzato secondo la lingua attiva.
- **AC-004**: il menu resta accessibile da tastiera, mantiene il focus gestito da Radix e funziona nei temi light/dark e su viewport mobili.
- **AC-005**: `npm run i18n:validate`, `npm run routes:validate`, test, lint, typecheck, build e `npm run design-system:validate` passano.

## Verifica prevista

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Frontend/component | Rendering di `PageScaffold` e `InformationMenu` | Nessun accordion; tre link informativi con percorso corretto |
| Accessibilità/manuale | Tastiera, focus, temi e viewport mobile | Menu utilizzabile senza mouse e guida raggiungibile |
| Validator repository | i18n, route, design system, lint, typecheck, test e build | Tutti gli esiti verdi |

## Definition of Done

- I criteri di accettazione sono verificati.
- Le traduzioni `it`/`en`, i test e la documentazione applicabile sono aggiornati.
- Le guide restano disponibili e il route registry resta coerente.
- Non sono introdotti componenti, stringhe o stili duplicati.
