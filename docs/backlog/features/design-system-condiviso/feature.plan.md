# Piano di implementazione - FEAT-014

## Obiettivo

Integrare il design system condiviso in tutta KinHub, sostituire completamente la UI legacy delle route correnti, centralizzare shell e floating navigation sopra primitive riusabili e rendere il loro riuso obbligatorio tramite validator, skill, catalogo e documentazione, senza introdurre funzionalita prodotto fittizie o una seconda libreria parallela.

## Decisioni esecutive

- La route laboratorio `/design-system` viene rimossa dalla superficie finale.
- Gli esempi utili del laboratorio vengono trasferiti a test, skill ed esempi versionati, non mantenuti come route prodotto.
- La shell possiede una sola floating bar condivisa; le pagine non montano barre indipendenti.
- KinList registra azioni contestuali tramite un contratto condiviso, ma espone soltanto azioni realmente funzionanti nello stato corrente del prodotto.
- Filtro, voce e selezione multipla non vengono simulati con controlli demo interattivi.
- FEAT-001 resta il prerequisito autorevole; FEAT-002 gia presente in `KinListAccessGate` viene migrata senza regressioni.
- La feature non modifica backend, schema dati o API salvo gli adattamenti strettamente necessari ai test o ai validator frontend.

## 1. Baseline e preflight

Prima di modificare il frontend:

1. Confermare che FEAT-001 sia completa nel codice corrente e nel backlog.
2. Riconoscere che il worktree contiene gia modifiche utente e file non tracciati del prototipo; non vanno sovrascritti o ripristinati.
3. Lavorare nel contenitore autorevole `docs/backlog/features/design-system-condiviso/`.
4. Portare la feature da `Open` a `In progress` all'avvio dell'implementazione effettiva.
5. Trattare la UI di creazione famiglia gia presente in `/kinlist` come parte della migrazione, non come feature separata da rinviare.

## 2. Stabilizzare primitive e token condivisi

Consolidare e correggere i touchpoint:

- `src/frontend/src/components/ui/core.tsx`
- `src/frontend/src/components/ui/controls.tsx`
- `src/frontend/src/components/ui/feedback.tsx`
- `src/frontend/src/components/ui/accordion.tsx`
- `src/frontend/src/styles.css`

Interventi richiesti:

- impostare `type="button"` come default delle primitive button quando non esplicitato;
- introdurre una variante semanticamente corretta per azioni che navigano, evitando nesting improprio di link e button;
- permettere alle primitive di ricevere attributi HTML, ARIA e ref necessari ai casi reali;
- estendere `StatePanel` con ruolo, live region, `aria-busy`, azioni e livello del titolo configurabili;
- rendere `Dialog` e `Drawer` utilizzabili anche in modalita controlled, con focus management coerente;
- completare `Tabs` con semantica, tastiera e associazioni ARIA corrette;
- localizzare completamente `Pagination` e i relativi nomi accessibili;
- correggere il contratto di `TextField` per helper, errori e `aria-describedby` stabili;
- coordinare safe area, overlay e snackbar nelle primitive di feedback;
- eliminare API incoerenti o errori statici del prototipo, inclusi i passaggi di prop non supportate.

Prima di migrare le pagine, aggiungere test mirati delle primitive condivise per semantica, focus, varianti e accessibilita.

## 3. Consolidare i pattern KinHub e KinList

Rifattorizzare `src/frontend/src/components/KinPatterns.tsx` affinche:

- `FeatureCard`, `KinServiceCard` e `ComingSoonServiceCard` compongano una sola base condivisa;
- `KinListItem` sia costruito sopra controlli e stati condivisi, non sopra button raw ad hoc;
- `FamilyCard`, `MemberRow` e `InviteRow` restino wrapper sottili sopra primitive generiche;
- nessun wrapper reintroduca stringhe hardcoded o markup equivalente alle primitive gia esistenti.

Aggiungere test per composizione, nomi accessibili, semantica dei link e stati interattivi.

## 4. Integrare la floating navigation nella shell

Rifattorizzare `src/frontend/src/components/FloatingBars.tsx` e integrarlo in `src/frontend/src/components/Layout.tsx`.

Il contratto finale deve:

