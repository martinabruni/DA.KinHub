# Piano operativo per Claude — Kin List, Identity broker e rimozione MCP

Ultimo aggiornamento: 30 giugno 2026.

Questo piano sostituisce la versione precedente orientata a Codex multi-agent. Da qui in avanti il documento deve essere eseguibile da Claude come implementatore principale, usando branch e worktree dedicati per task e una verifica finale esplicita `PASS` o `FAIL` basata su prove automatizzate.

## Obiettivo

Portare a completamento il programma Kin List + autenticazione, includendo:

- broker OAuth/OIDC comune;
- autorizzazione familiare centralizzata;
- bounded context Kin List autonomo;
- pipeline audio Speech + OpenAI;
- nuova SPA Kin List;
- estrazione completa da KinRecipe;
- rimozione completa MCP;
- migrazione dati, IaC, pipeline e verifica end-to-end finale.

## Modalita di esecuzione per Claude

- Leggere prima lo stato reale del repository e il `git status`; non assumere che la working tree sia pulita.
- Non sovrascrivere o revertare modifiche locali non proprie.
- Creare un branch di integrazione dedicato, ad esempio `claude/kin-list` oppure altro branch concordato, e far partire tutti i task da li.
- Usare un worktree separato per ogni task, in una struttura equivalente a:

```text
../Kin.KinHub-worktrees/
  t01-auth/
  t02-kinlist-backend/
  t03-kinlist-frontend/
  t04-cleanup/
  t05-infrastructure/
  t06-integration/
```

- Ogni worktree deve usare un branch task dedicato, ad esempio `claude/kin-list-t01-auth`, `claude/kin-list-t02-backend`, e cosi via.
- Ogni task parte dall'ultimo commit approvato del branch di integrazione.
- Se Claude esegue task in parallelo, non devono avanzare in parallelo task che modificano gli stessi file condivisi o contratti ancora instabili.
- Congelare i contratti API prima di far avanzare il frontend in modo definitivo.
- Segnare una checkbox come completata solo con evidenza verificabile: test automatizzati verdi, build verde, scansione verde o prova documentata nel codice.
- Se un punto e parzialmente coperto, lasciarlo non spuntato e annotare il perimetro gia coperto.
- Ogni task deve chiudersi con un mini-verdetto locale nel proprio worktree: `PASS` solo se requisiti, test pertinenti e assenza di regressioni sono confermati; altrimenti `FAIL` con gap espliciti.
- Dopo `PASS` del task, creare un commit atomico e integrare nel branch di integrazione con merge non fast-forward oppure con la strategia di merge concordata, mantenendo comunque tracciabile il perimetro del task.
- Prima della chiusura finale, rieseguire i gate trasversali e il pacchetto T07 completo.

## Stato attuale verificato

Le seguenti evidenze risultano gia ottenute:

- `dotnet test Kin.KinHub.Core.slnx -c Release --no-restore` verde.
- `npm run lint` verde per Kin List React.
- `npm run verify:auth-client` verde per Kin List React.
- `npm test` verde per Kin List React con 5 test iniziali.
- `npm run build` verde per Kin List React, con solo warning di chunk size.
- Fix lint/react gia applicati in `AudioCaptureDialog` e `KinListDetailPage`.
- Esiste una suite test iniziale SPA per `AudioCaptureDialog` e `draftSessionStore`.

Queste evidenze non chiudono i task completi: coprono solo una parte dei gate trasversali e di T04.

## Sequenza obbligatoria

Ordine di esecuzione:

1. `T01` prima di tutti gli altri.
2. `T02` dopo il merge di `T01` nel branch di integrazione.
3. `T03` e `T04` dopo il congelamento dei contratti di `T02`; possono usare worktree separati.
4. `T05` dopo il merge di `T02` nel branch di integrazione.
5. `T06` dopo `T05` e dopo la disponibilita degli output di `T02`.
6. `T07` in un worktree di integrazione finale, solo quando `T01`-`T06` sono chiusi e mergiati.

## Checklist integrata

## Gate trasversali

- [x] Rendere verde `dotnet test Kin.KinHub.Core.slnx -c Release --no-restore`.
  Evidenza: eseguito con esito verde il 30 giugno 2026.
- [x] Rendere verde `npm run lint` per Kin List React.
  Evidenza: lint verde il 30 giugno 2026.
