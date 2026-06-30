# Checklist di completamento — Kin List e autenticazione

Verdetto judge: **FAIL** (30 giugno 2026).

Il piano non può essere considerato completato finché tutti i punti seguenti non sono chiusi e il judge finale non restituisce `PASS`.

## Gate trasversali

- [ ] Rendere verde `dotnet test Kin.KinHub.Core.slnx -c Release` da checkout pulito. Attualmente 9 test in `OAuthAndAccessIntegrationTests` falliscono perché l'host di test non configura `PostgreSqlOptions.ConnectionString`.
- [ ] Rendere verde `npm run lint` per Kin List. Attualmente risultano 15 errori e 8 warning, inclusi errori nei nuovi componenti Kin List.
- [ ] Correggere e rendere verde `npm run verify:auth-client` per Kin List; lo script è ancora copiato da KinRecipe e cerca marker non presenti.
- [ ] Aggiungere una suite automatizzata per la SPA Kin List: oggi `package.json` non espone uno script di test e non esistono file `*.test.*`/`*.spec.*`.
- [ ] Eseguire e documentare build/test Release, build di tutte le SPA, test PostgreSQL reale, migrazione da schema precedente, Bicep build, build immagini e scansioni di sicurezza richieste da T07.

## T01 — Identity broker e authorization familiare

- [ ] Introdurre una vera interfaccia adapter per gli identity provider e implementare KinHub/password tramite tale adapter; l'attuale login usa direttamente credenziali/repository e non dimostra l'estensibilità Google/GitHub/Entra richiesta.
- [ ] Implementare API e regole di linking/unlinking dei `UserProvider`: niente linking automatico per email e divieto di scollegare l'ultimo provider. Al momento esiste la creazione del provider locale in registrazione, ma non i flussi di link/unlink.
- [ ] Allineare il rinnovo SPA al piano: niente refresh token nella SPA; rinnovo tramite nuovo authorize basato sulla sessione Identity. L'OAuth server espone ancora il grant `refresh_token`.
- [ ] Dimostrare che Core, Identity, KinRecipe e Kin List usano un unico client OAuth comune, senza relay o implementazioni copiate, e correggere la relativa verifica automatica.
- [ ] Applicare l'autorizzazione familiare tramite policy/handler condiviso request-scoped, non tramite controlli ripetuti dentro ogni action del controller Kin List.
- [ ] Verificare che il JWT non sia mai fonte autoritativa del `familyId` e che payload/route non possano imporlo, su tutti gli endpoint familiari.
- [ ] Completare i gate mancanti: registrazione senza famiglia, creazione famiglia senza riemissione token, cambio/uscita con revoca immediata, Core indisponibile/fail-closed, code replay, redirect non autorizzato e linking provider.
- [ ] Rendere uniformi tutti gli errori applicativi a RFC 9457 `ProblemDetails` con `code`, `correlationId` ed errori di campo; le risposte OAuth possono mantenere il formato OAuth solo dove richiesto dal protocollo.

## T02 — Bounded context e API Kin List

- [ ] Rimuovere completamente liste e item da Core/RecipeFeature: dominio, business, repository, mapping EF, controller/validator condivisi e registrazioni DI sono tuttora presenti.
- [ ] Rimuovere `ShoppingListEntity` e `ShoppingListItemEntity` dal `CoreDbContext` e dal relativo model snapshot dopo la migrazione contract.
- [ ] Verificare l'intero contratto API con integration test, inclusi tutti gli endpoint lista/item, bulk confirm, restore, soft-delete e isolamento familiare.
- [ ] Aggiungere test con PostgreSQL reale per transazioni, concorrenza, ordinamento e idempotenza; i test correnti sono prevalentemente unitari/in-memory.
- [ ] Provare con test che ogni mutazione richiede `If-Match`, che il mismatch restituisce `409 etag_conflict` senza retry, che una mutazione item aggiorna lista/versione e che item differenti restano aggiornabili indipendentemente.
- [ ] Provare retry massimo 3 con exponential backoff+jitter solo per errori transienti ammessi e retry dell'intera transazione per deadlock/serialization; provare esplicitamente l'assenza di retry per validation, authorization ed ETag.
- [ ] Provare atomicità e concorrenza della creazione idempotente: stessa chiave/hash restituisce il risultato, payload diverso dà `409`, lista+item sono atomici, retention 24 ore e cleanup configurabile.
- [ ] Validare con test tutti i limiti configurabili all'avvio: audio 60 s/10 MB, titolo 100, item 200, 100 item/lista, 50 item/registrazione, MIME WebM/MP4/M4A/OGG, timeout/retry/cleanup.

## T03 — Pipeline audio e prompt