- montare una sola floating bar posseduta dalla shell;
- derivare route attiva, transizioni e link da React Router, non da valori hardcoded;
- collegarsi a tema reale, lingua reale e stato MSAL reale;
- rimuovere nome, iniziali e stato attivo hardcoded;
- mantenere i `data-tour` richiesti dal tutorial;
- rendere le pagine inattive del carosello non focalizzabili e non esposte agli screen reader;
- supportare swipe, tastiera, indicatori, `prefers-reduced-motion` e reset al cambio route;
- riservare spazio di layout tramite `env(safe-area-inset-*)` e coordinare barra, microfono, snackbar e contenuto focalizzato.

Introdurre un contratto tipizzato minimale per registrare una barra contestuale di KinService. KinList usera questo contratto solo per azioni effettivamente disponibili.

## 5. Migrare la shell e i comportamenti trasversali

Migrare in questo ordine:

1. `src/frontend/src/components/Layout.tsx`: rimuovere header, menu e controlli legacy.
2. `src/frontend/src/components/PageScaffold.tsx`: preservare il focus anche nei cambi di slug documentale.
3. `src/frontend/src/components/LanguageSelector.tsx` e `ThemeSelector.tsx`: sostituire controlli nativi legacy con primitive condivise.
4. `src/frontend/src/components/AuthControls.tsx`: adattare login/logout/account alla shell condivisa.
5. `src/frontend/src/components/ProtectedRoute.tsx`: usare componenti di stato e azione condivisi.
6. `src/frontend/src/components/Onboarding.tsx`: migrare al dialog condiviso preservando Escape, focus e restart.
7. `src/frontend/src/components/VersionNotification.tsx`: migrare allo snackbar condiviso.
8. `src/frontend/src/components/ErrorBoundary.tsx`: rendere il fallback coerente con il design system anche quando la shell principale fallisce.

## 6. Migrare le route correnti

Migrare le pagine in ordine di rischio:

1. `/kinlist`
2. `/settings`
3. `/`
4. `/release-notes`
5. `/about`
6. `/docs/:slug`
7. `404` ed error boundary

Vincoli specifici per `/kinlist`:

- preservare bootstrap, offline, accesso negato, errore tecnico, loading, retry e sessione scaduta;
- preservare cancellazione richieste, lock del submit e protezione dai risultati stale;
- preservare il nome famiglia dopo errori recuperabili;
- mantenere `familyId` e dati sensibili solo in memoria;
- usare `StatePanel`, field e button condivisi senza perdere `role`, `aria-live`, `aria-busy` o focus significativo.

Ogni pagina continua a usare `PageScaffold`, help localizzato, route registry e stati asincroni condivisi.

## 7. Eliminare UI legacy e superfici duplicate

Dopo la migrazione completa dei consumer:

- rimuovere `/design-system` da `src/frontend/src/App.tsx` e `src/frontend/src/routes/route-registry.json`;
- rimuovere `src/frontend/src/pages/DesignSystemPage.tsx`;
- rimuovere help, traduzioni e guide dedicate alla route demo se non riusate altrove;
- rimuovere le classi `.ds-*`;
- rimuovere `.button`, `.state-card`, `.settings-card`, `.feature-card`, `.card-grid`, `.control` e altri wrapper legacy equivalenti;
- rimuovere `.inline-form` e `.project-list` se risultano inutilizzate;
- limitare o rimuovere selettori globali generici come `nav a` e `input` che interferiscono con le primitive;
- rimuovere la guida legacy `projects` e rigenerare l'indice documentale;
- verificare repository-wide che non restino componenti doppi, classi obsolete o stringhe visibili hardcoded.

## 8. Allineare tema e PWA ai token finali

Allineare la palette finale e il tema risolto tra:

- `src/frontend/index.html`
- `src/frontend/vite.config.ts`
- `src/frontend/public/icon.svg`
- `src/frontend/src/components/ThemeProvider.tsx`
- `src/frontend/src/styles.css`

Il risultato deve evitare flash, residui della palette precedente e incoerenze tra `theme-color`, manifest, icona e CSS finale.

## 9. Rendere il riuso del design system vincolante

Introdurre un validator dedicato, ad esempio `src/frontend/scripts/validate-design-system.mjs`, che impedisca almeno:

