---
status: In review
---

# FEAT-014 - Usare un design system condiviso in tutta KinHub

- **Codice**: `design-system-condiviso`
- **Tipo**: `enabler`
- **Readiness**: `ready`
- **Wave**: 2
- **Risultato**: tutte le pagine correnti di KinHub usano soltanto componenti del design system condiviso e le feature successive sono vincolate al loro riuso tramite harness e documentazione aggiornati.

## Contesto autonomo

Lo stato attuale del frontend combina componenti ad hoc, classi CSS specifiche di pagina e primitive riutilizzabili ancora parziali. Il prototipo approvato introduce palette calde, superfici tondeggianti, icone colorate, floating navigation bar a carosello, pattern KinService/KinList e componenti generici per azioni, form, feedback, overlay e navigazione. Questa feature trasforma il prototipo in fondazione stabile del prodotto: integra la nuova UI nelle route esistenti, elimina i componenti legacy e rende esplicito per le slice successive che non possono creare una seconda libreria parallela.

## Scope

### Incluso

- Token visuali, primitive generiche e pattern composti condivisi per bottoni, campi, card, badge, chip, avatar, stati, dialog, drawer, snackbar, tabs, paginazione e floating bars.
- Integrazione completa del design system nelle pagine esistenti (`/`, `/kinlist`, `/settings`, `/about`, `/release-notes`, guide, 404, error boundary e shell comune) senza lasciare componenti/stili legacy attivi.
- Sostituzione della navigazione/header attuale con il modello approvato di floating bar contestuale, inclusa la barra globale e la barra operativa di KinList come contratto per le future slice.
- Rimozione di classi CSS, componenti wrapper e markup specifici di pagina diventati ridondanti dopo la migrazione.
- Tutte le stringhe visibili in i18n, con componenti generici customizzabili e wrapper specifici solo quando riducono realmente duplicazione e boilerplate.
- Aggiornamento di skill/harness frontend e implementation, catalogo componenti, documentazione di riuso e, se la regola strutturale cambia, `AGENTS.md`.
- Possibilità di aggiungere nuove componenti se il catalogo approvato non basta, purché derivino da primitive generiche e non introducano duplicazione.

### Escluso

- Nuove capacità di prodotto non richieste dal design system approvato.
- Fork paralleli tra design system e UI legacy, mantenimento di route demo come superficie definitiva o doppia implementazione dello stesso pattern.
- Refactor backend, dati o API che non siano necessari a supportare la sola integrazione UI.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-001, FLOW-002, FLOW-009, FLOW-010 | Reimpacchetta onboarding, shell, lista e impostazioni con componenti condivisi |
| Requisiti | FR-027-FR-029, FR-049-FR-051 | PWA shell, temi, localizzazione, pagine documentate e stati UI sicuri |
| Regole/decisioni | Vincolo approvato di backlog sul design system condiviso; sezioni frontend/i18n/help di `AGENTS.md` | Nessun boilerplate, niente magic strings, riuso obbligatorio e rimozione legacy |
| Architettura | Sezioni 4, 5 e 6.3-6.8 di `docs/brainstorming/architecture.md` | Centralizzazione comportamenti comuni, componenti condivisi e navigazione coerente |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-001 - Entrare nel percorso corretto dopo il login | hard | Il design system va integrato sulla shell, sugli stati auth e sulle route correnti realmente esistenti | Routing, stati bootstrap/offline e contesto Auth/Theme funzionanti | Inizio dopo integrazione FEAT-001 |

### Gate e assunzioni

Nessuna.

### Parallelismo consentito

Nessuno nella wave. La feature congela catalogo componenti, token, convenzioni i18n e regole harness che le slice UI successive devono riusare senza ridefinirle.

## Contratto di consegna

### Comportamento

- Ogni pagina corrente sostituisce i propri elementi visuali con componenti del design system senza perdere tema, accessibilità, focus, responsive, help e localizzazione.
- La floating navigation bar è disponibile come contratto condiviso della shell; KinList espone la propria barra contestuale e le pagine generali la barra globale.
- I componenti specifici di dominio sono costruiti sopra primitive generiche; non esistono doppioni funzionali di button, card, field, state panel, drawer o snackbar.
- Le stringhe visibili non restano nei componenti; i wrapper di dominio ricevono testi da i18n e non reintroducono magic strings.
- Il completamento della feature rimuove stili, componenti e riferimenti legacy; la route temporanea `/design-system` resta solo se usa esattamente gli stessi componenti in produzione e non mantiene logica duplicata.

### Touchpoint previsti

