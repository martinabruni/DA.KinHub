# Descrizione generale

La macro feature **Gestione Ricette** implementa il ricettario condiviso della famiglia. Il dominio è gerarchico: una famiglia possiede uno o più **libri di ricette** (`RecipeBook`); ogni libro contiene **ricette** (`Recipe`); ogni ricetta ha **ingredienti** (`RecipeIngredient`) e **passi** (`RecipeStep`). Esiste inoltre il **frigorifero** (`Fridge`) con i suoi **ingredienti** (`FridgeIngredient`), usato sia da questa feature (ingredienti mancanti) sia dall'assistente AI.

Cosa fa: offre operazioni CRUD complete su libri, ricette, ingredienti, passi, frigorifero e ingredienti del frigorifero, esposte dall'host `Kin.KinHub.KinRecipe.Api`. Ogni operazione è filtrata dal **controllo di accesso per famiglia**: un utente può vedere/modificare solo i dati della propria famiglia.

Perché esiste: separare la gestione dei contenuti culinari dal resto del sistema, riusando le stesse entità di dominio del contesto Core che alimentano anche l'assistente AI (vedi [flusso-assistente-ricette-ai.md](./flusso-assistente-ricette-ai.md)).

Parti coinvolte:

- **Presentation** — controller in `KinRecipe.Api/RecipeFeature`: `RecipeBookController`, `RecipeController`, `RecipeIngredientController`, `RecipeStepController`, `FridgeController`, `FridgeIngredientController`; validator FluentValidation; `RemoteFamilyOwnershipService` per il contesto famiglia.
- **Business** — `Core.Business/RecipeFeature`: service facciata (`KinHubRecipeService`, `KinHubRecipeBookService`, `KinHubRecipeIngredientService`, `KinHubRecipeStepService`, `KinHubFridgeService`, `KinHubFridgeIngredientService`), handler `Commands/Queries`, gli **access service** (`RecipeAccessService`, `RecipeBookAccessService`, `RecipeIngredientAccessService`, `RecipeStepAccessService`) e i **mapper** (`RecipeResponseMapper`, …).
- **Domain** — entità `RecipeBook`, `Recipe`, `RecipeIngredient`, `RecipeStep`, `Fridge`, `FridgeIngredient`; interfacce repository (`IRecipeRepository` con `AddAsync/GetByIdAsync/GetAllByFamilyIdAsync`, ecc.).
- **Infrastructure** — repository in `Core.PostgreSql/RecipeFeature`, `CoreDbContext`.

Dati ricevuti: `CreateRecipeRequest` (nome, backstory, tempo, porzioni, `recipeBookId`, ingredienti/passi inline), `UpdateRecipeRequest`, e le analoghe per libri/ingredienti/passi/frigorifero. Dati prodotti: `RecipeResponse`, `RecipeBookResponse`, `RecipeIngredientResponse`, `RecipeStepResponse`, `FridgeResponse`, `FridgeIngredientResponse`.

Dipendenze: EF Core/Npgsql, FluentValidation, JWT (per `ICurrentUser`), `RemoteFamilyOwnershipService` (HTTP verso Identity).

# Casi d'uso

- **Creazione ricetta** — *Obiettivo*: aggiungere una ricetta a un libro. *Attore*: utente autenticato della famiglia proprietaria del libro. *Input*: `recipeBookId` (route) + `CreateRecipeRequest`. *Output*: 201 con `RecipeResponse`. *Errori*: libro non accessibile → 403/404.
- **Lettura ricette / ricetta per id** — *Output*: lista o singola `RecipeResponse` filtrata per famiglia. *Errore*: non trovata o non accessibile.
- **Aggiornamento/eliminazione ricetta** — *Condizione*: la ricetta deve appartenere a un libro della famiglia dell'utente. *Errore*: accesso negato → 403.
- **CRUD libro / ingrediente / passo / frigorifero** — analoghi, ciascuno con il proprio controller, service, access service e validator.
- **Ingredienti mancanti** — *Endpoint*: `POST api/recipe-books/{recipeBookId}/recipes/{id}/missing-ingredients?fridgeId=…`. *Obiettivo*: dato un frigorifero, elencare gli ingredienti della ricetta non presenti. *Nota*: usa un confronto per **similarità di embedding** (`IRecipeMissingIngredientsService`), quindi è un caso d'uso "di confine" tra questa feature e l'assistente AI.

# Flusso implementativo

## 1. Punto di ingresso

Controller REST in `KinRecipe.Api/RecipeFeature`. Esempio rappresentativo, `RecipeController` su `api/recipe-books/{recipeBookId:guid}/recipes`:

- `CreateAsync` (POST), `GetAllAsync` (GET), `GetByIdAsync` (GET `{id}`), `UpdateAsync` (PUT `{id}`), `DeleteAsync` (DELETE `{id}`), `GetMissingIngredientsAsync` (POST `{id}/missing-ingredients`).

I controller ricette usano `[Authorize]` a livello di classe; l'autenticazione è garantita dalla pipeline JWT senza guardie inline per azione. L'utente arriva da `ICurrentUser.UserId`; l'host è bootstrappato da `AddKinHubKinRecipeApi` che registra `AddKinHubCoreBusiness()`, l'infrastruttura PostgreSQL, l'infrastruttura OpenAI e sostituisce `IFamilyOwnershipService` con `RemoteFamilyOwnershipService` (client HTTP verso Identity).

## 2. Validazione iniziale

- Null-check del body → `InvalidRequestBody`.
- Validazione FluentValidation via `IRequestValidator<T>` (es. `CreateRecipeRequestValidator`) → 400 con errori.
- **Autorizzazione di dominio**: delegata agli *access service* nel Business (non solo alla policy HTTP).

## 3. Orchestrazione applicativa

- Il controller chiama il service facciata (`IRecipeService` → `KinHubRecipeService`), che inoltra all'handler (`CreateRecipeHandler`, `GetRecipesHandler`, …).
- `CreateRecipeHandler.HandleAsync`:
  1. verifica l'accesso al libro con `IRecipeBookAccessService.GetAccessibleRecipeBookAsync(recipeBookId, userId)`; se fallisce, ritorna `access.ToResult<RecipeResponse>()`;
  2. avvolge la creazione in `ICoreTransactionExecutor.ExecuteAsync`: crea la `Recipe` (`_recipeRepository.AddAsync`), crea ingredienti e passi inline in batch (`AddRangeAsync`), garantendo atomicità;
  3. mappa il risultato con `IRecipeResponseMapper.MapAsync` e restituisce `Result.Success`.
- La trasformazione entità→DTO è centralizzata nei **mapper** dedicati (`RecipeResponseMapper`, `RecipeBookResponseMapper`, ecc.), iniettati via DI.

## 4. Logica di dominio

- Regola centrale di **autorizzazione a cascata**: per accedere a una ricetta si risale la catena `Recipe → RecipeBook → Family` e si verifica che il `RecipeBook.FamilyId` coincida con la famiglia dell'utente. Implementata in `RecipeAccessService.GetAccessibleRecipeAsync`:
  - trova la famiglia (`IFamilyRepository.FindByUserIdAsync`) → se assente, NotFound;
  - trova la ricetta → se assente, NotFound;
  - trova il libro; se `recipeBook.FamilyId != family.Id` → Unauthorized (loggato).
- Le entità (`Recipe : BaseDeletableEntity<Guid>`) contengono i dati (nome, backstory, `FinalTime` come `TimeSpan`, porzioni) e le liste opzionali di ingredienti/passi, ma non incapsulano regole (Anemic Domain Model, coerente con il resto del progetto).

## 5. Accesso ai dati

- Repository in `Core.PostgreSql/RecipeFeature`: `RecipeRepository`, `RecipeBookRepository`, `RecipeIngredientRepository`, `RecipeStepRepository`, `FridgeRepository`, `FridgeIngredientRepository`. Metodi tipici: `AddAsync`, `AddRangeAsync`, `GetByIdAsync`, `GetAllByFamilyIdAsync`.
- Letture: caricamento di famiglia, libro, ricetta e collezioni figlie separatamente (gli ingredienti/passi vengono caricati con chiamate dedicate).
- Scritture: la creazione ricetta + ingredienti + passi è eseguita dentro `ICoreTransactionExecutor.ExecuteAsync` con inserimento batch (`AddRangeAsync`); update/delete tramite i repository.
- Gli ingredienti supportano un **embedding** (`float[]`, colonna vettoriale via Pgvector) usato per il calcolo degli ingredienti mancanti.

## 6. Integrazioni esterne

- **Identity (HTTP)** — il contesto/possesso della famiglia è risolto da `RemoteFamilyOwnershipService` che chiama `GET api/access/family-context` sull'Identity.Api inoltrando l'header `Authorization`. Timeout e base URL da `FamilyContextApiOptions`.
- **Azure OpenAI (embedding)** — solo per l'endpoint `missing-ingredients`, tramite `IRecipeMissingIngredientsService` (implementazione in `Core.OpenAi`) che confronta gli embedding con la **cosine similarity** (soglia 0.85). Dettagli in [flusso-assistente-ricette-ai.md](./flusso-assistente-ricette-ai.md).