- [x] Correggere e rendere verde `npm run verify:auth-client`.
  Evidenza: script corretto e verifica verde il 30 giugno 2026.
- [x] Introdurre una suite automatizzata iniziale per la SPA Kin List.
  Evidenza: `npm test` verde con 5 test iniziali su dialog audio e draft session store.
- [ ] Eseguire e documentare l'intero pacchetto finale richiesto da T07: build/test Release da checkout pulito, build di tutte le SPA, test PostgreSQL reale, migrazione da schema precedente, Bicep build, build immagini e scansioni finali.

## T01 — Identity broker e authorization familiare

Obiettivo:
realizzare il broker OAuth/OIDC comune e una authorization familiare centralizzata, senza usare il JWT come fonte autoritativa del `familyId`.

Checklist:

- [ ] Introdurre una vera interfaccia adapter per gli identity provider e implementare KinHub/password tramite tale adapter.
- [ ] Modellare `UserProvider` con linking/unlinking esplicito: niente linking automatico per email e divieto di scollegare l'ultimo provider.
- [ ] Implementare Authorization Code + PKCE con client/redirect validati, codice monouso a breve scadenza e protezioni anti-replay.
- [ ] Usare sessione Identity in cookie `HttpOnly` + `Secure`, con policy `SameSite` compatibile con redirect top-level.
- [ ] Tenere l'access token solo in memoria nelle SPA; nessun token in URL, `localStorage` o refresh token lato SPA.
- [ ] Allineare il rinnovo SPA al piano: nuovo authorize silenzioso/top-level basato sulla sessione Identity.
- [ ] Migrare Core, Identity, KinRecipe e KinList a un client OAuth comune, eliminando relay e duplicazioni.
- [ ] Aggiungere in Core `GET /api/access/family-context` come fonte autoritativa del contesto famiglia.
- [ ] Creare middleware/authorization handler condiviso request-scoped con fail closed a `503` se Core non e raggiungibile.
- [ ] Impedire che payload o route applicative impongano il `familyId`.
- [ ] Applicare la policy solo agli endpoint familiari; onboarding/login/registrazione/OAuth esclusi.
- [ ] Uniformare gli errori applicativi a RFC 9457 `ProblemDetails` con `code`, `correlationId` ed errori di campo, salvo i punti in cui il protocollo OAuth impone altro formato.
- [ ] Chiudere i gate di test: registrazione senza famiglia, creazione famiglia senza riemissione token, cambio/uscita con revoca immediata, Core indisponibile, PKCE invalido, code replay, redirect non autorizzato e linking provider.

## T02 — Bounded context e API Kin List

Obiettivo:
separare definitivamente Kin List da KinRecipe/Core e completare l'API con concorrenza, retry e idempotenza verificati.

Checklist:

- [ ] Creare i progetti separati Kin List: Domain, Business, PostgreSql, Speech/OpenAI e ASP.NET API Container App.
- [ ] Spostare liste e item fuori da `RecipeFeature`; Kin List deve essere l'unico proprietario.
- [ ] Usare PostgreSQL con schema dedicato `kinlist`.
- [ ] Completare il modello lista/item/idempotency record secondo il piano.
- [ ] Validare all'avvio tutti i limiti configurabili: audio 60 s/10 MB, titolo 100, item 200, 100 item/lista, 50 item/registrazione, MIME ammessi, timeout/retry/cleanup.
- [ ] Implementare ordinamento, soft-delete, restore e comportamento condiviso per famiglia come da requisiti.
- [ ] Imporre `If-Match` su ogni mutazione con `409 etag_conflict` su mismatch e senza retry.
- [ ] Garantire che le mutazioni item aggiornino anche versione e timestamp della lista e che item differenti restino aggiornabili indipendentemente.
- [ ] Implementare retry massimo 3 con exponential backoff + jitter solo per errori transienti ammessi.
- [ ] Implementare creazione lista idempotente con `Idempotency-Key`, hash payload, retention 24 ore e cleanup configurabile.
- [ ] Verificare con integration test tutti gli endpoint principali: liste, dettaglio, patch, delete, restore, CRUD item, bulk confirm e isolamento familiare.
- [ ] Aggiungere test con PostgreSQL reale per transazioni, concorrenza, ordering, ETag e idempotenza.
- [ ] Rimuovere `ShoppingListEntity` e `ShoppingListItemEntity` dal `CoreDbContext` e dal relativo snapshot quando il contract e pronto.

