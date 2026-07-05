# Descrizione generale

La macro feature **Assistente Ricette AI** aggiunge al ricettario funzionalità basate su intelligenza artificiale generativa (Azure OpenAI). Sfrutta gli stessi dati di dominio della gestione ricette (vedi [flusso-gestione-ricette.md](./flusso-gestione-ricette.md)) ma li combina con modelli di linguaggio e con embedding vettoriali.

Cosa fa (tre funzioni principali + una di supporto):

1. **Suggerimento ricette** (`suggest`) — dato un frigorifero, calcola le ricette esistenti della famiglia meglio realizzabili con gli ingredienti disponibili (con percentuale di match e ingredienti mancanti) **e** chiede al modello nuove ricette generate a partire dal frigorifero.
2. **Parsing ricetta** (`parse`) — trasforma un testo libero (es. una ricetta incollata) in una struttura `ParsedRecipeResponse` (nome, backstory, tempo, porzioni, ingredienti, passi).
3. **Adattamento ricetta** (`adapt`) — data una ricetta e una lista di vincoli (es. "senza lattosio", "vegetariana"), produce una versione adattata con l'elenco delle modifiche.
4. **Ingredienti mancanti** (di supporto, esposto dalla feature ricette) — confronta gli ingredienti della ricetta con quelli del frigorifero usando la **similarità coseno** tra embedding; se un ingrediente non ha embedding calcolato, applica un **fallback per nome** (confronto case-insensitive) prima di segnalarlo mancante.

Perché esiste: automatizzare compiti che sarebbero manuali (digitare ricette, adattarle a diete, capire cosa cucinare con ciò che si ha), mantenendo però l'AI dietro interfacce di dominio così che il resto del sistema non dipenda direttamente dagli SDK Azure.

Parti coinvolte:

- **Presentation** — `RecipeAssistantController` (`api/recipe-assistant`, azioni `suggest`/`parse`/`adapt`) con `[Authorize]` a livello di classe, e i validator (`SuggestRecipesRequestValidator`, `ParseRecipeRequestValidator`, `AdaptRecipeRequestValidator`) in `KinRecipe.Api`.
- **Business** — `Core.Business/RecipeAssistantFeature`: `KinHubRecipeAssistantManager` (`IRecipeAssistantManager`) e i modelli DTO (`SuggestRecipesResult`, `ParsedRecipeResponse`, `RecipeAdaptationResponse`, …).
- **Domain** — interfacce `IRecipeAssistantService`, `IEmbeddingService`, `IRecipeMissingIngredientsService`; modelli `RecipeSuggestion`, `RecipeAdaptationResult`, `RecipeChange`; eccezioni `RecipeAssistantUnavailableException`, `RecipeAssistantInvalidResponseException`.
- **Infrastructure** — `Core.OpenAi`: `OpenAiRecipeAssistantService` (chat completion con resilienza tramite `OpenAiExecutionHelper`), `OpenAiEmbeddingService` (embedding), `OpenAiRecipeMissingIngredientsService` (similarità + fallback per nome).

Dati ricevuti: `SuggestRecipesRequest` (fridgeId), `ParseRecipeRequest` (rawText), `AdaptRecipeRequest` (recipeId, constraints). Dati prodotti: `SuggestRecipesResult` (ricette esistenti ordinate per match + nuove ricette), `ParsedRecipeResponse?`, `RecipeAdaptationResponse` (ricetta originale, adattata, changes).

Dipendenze: `Azure.AI.OpenAI` (chat + embedding), Pgvector per gli embedding persistiti, i repository del Core (famiglia, frigorifero, libri, ricette, ingredienti, passi).

# Casi d'uso