## 7. Gestione errori

- Gli access service ritornano risultati tipizzati (`RecipeAccessResult` con `NotFound`/`Unauthorized`/`Success` e `ToResult<T>()`), quindi gli handler non lanciano eccezioni per il controllo di flusso.
- Il mapping HTTP è `HttpResultMapper.ToActionResult(controller, result)`: NotFound→404, Unauthorized→**403**, ValidationError→400, ServiceUnavailable→503, con corpo `ProblemDetails`.
- Se Identity è irraggiungibile, `RemoteFamilyOwnershipService` ritorna `ServiceUnavailable` → 503 (fail-closed), distinguendolo da "nessuna famiglia" (403) e "credenziali mancanti" (401).

## 8. Output finale

- Creazione: `Recipe` + `RecipeIngredient` + `RecipeStep` persistiti in transazione; 201 con `RecipeResponse` (mappata) contenente id, dati e collezioni.
- Letture: 200 con DTO; mutazioni: 200 con l'entità aggiornata; delete: esito.
- `missing-ingredients`: 200 con `{ missingIngredients: [...] }`.

# Pattern correttamente implementati

- **Repository Pattern** — interfacce di dominio (`IRecipeRepository`, `IRecipeBookRepository`, …) con implementazioni EF Core in `Core.PostgreSql`. *Correttezza*: i repository espongono metodi orientati al dominio (`GetAllByFamilyIdAsync`, `AddRangeAsync`) e nascondono `DbContext`/LINQ al Business.

- **Domain/Access Service** — `RecipeAccessService`, `RecipeBookAccessService`, `RecipeIngredientAccessService`, `RecipeStepAccessService` centralizzano il controllo di accesso a cascata (entità → libro → famiglia). *Perché corretto*: la stessa regola non è duplicata in ogni handler; è testabile e restituisce esiti espliciti. *Problema risolto*: evita accessi cross-famiglia (una forma di IDOR).

- **Mapper/Assembler** — `RecipeResponseMapper` e affini incapsulano la conversione entità→DTO. *Correttezza*: mapping coerente e riusato tra creazione, lettura e aggiornamento; iniettato via DI, quindi sostituibile e testabile.

- **Application Service / Facade** — `KinHubRecipeService` & co. come punto stabile per i controller, con deleghe agli handler CRUD.

- **Adapter (contesto famiglia remoto)** — `RemoteFamilyOwnershipService` implementa l'interfaccia di dominio `IFamilyOwnershipService` traducendo una chiamata HTTP a Identity. *Correttezza*: il Business resta ignaro del fatto che la famiglia provenga da un altro servizio; l'host la sostituisce via `RemoveAll<IFamilyOwnershipService>() + AddHttpClient<…>`.

- **Transaction Executor** — `ICoreTransactionExecutor`/`EfCoreTransactionExecutor` avvolgono la creazione ricetta + figli in una transazione atomica con inserimento batch. *Correttezza*: elimina il rischio di ricette parzialmente create e riduce i round-trip di database.

- **Result Pattern** — esiti applicativi tipizzati mappati centralmente in HTTP.

# Anti-pattern

- **Caricamento delle collezioni con chiamate separate** — *File*: `RecipeAccessService`/handler e `KinHubRecipeAssistantManager` caricano ingredienti/passi con chiamate separate per ogni ricetta. *Problema*: in scenari che iterano molte ricette (es. suggerimenti) si generano N+1 letture. *Impatto*: performance su dataset grandi. *Gravità*: media (più evidente nell'assistente, dove il `GetAllByRecipeIdsAsync` mitiga il caso suggest ma i passi restano caricati separatamente). *Direzione*: proiezioni/`Include` o query aggregate dove necessario.

- **Naming del metodo repository fuorviante** — *File*: repository Recipe, metodi come `GetAllByFamilyIdAsync(recipeId, …)` in realtà filtrano per `recipeId`/`fridgeId`, non per `familyId`. *Problema*: il nome non riflette il parametro reale, riducendo la leggibilità. *Impatto*: manutenibilità/comprensione. *Gravità*: bassa. *Direzione*: rinominare in modo coerente col filtro applicato.

- **Anemic Domain Model** — entità di dominio prive di comportamento; regole nei service/handler. *Gravità*: bassa (scelta coerente in tutto il backend, vedi [flusso-gestione-famiglie.md](./flusso-gestione-famiglie.md)).