## T03 — Pipeline audio e prompt

Obiettivo:
trascrivere audio senza persistenza, strutturare l'output con `gpt-4o-mini` e rendere il comportamento deterministico nei test.

Checklist:

- [ ] Provisionare adapter Azure AI Speech con rilevamento lingua automatico e trascrizione solo temporanea.
- [ ] Garantire che audio, trascrizione, titolo e item non vengano mai loggati.
- [ ] Passare la trascrizione a `gpt-4o-mini` con structured output validato.
- [ ] Versionare il prompt nel repository.
- [ ] Verificare con test il comportamento del prompt: lingua preservata, quantita/unita concatenate nel testo, deduplica solo di testi identici dopo normalizzazione, quantita differenti mantenute separate.
- [ ] Restituire per nuova lista `title`, `items`, `detectedLanguage`, `promptVersion`.
- [ ] Per lista esistente, restituire proposte + duplicati esistenti con duplicati deselezionati di default.
- [ ] Restituire `422 no_items_detected` quando non emergono item.
- [ ] Limitare la telemetria a byte, durata, lingua, latenze, esito, conteggio item, prompt version e correlation ID.
- [ ] Aggiungere test deterministici separati per adapter Speech/OpenAI con fake o mock; nessun test deve chiamare Azure.

## T04 — Static Web App Kin List

Obiettivo:
consegnare una SPA Kin List autonoma, mobile-first, integrata col nuovo auth flow e priva di residui legacy.

Checklist:

- [ ] Creare la Static Web App React mobile-first riusando design system e card esistenti.
- [ ] Implementare landing senza liste con grande pulsante microfono centrato e azione manuale sempre disponibile.
- [ ] Implementare landing con liste: griglia card, progress bar, menu, completed in fondo e microfono flottante.
- [ ] Implementare registrazione con `MediaRecorder`, contatore e stop automatico a 60 secondi, coprendo Safari iOS, Chrome Android e desktop correnti.
- [ ] Gestire permesso negato o browser non supportato con istruzioni e creazione manuale, senza upload alternativi.
- [ ] Mantenere il blob solo in memoria per `Riprova` e poi eliminarlo.
- [ ] Usare la stessa pagina dettaglio per creazione manuale e audio.
- [ ] Evitare qualsiasi persistenza della bozza prima di `Salva`.
- [ ] Mostrare conferma uscita se la bozza e modificata.
- [ ] Usare creazione idempotente.
- [ ] Nel dettaglio persistito: checklist, CRUD item, completed in fondo, undo server-side 5 secondi, audio -> anteprima nello stesso dettaglio, gestione conflitto ETag con avviso e reload.
- [ ] Integrare Authorization Code + PKCE comune, senza `activeMember`.
- [ ] Eliminare dalla SPA il codice copiato non pertinente e i residui legacy non piu usati.
- [ ] Sostituire le chiamate legacy `/api/shopping-lists` con `/api/lists` o rimuovere il codice morto.
- [x] Correggere gli errori lint specifici di `AudioCaptureDialog` e `KinListDetailPage`.
  Evidenza: fix gia applicati e lint verde.
- [x] Introdurre test iniziali SPA per dialog audio e draft session store.
  Evidenza: `AudioCaptureDialog.test.tsx` e `draftSessionStore.test.ts` presenti, `npm test` verde con 5 test.
- [ ] Estendere i test componenti/flussi con API mock per empty/list landing, creazione manuale/audio, draft condiviso, dirty navigation, retry blob, ordering, undo 5 secondi, conflitto ETag e responsive layout.
- [ ] Estendere i test MediaRecorder per permesso negato, API non supportata, stop automatico 60 secondi, MIME/browser supportati, retry in memoria e rilascio del blob.
- [ ] Verificare che non esistano realtime, offline o PWA.

## T05 — Estrazione da KinRecipe, catalogo Core e rimozione MCP

Obiettivo:
chiudere il passaggio di proprieta a Kin List e rimuovere definitivamente MCP e le vecchie superfici shopping list.

Checklist:

