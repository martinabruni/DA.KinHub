# Piano completo — Kin List, Identity broker e rimozione MCP

## 1. Strategia worktree e controllo qualità

Creare un branch di integrazione `codex/kin-list` e un worktree separato per ogni task:

```text
../Kin.KinHub-worktrees/
  t01-auth/
  t02-kinlist-backend/
  t03-kinlist-frontend/
  t04-cleanup/
  t05-infrastructure/
  t06-integration/
```

Regole operative:

- Ogni task parte dall’ultimo commit approvato del branch di integrazione.
- Ogni worktree usa un branch `codex/kin-list-tXX-*`.
- Massimo due implementation agent contemporanei, riservando uno slot al judge.
- Task che modificano gli stessi file condivisi non vengono eseguiti in parallelo.
- Ogni agente esegue build e test pertinenti prima del giudizio.
- Un agente LLM distinto, senza contesto delle decisioni implementative, esegue il judge leggendo requisiti, diff, test e file finali.
- Il judge restituisce:
  - `PASS` solo se requisiti, architettura, sicurezza, test e assenza di regressioni risultano completi;
  - `FAIL` con elenco numerato di problemi bloccanti e prove mancanti.
- In caso di `FAIL`, il task viene riaperto integralmente: l’implementation agent riesamina requisiti e diff, corregge, riesegue tutti i test e richiede un nuovo judge. Nessun merge finché non arriva `PASS`.
- Il task non viene azzerato con operazioni distruttive: “ricominciare” significa ripetere l’intero ciclo requisiti → implementazione → verifica, preservando le correzioni valide.
- Dopo `PASS`, commit atomico e merge `--no-ff` nel branch di integrazione.
- Il task finale `t06` applica un ulteriore judge end-to-end sull’intero branch, indipendente dai judge dei singoli task.

Rubrica obbligatoria del judge:

1. Tutti i requisiti assegnati sono implementati.
2. Build e test passano dal worktree pulito.
3. API, schema e configurazione coincidono con il piano.
4. Nessun segreto o contenuto utente viene loggato.
5. Authorization familiare non è aggirabile.
6. Retry, ETag e idempotenza non causano sovrascritture o duplicati.
7. Il task non lascia codice MCP o dipendenze obsolete quando pertinente.
8. Sono presenti test per happy path, errori e regressioni.
9. Nessun TODO o implementazione simulata resta nel codice produttivo.

## 2. Sequenza dei task

### T01 — Identity broker e authorization familiare

Worktree: `t01-auth`. Deve completare prima di tutti gli altri.

- Trasformare KinHub Identity in broker OAuth/OIDC:
  - provider locale KinHub/password dietro un’interfaccia provider;
  - account KinHub stabile collegabile a più `UserProvider`;
  - un provider può essere scollegato solo se ne resta almeno uno;
  - nessun linking automatico basato solo sull’email;
  - adapter futuri Google, GitHub ed Entra aggiungibili senza modificare Core, KinRecipe o KinList.
- Implementare Authorization Code + PKCE:
  - client e redirect URI registrati e validati;
  - codice monouso, breve scadenza e singolo utilizzo;
  - sessione Identity in cookie `HttpOnly`, `Secure`, con policy SameSite compatibile con redirect top-level;
  - access token tenuto in memoria dalle SPA;
  - nessun access token o refresh token in URL/localStorage;
  - rinnovo tramite nuovo authorize silenzioso/top-level basato sulla sessione Identity;
  - logout centralizzato.
- Migrare Core, Identity e KinRecipe al client OAuth comune, eliminando il relay corrente.
- Il JWT identifica l’utente ma non è fonte autoritativa del `familyId`.
- Aggiungere in Core `GET /api/access/family-context`:
  - usa il JWT dell’utente;
  - legge l’appartenenza corrente;
  - restituisce `familyId` oppure `403 family_required`;
  - onboarding e creazione famiglia restano accessibili senza famiglia.