- [ ] Aggiungere test deterministici separati degli adapter Speech e OpenAI (fake/mock), inclusi lingua automatica, structured output invalido, timeout/errori transienti e garanzia che nessun test chiami Azure.
- [ ] Aggiungere test completi del prompt versionato: lingua preservata, quantità/unità concatenate, deduplica solo dopo normalizzazione, quantità differenti non deduplicate e richiesta esplicita/elenco diretto.
- [ ] Aggiungere test per risposta nuova lista, duplicati su lista esistente deselezionati di default e `422 no_items_detected`.
- [ ] Implementare/verificare la telemetria consentita (byte, durata, lingua, latenze, esito, conteggio item, prompt version, correlation ID) e una prova automatica che audio, trascrizione, titolo e item non vengano mai loggati.

## T04 — Static Web App Kin List

- [ ] Eliminare dalla nuova SPA il codice copiato non pertinente (shopping-list legacy, ricette, frigoriferi, assistant e pagine/provider Core/Identity), lasciando Kin List come superficie autonoma.
- [ ] Sostituire ogni chiamata legacy `/api/shopping-lists` con il contratto `/api/lists` oppure rimuovere il relativo codice morto.
- [ ] Aggiungere test componenti/flussi con API mock per empty/list landing, creazione manuale/audio, draft condiviso, dirty navigation, retry blob, ordering, undo 5 secondi, ETag conflict e responsive layout.
- [ ] Aggiungere test MediaRecorder per permesso negato, API non supportata, stop automatico a 60 secondi, MIME/browser supportati, retry in memoria e rilascio del blob.
- [ ] Verificare che non esistano upload alternativi, persistenza draft prima di Salva, realtime, offline o PWA.
- [ ] Correggere gli errori lint specifici di `AudioCaptureDialog` e `KinListDetailPage` e gli altri errori/warning del progetto.

## T05 — Catalogo, estrazione KinRecipe e rimozione MCP

- [ ] Registrare Kin List nel catalogo persistito e abilitarlo automaticamente per tutte le famiglie esistenti e nuove; l'enum e i link frontend da soli non dimostrano assegnazione/backfill.
- [ ] Rimuovere completamente shopping list da KinRecipe/Core: modelli, servizi, repository, controller, validator, UI e accesso alle tabelle sono ancora presenti.
- [ ] Trasformare le vecchie pagine KinRecipe in soli redirect verso Kin List, preservando l'ID nel dettaglio.
- [ ] Rimuovere le vecchie API shopping-list; non devono restare endpoint di compatibilità.
- [ ] Aggiungere test per launcher/catalogo, assegnazione a famiglie esistenti/nuove e redirect lista/dettaglio.
- [ ] Mantenere verde una scansione CI che garantisca assenza di riferimenti MCP produttivi, package, endpoint, transport, handler, tool, configurazioni e test. La scansione locale attuale non trova riferimenti MCP fuori da documentazione/piani, ma manca il gate automatizzato.

## T06 — Migrazione, IaC e pipeline

- [ ] Sostituire la migration iniziale con l'expand/contract richiesto: spostamento transazionale di tabelle e dati da `kinrecipe` a `kinlist` e creazione delle view PostgreSQL aggiornabili con i vecchi nomi.
- [ ] Pianificare la rimozione delle view esclusivamente nella migration/release successiva.
- [ ] Aggiungere test dry-run da schema precedente con dati campione, verifica conservazione dati/stati, scrittura tramite view compatibili e rollback.
- [ ] Creare un'immagine/job Container Apps dedicato alle migration, eseguito una volta prima del rollout; verificare l'assenza di `Database.Migrate()` nelle replica applicative.
- [ ] Estendere `main.bicep` con Kin List Static Web App, Kin List Container App, Azure AI Speech, migration job, managed identities, RBAC, CORS e tutti i parametri di limiti/timeout/retry/retention/delay. Attualmente tali risorse non esistono.
- [ ] Aggiungere immagini e configurazioni Kin List/migration con Key Vault references per database, Speech, OpenAI e registry, senza segreti inline nelle revisioni.
- [ ] Eliminare i segreti passati come parametri/valori inline dove il piano richiede Key Vault references e aggiungere un controllo sulle revisioni Container App.
- [ ] Aggiornare la pipeline backend per build/test Kin List, immagini API/migration, Bicep build/what-if, migration job pre-rollout e deploy dev→prod.
- [ ] Aggiornare la pipeline frontend con Kin List nella matrice, variabili OAuth/API e deploy Static Web App dev→prod.
- [ ] Aggiornare solution e parametri di deployment e rigenerare `main.json` esclusivamente da Bicep.

## T07 — Integrazione e judge finale

- [ ] Automatizzare lo scenario end-to-end completo in 10 passi definito dal piano, inclusi audio con mock, concorrenza `409`, revoca famiglia immediata e redirect KinRecipe.
- [ ] Eseguire scansioni finali per assenza di MCP, token relay, segreti e logging di contenuti utente.
- [ ] Eseguire un nuovo judge indipendente su requisiti, diff, test e file finali; correggere ogni `FAIL` e ripetere tutti i gate fino a `PASS`.