- classi legacy vietate nelle pagine e nei componenti migrati;
- reintroduzione della route demo `/design-system`;
- uso di controlli raw nelle superfici dove esiste gia una primitive condivisa;
- import di librerie UI parallele o wrapper equivalenti fuori dai touchpoint approvati.

Registrare il validator in `src/frontend/package.json` e richiamarlo in:

- `.github/workflows/pr-quality.yml`
- `.github/workflows/deploy-code.yml`
- `.github/workflows/deploy-infrastructure.yml`

## 10. Documentazione, skill e contratti di riuso

Aggiornare:

- `skills/frontend/SKILL.md`
- `skills/frontend/catalog.json`
- `skills/frontend/examples/`
- `skills/implementation/SKILL.md`
- `AGENTS.md`
- `skills/registry.json` rigenerato dall'harness
- una guida tecnica autorevole, preferibilmente `docs/architecture/frontend-design-system.md`
- guide utente italiane e inglesi che descrivono shell, navigazione o stati migrati
- change fragment bilingue relativo alla feature

La documentazione deve esplicitare:

- primitive ufficiali e pattern promossi;
- regola primitive prima, wrapper specifici solo se riducono davvero la duplicazione;
- requisiti di accessibilita, tema, safe area e localizzazione;
- divieto di UI parallele, route demo prodotto e magic strings fuori da i18n.

## 11. Test e copertura dei criteri di accettazione

Aggiungere o completare test frontend per:

- primitive condivise;
- `FloatingBars` e shell;
- composizione di `KinPatterns`;
- routing e rendering delle route correnti;
- `KinListAccessGate` con gli stati FEAT-001 e FEAT-002 gia presenti;
- `ThemeProvider`, tutorial, snackbar ed error boundary;
- accessibilita essenziale, focus, tastiera e reduced motion.

Mappatura minima criteri/evidenze:

- AC-078: route correnti migrate e prive di markup legacy attivo.
- AC-079: floating navigation globale e contestuale integrata nella shell senza reimplementazioni.
- AC-080: validator, grep repository-wide e rimozione di route/classi/componenti duplicati.
- AC-081: wrapper costruiti sopra primitive condivise con test di composizione.
- AC-082: skill, catalogo, AGENTS e CI aggiornati per imporre il riuso.
- AC-083: temi, focus, help, responsive, reduced motion e stati asincroni preservati.

## 12. Verifica finale

Eseguire almeno:

```text
npm ci --prefix src/frontend
npm run --prefix src/frontend test
npm run --prefix src/frontend lint
npm run --prefix src/frontend typecheck
npm run --prefix src/frontend i18n:validate
npm run --prefix src/frontend routes:validate
npm run --prefix src/frontend design-system:validate
npm run --prefix src/frontend build

npm run docs:sync
npm run docs:validate
npm run release:generate
npm run release:validate
npm run skills:build
npm run skills:validate
```

Completare verifiche manuali su desktop, mobile e PWA installata per:

1. navigazione tra tutte le route con link, URL diretto, refresh e cronologia browser;
2. italiano e inglese;
3. light, dark e system;
4. viewport strette, zoom 200% e safe area;
5. tastiera, focus visibile e screen reader;
6. reduced motion;
7. login, logout, sessione scaduta, accesso negato, offline e riconnessione;
8. loading, empty, error e retry;
9. carosello tramite swipe, frecce e indicatori;
10. tutorial, dialog, drawer e snackbar senza sovrapposizioni o perdita di focus.

## Sequenza di consegna

1. Stabilizzare primitive, token e test base.
2. Rifattorizzare `KinPatterns`.
3. Integrare floating navigation e contratto contestuale nella shell.
4. Migrare shell e comportamenti trasversali.
5. Migrare le route correnti, con priorita a `/kinlist`.
6. Eliminare route demo, CSS legacy e duplicazioni residue.
7. Aggiornare validator, workflow, skill, catalogo, guide e change fragment.
8. Eseguire test, build e validatori fino al verde.
9. Portare FEAT-014 da `In progress` a `In review`, senza segnarla autonomamente `Completed`.
10. Controllare diff e stato Git; creare commit su `dev`, push e pull request verso `main`; monitorare le GitHub Actions fino a `success` per l'ultimo SHA senza eseguire merge.