- **Suggerisci ricette dal frigorifero** — *Obiettivo*: proporre cosa cucinare. *Attore*: utente autenticato. *Input*: `fridgeId`. *Output*: `SuggestRecipesResult` con `ExistingRecipes` (ordinate per `MatchPercentage`) e `NewRecipes` (generate dall'AI). *Condizioni/errori*: famiglia assente → 404; frigorifero non trovato → 404; frigorifero di un'altra famiglia → 403; servizio AI non disponibile → 503; risposta AI non valida → 422 UnprocessableEntity.
- **Analizza/parsa una ricetta testuale** — *Obiettivo*: strutturare testo libero. *Input*: `rawText`. *Output*: `ParsedRecipeResponse?` (può essere `null` se il modello non riconosce una ricetta → 200 con corpo nullo). *Nota*: **non** persiste nulla; produce solo una bozza.
- **Adatta una ricetta a vincoli** — *Obiettivo*: riscrivere una ricetta rispettando vincoli. *Input*: `recipeId`, `constraints[]`. *Output*: `RecipeAdaptationResponse` (originale + adattata + changes con id ingrediente sostituito/rimosso/aggiunto). *Errori*: famiglia/ricetta/libro non trovati → 404; ricetta di un'altra famiglia → 403.
- **Ingredienti mancanti** — *Obiettivo*: quali ingredienti mancano rispetto al frigorifero. *Input*: `recipeId`, `fridgeId`. *Output*: lista di nomi. *Condizione limite*: se un ingrediente della ricetta non ha embedding, si applica un fallback per confronto per nome; solo se anche il nome non combacia è considerato mancante.

# Flusso implementativo

## 1. Punto di ingresso

`RecipeAssistantController` su `api/recipe-assistant` con `[Authorize]` a livello di classe:

- `POST suggest` → `SuggestAsync` (input `SuggestRecipesRequest`);
- `POST parse` → `ParseAsync` (input `ParseRecipeRequest`);
- `POST adapt` → `AdaptAsync` (input `AdaptRecipeRequest`).

Tutte delegano a `IRecipeAssistantManager` (`KinHubRecipeAssistantManager`). L'endpoint di ingredienti mancanti è invece su `RecipeController` (feature ricette) e usa `IRecipeMissingIngredientsService`.

## 2. Validazione iniziale

- Null-check del body.
- Validazione FluentValidation via `IRequestValidator<T>` (es. `AdaptRecipeRequestValidator`) → 400 con errori.
- L'autorizzazione di dominio (famiglia/possesso) è verificata **dentro** il manager.

## 3. Orchestrazione applicativa

- **suggest** (`KinHubRecipeAssistantManager.SuggestRecipesAsync`):
  1. risolve la famiglia (`_familyRepository.FindByUserIdAsync`), il frigorifero e verifica `fridge.FamilyId == family.Id`;
  2. costruisce una mappa nome→quantità disponibile dagli ingredienti del frigorifero;
  3. carica **tutti** gli ingredienti delle ricette della famiglia con un'unica chiamata batch (`GetAllByRecipeIdsAsync`), organizzandoli in un dizionario per `RecipeId`; per ogni ricetta calcola gli **ingredienti mancanti** e la **percentuale di match** (`(totali − mancanti)/totali * 100`) e li accumula in `ExistingRecipeSuggestionResponse`;
  4. ordina le esistenti per `MatchPercentage` decrescente;
  5. chiama `_recipeAssistantService.SuggestNewRecipesAsync(fridgeAi)` per ottenere nuove ricette dal modello, avvolto da resilienza (`OpenAiExecutionHelper`);
  6. compone `SuggestRecipesResult` (usa **Mapster** `Adapt<…>` per convertire i modelli di dominio in DTO di risposta).
- **parse** (`ParseRecipeAsync`): chiama direttamente `_recipeAssistantService.ParseRecipeAsync(rawText)` e mappa in `ParsedRecipeResponse?`.
- **adapt** (`AdaptRecipeAsync`): risolve famiglia + ricetta + libro (con controllo `book.FamilyId == family.Id`), costruisce la ricetta AI completa (`BuildAiRecipeAsync` carica ingredienti e passi ordinati), poi chiama `_recipeAssistantService.AdaptRecipeAsync(aiRecipe, constraints)` e mappa in `RecipeAdaptationResponse`.

## 4. Logica di dominio e trasformazioni AI

- Il **controllo di accesso** (famiglia possiede il frigorifero/la ricetta) è la regola di dominio applicata dal manager, coerente con gli access service della feature ricette.
- Il calcolo del **match** e degli **ingredienti mancanti** per le ricette esistenti è logica applicativa deterministica (nessuna AI): confronto per nome (case-insensitive) e quantità.
- La parte AI vive in `OpenAiRecipeAssistantService`:
  - costruisce un input JSON con un `task_type` (`recipe_suggestion`/`recipe_parsing`/`recipe_adaptation`) e i dati;
  - invia una **chat completion** ad Azure OpenAI con un *system prompt* dedicato e `ResponseFormat = JsonObject` (output JSON forzato) e temperature calibrate (0.7 per suggerimenti, 0.3 per parsing/adattamento);
  - deserializza la risposta con `DeserializeRequired<T>` che lancia `RecipeAssistantInvalidResponseException` se il JSON non è conforme o un campo richiesto è assente (nessun null-forgiving).
  - avvolge ogni chiamata con `OpenAiExecutionHelper` (timeout configurabile + retry con backoff) che lancia `RecipeAssistantUnavailableException` in caso di errore dell'SDK o timeout.
- In `AdaptRecipeAsync` (infrastruttura) la ricetta adattata è ricostruita applicando le `Changes` restituite dal modello (sostituzione per `OriginalIngredientId`, rimozione, aggiunta di tipo `addition`).
- Per gli **embedding mancanti**: `OpenAiRecipeMissingIngredientsService` applica il confronto per similarità coseno (soglia 0.85) quando l'embedding è disponibile; se assente, tenta un **fallback per nome** (case-insensitive) sull'ingrediente del frigorifero prima di dichiararlo mancante.

## 5. Accesso ai dati

- Letture dai repository del Core: `IFamilyRepository`, `IFridgeRepository`, `IFridgeIngredientRepository`, `IRecipeBookRepository`, `IRecipeRepository`, `IRecipeIngredientRepository` (con metodo batch `GetAllByRecipeIdsAsync`), `IRecipeStepRepository`.
- Gli embedding degli ingredienti (`float[]`) sono persistiti tramite Pgvector e letti per il calcolo della similarità.
- **Nessuna scrittura**: l'assistente produce bozze/suggerimenti; la persistenza avviene solo se l'utente poi crea la ricetta tramite la feature ricette.

## 6. Integrazioni esterne

- **Azure OpenAI – Chat** (`OpenAiRecipeAssistantService`): client `AzureOpenAIClient` + `GetChatClient(ModelDeploymentName)`, autenticazione con `AzureKeyCredential`. Prompt di sistema configurabili (`ParseRecipeSystemPrompt`, `SuggestRecipesSystemPrompt`, `AdaptRecipeSystemPrompt`). Chiamate avvolte da `OpenAiExecutionHelper` (timeout + retry).
- **Azure OpenAI – Embedding** (`OpenAiEmbeddingService`): `GetEmbeddingClient(EmbeddingDeploymentName)`, `GenerateEmbeddingAsync` → `float[]`.
- **Ingredienti mancanti** (`OpenAiRecipeMissingIngredientsService`): confronta gli embedding pre-calcolati con **cosine similarity** e soglia `0.85`; se l'ingrediente della ricetta non ha embedding, applica un fallback per nome prima di segnalarlo mancante.

## 7. Gestione errori

- Il manager ritorna `Result<T>` con `NotFound`/`Unauthorized` per i controlli famiglia/possesso; mappati in HTTP da `HttpResultMapper` (Unauthorized→403).
- Le chiamate AI sono avvolte da gestione errori tipizzata nel manager: `RecipeAssistantUnavailableException` (errore SDK o timeout) → `ServiceUnavailable` (503); `RecipeAssistantInvalidResponseException` (JSON non conforme o campo richiesto assente) → `UnprocessableEntity` (422). Non esistono più eccezioni non gestite a 500 per i fallimenti AI tipici.
- Il parsing può legittimamente restituire `null` (nessuna ricetta riconosciuta) → 200 con corpo nullo, non un errore.

## 8. Output finale

- **suggest**: `SuggestRecipesResult` (esistenti ordinate per match + nuove ricette AI). Nessun dato persistito.
- **parse**: `ParsedRecipeResponse?` (bozza). Nessun dato persistito.
- **adapt**: `RecipeAdaptationResponse` (originale + adattata + changes). Nessun dato persistito.
- **missing-ingredients**: `{ missingIngredients: [...] }`.

# Pattern correttamente implementati

- **Adapter (anti-corruption verso l'AI)** — `IRecipeAssistantService`/`IEmbeddingService` (Domain) con implementazioni `OpenAiRecipeAssistantService`/`OpenAiEmbeddingService` (Infrastructure). *Perché corretto*: il Business (`KinHubRecipeAssistantManager`) lavora su modelli di dominio (`Recipe`, `RecipeSuggestion`) e non conosce Azure OpenAI; la serializzazione/deserializzazione JSON e i prompt restano confinati nell'infrastruttura. *Problema risolto*: disaccoppia il caso d'uso dal fornitore AI (sostituibile).

- **Application Service / Orchestratore** — `KinHubRecipeAssistantManager` coordina repository + AI + regole di accesso per ogni caso d'uso. *Correttezza*: unico punto che combina dati locali (match deterministico, batch `GetAllByRecipeIdsAsync`) e generazione AI, mantenendo la sicurezza (controllo famiglia) prima di invocare il modello.

- **Resilienza AI (`OpenAiExecutionHelper`)** — timeout configurabile + retry con backoff attorno ad ogni chiamata AI. Fallimenti mappati in eccezioni tipizzate (`RecipeAssistantUnavailableException`/`RecipeAssistantInvalidResponseException`), poi tradotti in `Result` semantici. *Correttezza*: l'AI è trattata come integrazione esterna instabile, con degrado controllato anziché propagazione di eccezioni raw.

- **Deserializzazione difensiva (`DeserializeRequired<T>`)** — lancia `RecipeAssistantInvalidResponseException` su output non conforme invece di null-forgiving. *Correttezza*: rende espliciti i contratti di output del modello e produce errori diagnosticabili.

- **Strategy sui prompt/parametri** — `OpenAiRecipeAssistantService.SendAsync` parametrizza system prompt e temperature per task. *Correttezza*: comportamento del modello calibrato per compito (creatività alta sui suggerimenti, bassa su parsing/adattamento) con output JSON forzato.

- **Options Pattern** — `OpenAiOptions` (endpoint, api key, deployment, prompt) da configurazione. *Correttezza*: nessun segreto/prompt hardcoded nel codice applicativo.

- **DTO Pattern + Mapper** — record interni per il JSON dell'AI e modelli di dominio separati, con mapping esplicito (`MapToRecipe`, ecc.) e Mapster verso i DTO di risposta.

# Anti-pattern

- **Caricamento passi separato per `adapt`** — *File*: `KinHubRecipeAssistantManager.BuildAiRecipeAsync`. I passi della ricetta da adattare sono caricati con una chiamata separata rispetto agli ingredienti. *Problema*: in un contesto di adattamento su ricette con molti ingredienti/passi il numero di chiamate è ancora O(1) per ricetta singola, ma il pattern rimane inconsistente rispetto al batch usato per `suggest`. *Gravità*: bassa. *Direzione*: uniformare con un caricamento aggregato se il numero di ricette adattate in batch dovesse crescere.

> I contenuti esatti dei system prompt e i nomi dei deployment del modello dipendono dalla configurazione e non sono deducibili con certezza dalla codebase analizzata.