- **Dominio/business**: Non pertinente salvo eventuali DTO/stati UI già esposti dai casi d'uso correnti.
- **Persistenza/migration**: Non pertinente.
- **API/integrazioni**: client API tipizzato e contratti già esistenti, senza cambiare comportamento server-side.
- **Frontend/UX**: `src/frontend/src/components/ui/`, `src/frontend/src/components/FloatingBars.tsx`, `src/frontend/src/components/KinPatterns.tsx`, `src/frontend/src/components/Layout.tsx`, `src/frontend/src/components/PageScaffold.tsx`, pagine correnti, route registry, risorse `it`/`en` e `styles.css`.
- **Infrastruttura/configurazione**: Nessuna nuova risorsa; solo eventuali asset/configurazioni frontend strettamente necessari alla UI.
- **Documentazione/operazioni**: `skills/frontend/SKILL.md`, `skills/frontend/catalog.json`, eventuali esempi skill, `skills/implementation/*`, guide/help bilingui, change fragment e `AGENTS.md` se le regole di riuso cambiano.

La proposta di modifica successiva su help e navigazione informativa è descritta in `cr-help-navigation.md`.

### Errori, sicurezza e osservabilità

- Nessuna regressione su stati di errore, `403`, sessione scaduta, offline, loading o empty: devono essere renderizzati tramite componenti condivisi senza mostrare dati stale.
- I componenti non introducono logica auth locale, secret o persistenze browser aggiuntive; tutto il testo resta localizzato e nessun contenuto sensibile entra in log/toast/debug UI.
- Telemetria e data attributes condivisi restano coerenti e a bassa cardinalita; eventuali eventi UI riusano nomi centralizzati invece di magic strings duplicate.

## Criteri di accettazione

### AC-078 - Pagine correnti migrate

- **Dato** le route oggi presenti in KinHub
- **Quando** la feature è integrata
- **Allora** Home, KinList bootstrap, Settings, Versione, Note di rilascio, guide, 404, error boundary e shell comune usano componenti del design system senza markup/stili legacy attivi
- **Fonte**: vincolo backlog design system, FR-027-FR-029

### AC-079 - Navigazione flottante condivisa

- **Dato** una pagina generale e una pagina KinService
- **Quando** l'utente interagisce con la navigazione inferiore
- **Allora** vede la floating navigation bar comune, il carosello contestuale e, in KinList, la barra operativa dedicata come default senza reimplementazioni separate
- **Fonte**: vincolo backlog design system, FLOW-002, FLOW-010

### AC-080 - Nessun legacy o duplicazione

- **Dato** il codice frontend finale
- **Quando** si ispezionano componenti, CSS, route demo e testi visibili
- **Allora** non restano vecchi componenti equivalenti, classi obsolete, boilerplate duplicato, componenti doppioni o magic strings fuori da i18n
- **Fonte**: vincolo backlog design system, `AGENTS.md`, architettura sezione 5

### AC-081 - Primitive e wrapper coerenti

- **Dato** componenti generici e componenti specifici KinHub/KinList
- **Quando** si confrontano casi d'uso diversi
- **Allora** i wrapper specifici estendono primitive condivise invece di duplicarle e l'aggiunta di nuove componenti segue lo stesso contratto
- **Fonte**: vincolo backlog design system, architettura sezioni 4 e 5

### AC-082 - Documentazione e harness vincolanti

- **Dato** una futura feature frontend o full-stack con superficie UI
- **Quando** consulta skill, catalogo e istruzioni repository
- **Allora** trova obbligatorio il riuso del design system, i touchpoint ufficiali e i limiti contro boilerplate, duplicazioni e magic strings
- **Fonte**: vincolo backlog design system, `AGENTS.md`

### AC-083 - Stati e temi preservati

- **Dato** temi light/dark, tastiera, reduced motion e stati asincroni correnti
- **Quando** le pagine migrate vengono usate
- **Allora** help, focus, animazioni, loading, empty, error, offline, auth e responsive restano comprensibili e coerenti con i componenti condivisi
- **Fonte**: FR-028, FR-051, NFR-009, NFR-010

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Funzioni helper, token mapping, varianti e utility di composizione | Test mirati frontend/utilita oppure N/A motivato |
| Integrazione | Route correnti, shell, i18n/help e componenti condivisi senza regressioni | Test frontend e validatori route/i18n/docs |
| Frontend/component | Primitive, floating bars, stati, temi, focus, reduced motion e wrapper specifici | Test componenti/accessibilità |
| End-to-end/manuale | Navigazione reale tra pagine correnti, cambio tema/lingua, auth visibile e carosello barre | Evidenza desktop/mobile/PWA dove applicabile |
| Validator repository | `npm run lint`, `npm run typecheck`, `npm run i18n:validate`, `npm run routes:validate`, `npm run build`, validatori docs/skills/release applicabili | Esiti registrati |

## Definition of Done

- Tutti i criteri di accettazione sono verificati e FEAT-001 è integrata.
- Le pagine correnti non conservano componenti, classi CSS o layout legacy equivalenti al design system approvato.
- Catalogo componenti, skill frontend, harness implementation, help/guide `it`/`en`, i18n, route registry e change fragment sono aggiornati in modo non ambiguo.
- Le nuove componenti introdotte sono generiche o wrapper espliciti sopra primitive condivise; non esistono duplicazioni o stringhe visibili hardcoded.
- I comandi di qualità applicabili di `AGENTS.md` sono eseguiti e riportati.
- La feature è completa senza richiedere una slice futura per rimuovere il legacy UI o imporre il riuso del design system.