- Creare middleware/authorization handler condiviso:
  - Core risolve direttamente dal repository;
  - KinRecipe e successivamente KinList propagano il bearer token a Core;
  - nessuna cache, per garantire revoca immediata;
  - Core non raggiungibile → fail closed con `503`;
  - il `familyId` viene aggiunto solo al principal request-scoped;
  - payload e route applicative non possono imporre un `familyId`.
- Applicare la policy solo agli endpoint familiari; login, registrazione, OAuth e onboarding sono esclusi.
- Standardizzare gli errori con RFC 9457 `ProblemDetails`, includendo `code`, `correlationId` ed errori di campo.

Gate: test di registrazione senza famiglia, creazione famiglia senza riemissione token, cambio/uscita con revoca immediata, Core indisponibile, PKCE invalido, code replay, redirect non autorizzato e linking provider.

### T02 — Bounded context e API Kin List

Worktree: `t02-kinlist-backend`, creato dal branch dopo T01.

- Creare progetti separati Kin List: Domain, Business, PostgreSql, Speech/OpenAI e ASP.NET API Container App.
- Spostare liste e item fuori da `RecipeFeature`; Kin List diventa unico proprietario.
- Usare lo stesso PostgreSQL con schema dedicato `kinlist`.
- Modello:
  - lista: ID, famiglia, titolo, stato soft-delete, timestamp, versione;
  - item: ID, lista, testo completo, stato completato, ordine di attivazione, soft-delete, timestamp, versione;
  - idempotency record: chiave, famiglia/utente, hash payload, risultato e scadenza.
- Limiti configurabili e validati all’avvio:
  - audio 60 secondi e 10 MB;
  - titolo 100 caratteri;
  - item 200 caratteri;
  - 100 item per lista;
  - 50 item per registrazione;
  - MIME consentiti: WebM, MP4/M4A e OGG;
  - timeout, retry e cleanup idempotenza.
- Comportamenti:
  - liste condivise da tutti i membri della famiglia;
  - nuovi item e item deselezionati in cima;
  - item completati oscurati e in fondo;
  - liste attive ordinate per ultima modifica;
  - liste completate grigie e in fondo;
  - deselezionare un item riattiva la lista;
  - cancellazione lista/item soft-delete;
  - endpoint restore per undo entro snackbar client di 5 secondi;
  - ripristinare una lista conserva item e stati precedenti.
- ETag/version:
  - ogni mutazione richiede `If-Match`;
  - mismatch → `409 etag_conflict`, senza retry;
  - modifiche item aggiornano anche versione e timestamp della lista;
  - item differenti possono essere aggiornati indipendentemente.
- Retry:
  - massimo tre tentativi solo per timeout, reset, `429`, `5xx` e codici PostgreSQL transienti;
  - exponential backoff con jitter;
  - deadlock/serialization retry dell’intera transazione;
  - nessun retry su validation, authorization o ETag.
- Creazione lista idempotente:
  - `Idempotency-Key` obbligatoria;
  - lista e item salvati in una transazione;
  - stessa chiave e stesso hash restituiscono il risultato precedente;
  - stessa chiave e payload diverso → `409`;
  - retention 24 ore e cleanup configurabile.

API principali:

- `GET /api/lists`
- `GET /api/lists/{id}`
- `POST /api/lists`
- `PATCH /api/lists/{id}`
- `DELETE /api/lists/{id}`
- `POST /api/lists/{id}/restore`
- CRUD item e bulk confirm sotto `/api/lists/{id}/items`
- `POST /api/list-drafts/from-audio`
- `POST /api/lists/{id}/item-drafts/from-audio`

Gli endpoint audio ricevono `multipart/form-data` e non persistono dati.

### T03 — Pipeline audio e prompt

Incluso nello stesso worktree T02 per evitare contratti divergenti, ma con commit separato e judge dedicato.

- Provisionare adapter Azure AI Speech:
  - rilevamento automatico lingua;
  - trascrizione temporanea;
  - audio mai salvato;
  - nessun audio, trascrizione, titolo o item nei log.
