# Descrizione generale

La macro feature **Assistente Ricette AI** aggiunge al ricettario funzionalità basate su intelligenza artificiale generativa (Azure OpenAI). Sfrutta gli stessi dati di dominio della gestione ricette (vedi [flusso-gestione-ricette.md](./flusso-gestione-ricette.md)) ma li combina con modelli di linguaggio e con embedding vettoriali.

Cosa fa (tre funzioni principali + una di supporto):

1. **Suggerimento ricette** (`suggest`) — dato un frigorifero, calcola le ricette esistenti della famiglia meglio realizzabili con gli ingredienti disponibili (con percentuale di match e ingredienti mancanti) **e** chiede al modello nuove ricette generate a partire dal frigorifero.
2. **Parsing ricetta** (`parse`) — trasforma un testo libero (es. una ricetta incollata) in una struttura `ParsedRecipeResponse` (nome, backstory, tempo, porzioni, ingredienti, passi).
3. **Adattamento ricetta** (`adapt`) — data una ricetta e una lista di vincoli (es. "senza lattosio", "vegetariana"), produce una versione adattata con l'elenco delle modifiche.
4. **Ingredienti mancanti** (di supporto, esposto dalla feature ricette) — confronta gli ingredienti della ricetta con quelli del frigorifero usando la **similarità coseno** tra embedding.

Perché esiste: automatizzare compiti che sarebbero manuali (digitare ricette, adattarle a diete, capire cosa cucinare con ciò che si ha), mantenendo però l'AI dietro interfacce di dominio così che il resto del sistema non dipenda direttamente dagli SDK Azure.

Parti coinvolte:

- **Presentation** — `RecipeAssistantController` (`api/recipe-assistant`, azioni `suggest`/`parse`/`adapt`) e i validator (`SuggestRecipesRequestValidator`, `ParseRecipeRequestValidator`, `AdaptRecipeRequestValidator`) in `KinRecipe.Api`.
- **Business** — `Core.Business/RecipeAssistantFeature`: `KinHubRecipeAssistantManager` (`IRecipeAssistantManager`) e i modelli DTO (`SuggestRecipesResult`, `ParsedRecipeResponse`, `RecipeAdaptationResponse`, …).
- **Domain** — interfacce `IRecipeAssistantService`, `IEmbeddingService`, `IRecipeMissingIngredientsService`; modelli `RecipeSuggestion`, `RecipeAdaptationResult`, `RecipeChange`.
- **Infrastructure** — `Core.OpenAi`: `OpenAiRecipeAssistantService` (chat completion), `OpenAiEmbeddingService` (embedding), `OpenAiRecipeMissingIngredientsService` (similarità).

Dati ricevuti: `SuggestRecipesRequest` (fridgeId), `ParseRecipeRequest` (rawText), `AdaptRecipeRequest` (recipeId, constraints). Dati prodotti: `SuggestRecipesResult` (ricette esistenti ordinate per match + nuove ricette), `ParsedRecipeResponse?`, `RecipeAdaptationResponse` (ricetta originale, adattata, changes).

Dipendenze: `Azure.AI.OpenAI` (chat + embedding), Pgvector per gli embedding persistiti, i repository del Core (famiglia, frigorifero, libri, ricette, ingredienti, passi).

# Casi d'uso

