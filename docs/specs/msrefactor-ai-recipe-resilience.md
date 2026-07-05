> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Rendere **resilienti e osservabili** le integrazioni con Azure OpenAI usate dall'assistente ricette (parse/suggest/adapt) e dal calcolo degli ingredienti mancanti. Oggi queste chiamate non hanno timeout, retry o gestione degli errori, e assumono che il modello restituisca sempre un JSON valido (`Deserialize<…>!`). L'obiettivo è portarle allo stesso standard già presente nella pipeline audio di KinList (timeout + retry sui transitori + mappatura esplicita dei fallimenti), così che un disservizio dell'AI produca un errore **controllato** (es. 503) invece di un 500 non gestito, con log/telemetria adeguati.

Problema risolto: **fragilità verso un'integrazione esterna instabile** e assenza di diagnosticabilità.

# Stato attuale

La logica AI del ricettario è divisa tra Business e Infrastructure:

- **Business** — `KinHubRecipeAssistantManager` (`src/Businesses/Kin.KinHub.Core.Business/RecipeAssistantFeature/Services/KinHubRecipeAssistantManager.cs`) orchestra i tre casi d'uso:
  - `SuggestRecipesAsync`: risolve famiglia/frigorifero, itera su libri e ricette caricando gli ingredienti per ciascuna ricetta con chiamate separate (`GetAllByFamilyIdAsync`), calcola match/mancanti, poi chiama `_recipeAssistantService.SuggestNewRecipesAsync(...)`;
  - `ParseRecipeAsync`: delega a `_recipeAssistantService.ParseRecipeAsync(rawText)`;
  - `AdaptRecipeAsync`: carica la ricetta completa (`BuildAiRecipeAsync`) e chiama `_recipeAssistantService.AdaptRecipeAsync(...)`.
  - Le conversioni finali usano Mapster (`Adapt<…>`). Non c'è try/catch attorno alle chiamate AI.

- **Infrastructure** — `OpenAiRecipeAssistantService` (`src/Infrastructures/Kin.KinHub.Core.OpenAi/RecipeAssistantFeature/Services/OpenAiRecipeAssistantService.cs`):
  - costruisce input JSON con `task_type`, invia una chat completion con `SendAsync` (`ResponseFormat = JsonObject`, temperature 0.7/0.3);
  - deserializza la risposta con `JsonSerializer.Deserialize<SuggestionResponse>(json, JsonOptions)!` (e analoghi per parse/adapt), usando l'operatore null-forgiving `!`;
  - non ha timeout, retry, né gestione delle eccezioni degli SDK Azure.
  - `OpenAiEmbeddingService` (`GenerateEmbeddingAsync`) e `OpenAiRecipeMissingIngredientsService` (cosine similarity, soglia 0.85) hanno lo stesso stile senza resilienza; inoltre un ingrediente **senza embedding** viene sempre considerato "mancante".

Riferimento positivo già presente nel repo: `src/Infrastructures/Kin.KinHub.KinList.Ai/Common/TransientExecutionHelper.cs` (retry con backoff) e `AzureSpeechKinListTranscriber.ExecuteWithTimeoutAsync` (timeout → `ServiceUnavailable`), che mostrano il pattern desiderato applicato altrove.

L'host `KinRecipe.Api` registra queste integrazioni via `AddKinHubCoreOpenAiInfrastructure` in `ServiceCollectionExtensions`.

# Problemi individuati