- Passare la trascrizione a `gpt-4o-mini` con structured output validato.
- Prompt versionato e distribuito nel repository:
  - accetta elenco diretto o richiesta esplicita;
  - produce titolo breve e item;
  - preserva lingua parlata;
  - concatena quantità/unità nel testo, ad esempio `2 confezioni di latte`;
  - non separa quantità in campi;
  - deduplica solo testi identici dopo normalizzazione;
  - quantità differenti restano item differenti.
- Nuova lista: risposta `title`, `items`, `detectedLanguage`, `promptVersion`.
- Lista esistente: restituisce proposte e duplicati esistenti; duplicati deselezionati di default.
- Nessun item rilevato → `422 no_items_detected`.
- Telemetria ammessa: byte, durata, lingua, latenze, esito, conteggio item, prompt version e correlation ID.
- Testare Speech e OpenAI solo tramite mock/fake deterministici; nessun test chiama Azure.

### T04 — Static Web App Kin List

Worktree: `t03-kinlist-frontend`, creato dopo il congelamento dei contratti T02; può procedere in parallelo con T03.

- Creare React Static Web App mobile-first riusando design system e card esistenti.
- Landing senza liste:
  - grande pulsante microfono centrato;
  - azione manuale sempre disponibile.
- Landing con liste:
  - griglia con layout delle card Kin Service;
  - card con titolo, completati/totali, progress bar e menu;
  - liste completate grigie e in fondo;
  - piccolo pulsante microfono flottante centro-basso.
- Registrazione:
  - `MediaRecorder`, contatore e stop automatico a 60 secondi;
  - supporto Safari iOS, Chrome Android e browser desktop correnti;
  - permesso negato/non supportato → istruzioni e creazione manuale;
  - nessun upload file alternativo;
  - blob mantenuto solo in memoria per “Riprova” e poi eliminato.
- Bozza:
  - creazione manuale e audio usano la stessa pagina dettaglio;
  - titolo e item modificabili;
  - nessuna persistenza prima di “Salva”;
  - conferma uscita se la bozza è modificata;
  - creazione idempotente.
- Dettaglio persistito:
  - checklist, modifica/creazione/eliminazione item;
  - completati oscurati e in fondo;
  - undo lista/item per 5 secondi tramite restore server-side;
  - audio aggiunge item tramite anteprima nello stesso dettaglio;
  - conflitto ETag mostra avviso e ricarica dati.
- Nessun realtime, offline o PWA.
- Integrare Authorization Code + PKCE comune; nessuna selezione `activeMember`.

Gate: test componenti e flussi con API mock, permessi microfono, retry blob, draft dirty, undo, ordering, ETag conflict e responsive layout.

### T05 — Estrazione da KinRecipe, catalogo Core e rimozione MCP

Worktree: `t04-cleanup`, dopo T02; non parallelo a task che modificano Shared API.

- Registrare Kin List nel catalogo servizi Core e abilitarlo per tutte le famiglie esistenti e nuove.
- Aggiungere URL Kin List al service launcher Core.
- Rimuovere completamente shopping list da KinRecipe:
  - dominio/business/repository/controller/UI non più proprietari;
  - vecchie pagine redirigono a Kin List preservando l’ID.
- Rimuovere MCP dall’intera codebase:
  - endpoint, transport, handler, tool, package, configurazioni e test MCP;
  - nessun proxy o compatibility tool.
- Eliminare dal vecchio `CoreDbContext` il mapping proprietario delle liste dopo l’introduzione del nuovo contesto.
- Mantenere solo i redirect web compatibili; le vecchie API shopping-list non restano supportate.

Gate: ricerca repository senza riferimenti MCP produttivi, build completa, redirect lista e dettaglio, catalogo/assegnazioni famiglia e assenza di accesso KinRecipe alle tabelle Kin List.