- [ ] Registrare Kin List nel catalogo servizi Core.
- [ ] Abilitare Kin List automaticamente per tutte le famiglie esistenti e nuove.
- [ ] Aggiungere l'URL Kin List al service launcher Core.
- [ ] Rimuovere completamente shopping list da KinRecipe/Core: dominio, business, repository, controller, validator, UI e accessi dati.
- [ ] Trasformare le vecchie pagine KinRecipe in redirect verso Kin List preservando l'ID.
- [ ] Rimuovere le vecchie API shopping-list; niente endpoint di compatibilita.
- [ ] Rimuovere MCP da endpoint, transport, handler, tool, package, configurazioni e test.
- [ ] Eseguire una scansione repository/CI che garantisca l'assenza di riferimenti MCP produttivi.
- [ ] Verificare con test launcher/catalogo, assegnazione alle famiglie e redirect lista/dettaglio.

Nota:
ignorare via ESLint moduli legacy copiati non equivale a chiudere questo task. La rimozione reale del codice legacy resta aperta.

## T06 — Migrazione dati, IaC e pipeline

Obiettivo:
portare in produzione Kin List con migrazione expand/contract, risorse Azure dedicate e pipeline complete.

Checklist:

- [ ] Implementare la migration expand/contract: spostamento transazionale da `kinrecipe` a `kinlist`.
- [ ] Creare view PostgreSQL aggiornabili con i vecchi nomi nello schema `kinrecipe`.
- [ ] Pianificare la rimozione delle view solo nel rilascio successivo.
- [ ] Creare migration history separata per `KinListDbContext`.
- [ ] Aggiungere un Container Apps Job di migration eseguito una volta prima dell'attivazione della revisione.
- [ ] Verificare assenza di `Database.Migrate()` nelle repliche applicative.
- [ ] Estendere `main.bicep` con Static Web App Kin List, Container App Kin List, Azure AI Speech, migration job, managed identities, Key Vault references, RBAC, CORS, modello e tutti i parametri operativi richiesti.
- [ ] Spostare i segreti di database, Speech, OpenAI e registry a Key Vault references, senza valori inline nelle Container App.
- [ ] Aggiornare la pipeline backend per build/test nuovi progetti, immagini Kin List/migration, Bicep build/what-if, migration job pre-rollout e deploy dev -> prod.
- [ ] Aggiornare la pipeline frontend con matrice Kin List, variabili URL OAuth/API e deploy Static Web App dev/prod.
- [ ] Aggiornare solution e parametri deployment; `main.json` va rigenerato da Bicep e non editato a mano.
- [ ] Aggiungere dry-run migration su database temporaneo con dati campione, verifica rollback e verifica compatibilita tramite view.
- [ ] Verificare che i secret non compaiano nelle revisioni Container App.

## T07 — Integrazione end-to-end e review finale

Obiettivo:
rieseguire tutto il sistema integrato e chiudere il programma solo con un verdetto finale motivato.

Checklist:

- [ ] Risolvere eventuali conflitti solo sul branch di integrazione finale.
- [ ] Eseguire restore/build/test .NET Release completi.
- [ ] Eseguire build di tutte le SPA.
- [ ] Eseguire test migration da schema precedente con dati campione.
- [ ] Eseguire test auth PKCE e `family-context`.
- [ ] Eseguire test API Kin List con PostgreSQL reale di test.
- [ ] Eseguire test audio con mock Speech/OpenAI.
- [ ] Eseguire `bicep build`.
- [ ] Eseguire scansioni finali per assenza di MCP, token relay, segreti e logging di contenuti utente.
- [ ] Automatizzare o verificare integralmente lo scenario end-to-end:
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
- [ ] Emettere un verdetto finale `PASS` o `FAIL` sull'intero diff.

Regola di chiusura:
`PASS` finale solo se tutti i punti sopra sono chiusi con prove. Qualsiasi gap residuo mantiene il programma in `FAIL`.

## Assunzioni bloccate

- `Function` significa nuova API ASP.NET in Azure Container Apps, non Azure Functions.
- Kin List e l'unico proprietario delle liste.
- Le liste sono condivise per famiglia con stessi permessi per tutti i membri.
- Il rilascio e diretto a tutte le famiglie, senza feature flag.
- Nessun realtime, streaming audio, storage audio, offline o PWA.
- Azure Speech trascrive; `gpt-4o-mini` struttura i dati.
- Nessun test usa Azure Speech reale.
- Identity locale e il primo provider del broker; Google/GitHub/Entra non si implementano ora ma devono essere supportabili via adapter.
- In caso di indisponibilita di Core, prevale la revoca immediata: fail closed con `503`.
- Le view di compatibilita restano per un ciclo di rilascio.