- **Assenza di gestione errori sulle chiamate AI (rischio di regressione + affidabilità)**: un'eccezione degli SDK Azure (rete, throttling 429, timeout) risale non gestita → HTTP 500 generico verso l'utente, senza `ProblemDetails` significativo.
- **`Deserialize<…>!` con null-forgiving (bug potenziale)**: se il modello restituisce un JSON non conforme o vuoto, si ottiene una `NullReferenceException` invece di un errore chiaro; il contratto con l'LLM non è validato.
- **Nessun timeout dedicato (rischio di scalabilità)**: una chiamata lenta occupa il thread/connessione per un tempo indefinito (dipende solo dai default dell'SDK), peggiorando sotto carico.
- **Nessun retry sui transitori (affidabilità)**: i 429/500 transitori di Azure OpenAI non vengono ritentati, a differenza della pipeline audio.
- **N+1 di lettura in `SuggestRecipesAsync` (rischio di scalabilità/performance)**: per ogni libro carica le ricette e per ogni ricetta carica gli ingredienti; il numero di query cresce col ricettario.
- **Ingrediente senza embedding = "mancante" (debito tecnico/funzionale)**: `OpenAiRecipeMissingIngredientsService` segnala come mancante ogni ingrediente privo di embedding, generando possibili falsi positivi.
- **Osservabilità assente (debito tecnico)**: nessun log/metrica su latenza, esito o fallback delle chiamate AI (mentre KinList ha `KinListAudioTelemetry`).

# Come Microsoft farebbe il refactor

Approccio incrementale che riusa i pattern già nel repo e le pratiche standard .NET (resilienza con `Microsoft.Extensions.Http.Resilience`/Polly o l'helper interno esistente; `IHttpClientFactory`), senza overengineering:

1. **Confinare la resilienza nell'Infrastructure**: il Business continua a ricevere un `Result`/eccezione tipizzata; timeout e retry vivono in `Core.OpenAi`, dietro le interfacce di dominio (`IRecipeAssistantService`, `IEmbeddingService`), coerentemente con l'anti-corruption layer già in essere.
2. **Riusare `TransientExecutionHelper` o adottare Polly**: preferire l'approccio già presente in KinList per coerenza; in alternativa `Microsoft.Extensions.Http.Resilience` è lo standard Microsoft per policy di retry/timeout/circuit breaker su client HTTP. La scelta va documentata, non entrambe.
3. **Validare l'output del modello**: sostituire `Deserialize<…>!` con deserializzazione difensiva; se il payload è nullo/non conforme, restituire un esito d'errore controllato (mappato su `ValidationError`/`ServiceUnavailable`) invece di lanciare `NullReferenceException`.
4. **Mappare i fallimenti a stati applicativi**: introdurre nel contesto Core la possibilità di restituire `ServiceUnavailable` per i disservizi AI (dipende dal consolidamento `Result`, vedi [msrefactor-shared-result-error-handling.md](./msrefactor-shared-result-error-handling.md)); nel frattempo mappare tramite gli stati già esistenti.
5. **Aggiungere observability**: `ActivitySource`/log strutturati attorno alle chiamate AI (latenza, esito, retry count), sul modello di `KinListAudioTelemetry`.
6. **Ridurre l'N+1**: pre-caricare ingredienti/ricette con query aggregate.
7. **Gestire l'assenza di embedding** con un fallback esplicito (confronto per nome) invece di marcare sempre "mancante".
8. **Backward compatibility + rollback**: le firme pubbliche restano invariate; i cambiamenti sono interni alle implementazioni e reversibili per commit.

# Piano operativo

**Step 1 — Test di caratterizzazione delle chiamate AI.**
- *Cosa*: test con un `IRecipeAssistantService`/`IEmbeddingService` fake che simuli (a) risposta valida, (b) eccezione transitoria, (c) JSON malformato/nullo.
- *Dove*: `src/Tests/Kin.KinHub.Core.Test` (sul modello di `KinListRetryPolicyTests`/`KinListAudioDraftGeneratorTests`).
- *Perché*: definire il comportamento atteso prima di introdurre resilienza.
- *Impatto/Rischio*: nessuno sul runtime; rischio basso.
- *Test dopo*: suite Core.

**Step 2 — Deserializzazione difensiva in `OpenAiRecipeAssistantService`.**
- *Cosa*: rimuovere gli operatori `!`; se `Deserialize` ritorna null o mancano campi chiave, restituire un errore controllato.
- *Dove*: `OpenAiRecipeAssistantService.cs` (`SuggestNewRecipesAsync`, `ParseRecipeAsync`, `AdaptRecipeAsync`).
- *Perché*: eliminare i `NullReferenceException`.
- *Impatto previsto*: gli output non conformi diventano errori chiari.
- *Rischio dello step*: basso/medio.
- *Test dopo*: caso (c) dello Step 1.

**Step 3 — Timeout + retry sulle chiamate AI.**
- *Cosa*: avvolgere `CompleteChatAsync`/`GenerateEmbeddingAsync` con timeout e retry (helper esistente o Polly/Http.Resilience).
- *Dove*: `Core.OpenAi` (servizi + registrazione in `ServiceCollectionExtensions`).
- *Perché*: resilienza ai transitori.
- *Impatto previsto*: fallimenti transitori assorbiti; disservizi persistenti mappati a `ServiceUnavailable`.
- *Rischio dello step*: medio.
- *Test dopo*: casi (a)/(b) dello Step 1.

**Step 4 — Mappatura errori nel manager + controller.**
- *Cosa*: `KinHubRecipeAssistantManager` intercetta l'esito d'errore dell'AI e ritorna un `Result` con lo stato appropriato; `RecipeAssistantController` lo traduce in `ProblemDetails`.
- *Dove*: `KinHubRecipeAssistantManager.cs`, `RecipeAssistantController.cs`.
- *Perché*: contratto d'errore coerente verso il client.
- *Impatto previsto*: niente più 500 non gestiti dall'AI.
- *Rischio dello step*: medio.
- *Test dopo*: integration test dei tre endpoint con AI fake in errore.

**Step 5 — Observability.**
- *Cosa*: `ActivitySource`/log strutturati (latenza, esito, retry) attorno alle chiamate AI.
- *Dove*: `Core.OpenAi` servizi.
- *Perché*: diagnosticabilità in produzione.
- *Impatto/Rischio*: basso.
- *Test dopo*: verifica manuale/telemetria in staging.

**Step 6 — Ridurre N+1 e gestire embedding assente.**
- *Cosa*: query aggregate in `SuggestRecipesAsync`; fallback per nome quando l'embedding manca in `OpenAiRecipeMissingIngredientsService`.
- *Dove*: `KinHubRecipeAssistantManager.cs`, `OpenAiRecipeMissingIngredientsService.cs`, repository ricette/ingredienti.
- *Perché*: performance e correttezza funzionale.
- *Rischio dello step*: medio (cambia il set di risultati "mancanti"): coprire con test.
- *Test dopo*: test su `missing-ingredients` con/senza embedding.

# Pattern da applicare

- **Retry con backoff (Transient Fault Handling)**.
  - *Problema*: 429/500 transitori di Azure OpenAI. *Dove*: `Core.OpenAi` servizi. *Perché adatto*: standard per servizi cloud; già usato in KinList. *Perché non overengineering*: policy minima (max attempts + backoff), non un framework.
- **Timeout Pattern**.
  - *Problema*: chiamate pendenti indefinitamente. *Dove*: attorno alle chiamate SDK. *Perché adatto*: limita l'occupazione risorse. *Non overengineering*: un `CancellationTokenSource.CancelAfter` o policy di timeout.
- **Anti-Corruption Layer (già presente, da rafforzare)**.
  - *Problema*: isolare il dominio dai dettagli/instabilità dell'LLM. *Dove*: interfacce `IRecipeAssistantService`/`IEmbeddingService`. *Perché adatto*: mantiene il Business ignaro del fornitore. *Non overengineering*: le interfacce esistono già.
- **(Opzionale) Circuit Breaker** solo se la telemetria mostrasse fallimenti a cascata; non introdurlo preventivamente per evitare overengineering.

# Anti-pattern da rimuovere

- **Null-forgiving sulla deserializzazione** (`Deserialize<…>!`): sostituito da deserializzazione validata.
- **Chiamate esterne "nude"** senza timeout/retry/gestione errori: incapsulate in policy di resilienza.
- **Swallowing implicito via eccezione non gestita** (500 generico): sostituito da esiti tipizzati e `ProblemDetails`.
- **N+1 di lettura** nel suggerimento: sostituito da query aggregate.
- **Regola "no embedding = mancante"** come comportamento silenzioso: resa esplicita con fallback.

# Strategia di test

- **Unit test**: `OpenAiRecipeAssistantService` con client fake per verificare deserializzazione difensiva e mappatura degli errori; verifica che il retry sia invocato N volte sui transitori.
- **Integration test**: i tre endpoint `RecipeAssistantController` con AI fake che restituisce successo, transitorio ripetuto, e disservizio persistente → asserire 200 / 200-dopo-retry / 503.
- **Contract test**: fissare lo schema JSON atteso dal modello e verificarne il rispetto (o il fallimento controllato).
- **Regression test**: `missing-ingredients` con e senza embedding.
- **Performance test (leggero)**: verificare che `SuggestRecipesAsync` non generi query proporzionali al numero di ricette dopo lo Step 6.
- **Scenari da coprire *prima* di iniziare**: happy path dei tre casi d'uso e almeno un fallimento AI per ciascuno.

# Rischi del refactor

- **Cambiamento della semantica d'errore**: alcune risposte che oggi sono 500 diventeranno 503/400 — è il comportamento voluto, ma i client vanno informati.
- **Retry che amplifica il carico**: un retry mal configurato può moltiplicare le chiamate a un servizio già in difficoltà — mitigazione: max attempts basso + backoff (come in KinList), no retry su errori non transitori.
- **Modifica dei risultati "mancanti" (Step 6)**: il fallback per nome cambia l'output — mitigazione: test dedicati e rollout dietro verifica.
- **Doppia libreria di resilienza**: introdurre Polly *e* l'helper interno creerebbe incoerenza — mitigazione: sceglierne una sola e documentarla.

# Strategia di rollback

- Ogni step è un commit isolato e reversibile; il **revert** dell'implementazione di `Core.OpenAi` ripristina il comportamento precedente (nessuna migrazione DB coinvolta).
- Le policy di resilienza sono configurabili: in caso di problemi si possono disattivare (max attempts = 1, timeout ampio) senza revert del codice.
- Deploy progressivo su `KinRecipe.Api` con monitoraggio di error rate e latenza AI; rollback dell'immagine precedente se peggiora.

# Checklist finale

- [ ] Test di caratterizzazione (successo / transitorio / JSON malformato) verdi prima delle modifiche.
- [ ] Rimossi tutti i `Deserialize<…>!`; output non conforme → errore controllato.
- [ ] Timeout applicato a tutte le chiamate Azure OpenAI (chat + embedding).
- [ ] Retry sui transitori con backoff (una sola libreria/approccio, documentato).
- [ ] Fallimenti AI mappati a `ProblemDetails` (niente 500 non gestiti).
- [ ] Log/telemetria (latenza, esito, retry) sulle chiamate AI.
- [ ] N+1 di `SuggestRecipesAsync` ridotto con query aggregate.
- [ ] Embedding assente gestito con fallback esplicito e testato.
- [ ] Suite `Kin.KinHub.Core.Test` verde (unit + integration + regression).
- [ ] Verifica in staging del comportamento sotto disservizio AI simulato.