### T06 — Migrazione dati, IaC e pipeline

Worktree: `t05-infrastructure`, dopo T02 e T05.

- Migration expand/contract:
  1. spostare transazionalmente tabelle e dati da `kinrecipe` a `kinlist`;
  2. creare view PostgreSQL aggiornabili con i vecchi nomi nello schema `kinrecipe`;
  3. distribuire Kin List;
  4. distribuire redirect/rimozione vecchie API;
  5. rimuovere le view solo nel rilascio successivo.
- Creare migration history separata per `KinListDbContext`.
- Aggiungere Container Apps Job di migration, eseguito una volta prima dell’attivazione della revisione; vietato `Database.Migrate()` nelle replica applicative.
- Estendere `main.bicep`:
  - Kin List Static Web App;
  - Kin List Container App;
  - Azure AI Speech;
  - migration job;
  - managed identities;
  - Key Vault references e ruoli RBAC;
  - origin CORS;
  - nomi deployment/modello;
  - tutti i limiti, timeout, retry, retention e delay definiti sopra.
- Spostare segreti di database, Speech, OpenAI e registry a Key Vault references. Nessun segreto duplicato come valore inline nelle Container App.
- Aggiornare pipeline backend:
  - build/test nuovi progetti;
  - immagini Kin List e migration;
  - Bicep build/what-if;
  - migration job prima del rollout;
  - deploy dev e poi prod.
- Aggiornare pipeline frontend:
  - matrice build con Kin List;
  - variabili URL OAuth/API;
  - deploy Static Web App dev/prod.
- Aggiornare solution e parametri di deployment; `main.json` viene rigenerato da Bicep, non modificato manualmente.

Gate: Bicep build, build immagini, dry-run migration su database temporaneo, verifica rollback con view compatibili e controllo che i secret non compaiano nelle revisioni Container App.

### T07 — Integrazione end-to-end e judge finale

Worktree: `t06-integration`, creato dal branch con tutti i task approvati.

- Risolvere conflitti solo nel branch di integrazione; nessun fix viene fatto direttamente nei branch task già giudicati.
- Eseguire:
  - restore/build/test .NET Release;
  - build di tutte le SPA;
  - test migration da schema precedente con dati campione;
  - test auth PKCE e family-context;
  - test API Kin List con PostgreSQL reale di test;
  - test audio con mock Speech/OpenAI;
  - Bicep build;
  - scansione assenza MCP, token relay, segreti e logging contenuti.
- Scenario end-to-end:
  1. registrazione senza famiglia;
  2. onboarding e creazione famiglia senza nuovo token;
  3. apertura Kin List dal Core;
  4. creazione manuale;
  5. creazione audio con anteprima;
  6. aggiunta audio con duplicati;
  7. checklist, ordering, soft delete e undo;
  8. modifica concorrente con `409`;
  9. cambio/uscita famiglia con revoca immediata;
  10. redirect da vecchia URL KinRecipe.
- Eseguire judge finale indipendente sull’intero diff. Ogni `FAIL` riapre T07 e, se necessario, il task proprietario della regressione; ripetere build, test e judge fino a `PASS`.

## 3. Assunzioni bloccate

- “Function” indica una nuova API ASP.NET in Azure Container Apps, non Azure Functions.
- Kin List è l’unico proprietario delle liste.
- Liste condivise per famiglia; stessi permessi per tutti.
- Rilascio diretto a tutte le famiglie, senza feature flag.
- Nessun realtime, streaming audio, storage audio, offline o PWA.
- Azure Speech esegue trascrizione; `gpt-4o-mini` struttura i dati.
- Nessuna integrazione Azure Speech reale nei test.
- Identity locale è il primo provider del broker; Google/GitHub/Entra non vengono implementati ora, ma l’architettura deve accoglierli tramite adapter.
- L’immediatezza della revoca prevale sulla disponibilità: Core non raggiungibile comporta `503`.
- Le view di compatibilità restano per un ciclo di rilascio.