- **Suggerisci ricette dal frigorifero** — *Obiettivo*: proporre cosa cucinare. *Attore*: utente autenticato. *Input*: `fridgeId`. *Output*: `SuggestRecipesResult` con `ExistingRecipes` (ordinate per `MatchPercentage`) e `NewRecipes` (generate dall'AI). *Condizioni/errori*: famiglia assente → 404; frigorifero non trovato → 404; frigorifero di un'altra famiglia → 403.
- **Analizza/parsa una ricetta testuale** — *Obiettivo*: strutturare testo libero. *Input*: `rawText`. *Output*: `ParsedRecipeResponse?` (può essere `null` se il modello non riconosce una ricetta → 200 con corpo nullo). *Nota*: **non** persiste nulla; produce solo una bozza.
- **Adatta una ricetta a vincoli** — *Obiettivo*: riscrivere una ricetta rispettando vincoli. *Input*: `recipeId`, `constraints[]`. *Output*: `RecipeAdaptationResponse` (originale + adattata + changes con id ingrediente sostituito/rimosso/aggiunto). *Errori*: famiglia/ricetta/libro non trovati → 404; ricetta di un'altra famiglia → 403.
- **Ingredienti mancanti** — *Obiettivo*: quali ingredienti mancano rispetto al frigorifero. *Input*: `recipeId`, `fridgeId`. *Output*: lista di nomi. *Condizione limite*: se un ingrediente della ricetta non ha embedding, è considerato **mancante** per default.

# Flusso implementativo

## 1. Punto di ingresso

`RecipeAssistantController` su `api/recipe-assistant`:

- `POST suggest` → `SuggestAsync` (input `SuggestRecipesRequest`);
- `POST parse` → `ParseAsync` (input `ParseRecipeRequest`);
- `POST adapt` → `AdaptAsync` (input `AdaptRecipeRequest`).

Tutte delegano a `IRecipeAssistantManager` (`KinHubRecipeAssistantManager`). L'endpoint di ingredienti mancanti è invece su `RecipeController` (feature ricette) e usa `IRecipeMissingIngredientsService`.

## 2. Validazione iniziale

- `if (!_currentUser.IsAuthenticated) return ApiProblemDetails.AuthenticationRequired(this);`.
- Null-check del body.
- Validazione FluentValidation via `IRequestValidator<T>` (es. `AdaptRecipeRequestValidator`) → 400 con errori.
- L'autorizzazione di dominio (famiglia/possesso) è verificata **dentro** il manager.

## 3. Orchestrazione applicativa

- **suggest** (`KinHubRecipeAssistantManager.SuggestRecipesAsync`):
  1. risolve la famiglia (`_familyRepository.FindByUserIdAsync`), il frigorifero e verifica `fridge.FamilyId == family.Id`;
  2. costruisce una mappa nome→quantità disponibile dagli ingredienti del frigorifero;
  3. itera su tutti i libri e ricette della famiglia, carica gli ingredienti di ciascuna ricetta, calcola gli **ingredienti mancanti** e la **percentuale di match** (`(totali − mancanti)/totali * 100`), e li accumula in `ExistingRecipeSuggestionResponse`;
  4. ordina le esistenti per `MatchPercentage` decrescente;
  5. chiama `_recipeAssistantService.SuggestNewRecipesAsync(fridgeAi)` per ottenere nuove ricette dal modello;
  6. compone `SuggestRecipesResult` (usa **Mapster** `Adapt<…>` per convertire i modelli di dominio in DTO di risposta).
- **parse** (`ParseRecipeAsync`): chiama direttamente `_recipeAssistantService.ParseRecipeAsync(rawText)` e mappa in `ParsedRecipeResponse?`.
- **adapt** (`AdaptRecipeAsync`): risolve famiglia + ricetta + libro (con controllo `book.FamilyId == family.Id`), costruisce la ricetta AI completa (`BuildAiRecipeAsync` carica ingredienti e passi ordinati), poi chiama `_recipeAssistantService.AdaptRecipeAsync(aiRecipe, constraints)` e mappa in `RecipeAdaptationResponse`.

## 4. Logica di dominio e trasformazioni AI

- Il **controllo di accesso** (famiglia possiede il frigorifero/la ricetta) è la regola di dominio applicata dal manager, coerente con gli access service della feature ricette.
- Il calcolo del **match** e degli **ingredienti mancanti** per le ricette esistenti è logica applicativa deterministica (nessuna AI): confronto per nome (case-insensitive) e quantità.
- La parte AI vive in `OpenAiRecipeAssistantService`:
  - costruisce un input JSON con un `task_type` (`recipe_suggestion`/`recipe_parsing`/`recipe_adaptation`) e i dati;
  - invia una **chat completion** ad Azure OpenAI con un *system prompt* dedicato e `ResponseFormat = JsonObject` (output JSON forzato) e temperature calibrate (0.7 per suggerimenti, 0.3 per parsing/adattamento);
  - deserializza la risposta JSON in record interni e la rimappa nei modelli di dominio (`MapToRecipe`, `MapToIngredient`, `MapToStep`, `MapToSuggestion`).
- In `AdaptRecipeAsync` (infrastruttura) la ricetta adattata è ricostruita applicando le `Changes` restituite dal modello (sostituzione per `OriginalIngredientId`, rimozione, aggiunta di tipo `addition`).

## 5. Accesso ai dati

- Letture dai repository del Core: `IFamilyRepository`, `IFridgeRepository`, `IFridgeIngredientRepository`, `IRecipeBookRepository`, `IRecipeRepository`, `IRecipeIngredientRepository`, `IRecipeStepRepository`.
- Gli embedding degli ingredienti (`float[]`) sono persistiti tramite Pgvector e letti per il calcolo della similarità.
- **Nessuna scrittura**: l'assistente produce bozze/suggerimenti; la persistenza avviene solo se l'utente poi crea la ricetta tramite la feature ricette.

## 6. Integrazioni esterne

- **Azure OpenAI – Chat** (`OpenAiRecipeAssistantService`): client `AzureOpenAIClient` + `GetChatClient(ModelDeploymentName)`, autenticazione con `AzureKeyCredential`. Prompt di sistema configurabili (`ParseRecipeSystemPrompt`, `SuggestRecipesSystemPrompt`, `AdaptRecipeSystemPrompt`).
- **Azure OpenAI – Embedding** (`OpenAiEmbeddingService`): `GetEmbeddingClient(EmbeddingDeploymentName)`, `GenerateEmbeddingAsync` → `float[]`.
- **Ingredienti mancanti** (`OpenAiRecipeMissingIngredientsService`): confronta gli embedding pre-calcolati con **cosine similarity** e soglia `0.85`; se l'ingrediente della ricetta non ha embedding, è considerato mancante.

## 7. Gestione errori

- Il manager ritorna `Result<T>` con `NotFound`/`Unauthorized` per i controlli famiglia/possesso; mappati in HTTP da `HttpResultMapper` (Unauthorized→403).
- **Le chiamate AI non sono avvolte da gestione errori dedicata nel manager**: eventuali eccezioni degli SDK Azure o deserializzazioni fallite (`Deserialize<…>!` con null-forgiving) risalgono come eccezioni non gestite → 500 (a differenza della pipeline audio di KinList, che gestisce esplicitamente i fallimenti transitori). Vedi Anti-pattern.
- Il parsing può legittimamente restituire `null` (nessuna ricetta riconosciuta) → 200 con corpo nullo, non un errore.

## 8. Output finale

- **suggest**: `SuggestRecipesResult` (esistenti ordinate per match + nuove ricette AI). Nessun dato persistito.
- **parse**: `ParsedRecipeResponse?` (bozza). Nessun dato persistito.
- **adapt**: `RecipeAdaptationResponse` (originale + adattata + changes). Nessun dato persistito.
- **missing-ingredients**: `{ missingIngredients: [...] }`.

# Pattern correttamente implementati

- **Adapter (anti-corruption verso l'AI)** — `IRecipeAssistantService`/`IEmbeddingService` (Domain) con implementazioni `OpenAiRecipeAssistantService`/`OpenAiEmbeddingService` (Infrastructure). *Perché corretto*: il Business (`KinHubRecipeAssistantManager`) lavora su modelli di dominio (`Recipe`, `RecipeSuggestion`) e non conosce Azure OpenAI; la serializzazione/deserializzazione JSON e i prompt restano confinati nell'infrastruttura. *Problema risolto*: disaccoppia il caso d'uso dal fornitore AI (sostituibile).

- **Application Service / Orchestratore** — `KinHubRecipeAssistantManager` coordina repository + AI + regole di accesso per ogni caso d'uso. *Correttezza*: unico punto che combina dati locali (match deterministico) e generazione AI, mantenendo la sicurezza (controllo famiglia) prima di invocare il modello.

- **Strategy sui prompt/parametri** — `OpenAiRecipeAssistantService.SendAsync` parametrizza system prompt e temperature per task. *Correttezza*: comportamento del modello calibrato per compito (creatività alta sui suggerimenti, bassa su parsing/adattamento) con output JSON forzato.

- **Options Pattern** — `OpenAiOptions` (endpoint, api key, deployment, prompt) da configurazione. *Correttezza*: nessun segreto/prompt hardcoded nel codice applicativo.

- **DTO Pattern + Mapper** — record interni per il JSON dell'AI e modelli di dominio separati, con mapping esplicito (`MapToRecipe`, ecc.) e Mapster verso i DTO di risposta.

# Anti-pattern

- **Mancanza di gestione errori/timeout sulle chiamate AI** — *File*: `KinHubRecipeAssistantManager` e `OpenAiRecipeAssistantService`. Le chiamate a `CompleteChatAsync` e le `JsonSerializer.Deserialize<…>(json, …)!` non hanno try/catch, retry o timeout dedicati; un errore del servizio o un JSON inatteso diventa un'eccezione non gestita (500). *Problema*: assenza di resilienza verso un'integrazione esterna intrinsecamente instabile. *Impatto*: affidabilità e UX; nessun messaggio d'errore controllato. *Gravità*: media/alta. *Direzione*: introdurre retry/timeout e mappare i fallimenti su `ServiceUnavailable`, come già fatto per KinList (vedi [flusso-kinlist.md](./flusso-kinlist.md)).

- **N+1 query nel suggerimento** — *File*: `KinHubRecipeAssistantManager.SuggestRecipesAsync`. Per ogni libro carica le ricette e per ogni ricetta carica gli ingredienti con chiamate separate (`GetAllByFamilyIdAsync`). *Problema*: numero di query proporzionale al numero di ricette. *Impatto*: performance su ricettari ampi. *Gravità*: media. *Direzione*: query aggregate/`Include` o pre-caricamento.

- **`null-forgiving` sulla deserializzazione** — *File*: `OpenAiRecipeAssistantService` (`Deserialize<SuggestionResponse>(json, …)!`, ecc.). *Problema*: assume che il modello restituisca sempre JSON valido e conforme; un output non conforme provoca `NullReferenceException` invece di un errore chiaro. *Impatto*: robustezza. *Gravità*: media. *Direzione*: validare l'output e restituire un `Result` di errore controllato.

- **Ingrediente senza embedding trattato come "mancante"** — *File*: `OpenAiRecipeMissingIngredientsService.GetMissingIngredientsAsync`. *Problema*: un ingrediente privo di embedding (es. non ancora calcolato) è sempre segnalato mancante, indipendentemente dal frigorifero. *Impatto*: possibili falsi positivi funzionali. *Gravità*: bassa. *Direzione*: fallback su confronto per nome quando l'embedding non è disponibile.

> I contenuti esatti dei system prompt e i nomi dei deployment del modello dipendono dalla configurazione e non sono deducibili con certezza dalla codebase analizzata.
