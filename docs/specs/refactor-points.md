> Stato validazione: PASS
> Iterazioni eseguite: 2

# Descrizione generale

Il backend di **Kin.KinHub** è un monolite modulare in **.NET 10 / ASP.NET Core** organizzato per bounded context (Core, Identity, KinList) secondo una **Clean Architecture** a quattro anelli (`Domain` → `Business` → `Infrastructure`/`Presentation`). L'impianto è nel complesso **solido e coerente**: la direzione delle dipendenze è rispettata, i pattern di base (Repository, Application Service, Result, Options con validazione *fail-fast*, autorizzazione basata su policy e contesto famiglia) sono applicati in modo uniforme, e il contesto KinList mostra maturità enterprise (concorrenza ottimistica con ETag, idempotenza, transazioni esplicite, retry/poison queue, telemetria).

Tuttavia, analizzando il backend con l'ottica di un sistema destinato a crescere, emergono aree che concentrano **rischio reale** e che vanno rifattorizzate prima che il costo di intervento aumenti. I temi ricorrenti sono:

1. **Consistenza transazionale delle scritture nel contesto Core** — le creazioni multi-entità (famiglia, ricetta) non sono avvolte in una transazione e usano scritture in loop (N+1), con rischio di dati parzialmente inseriti.
2. **Resilienza delle integrazioni AI** — le chiamate ad Azure OpenAI nel contesto ricette non hanno timeout, retry né gestione degli errori, a differenza della pipeline audio di KinList che invece è resiliente.
3. **Un service "onnisciente"** (`KinListService`) che implementa sia l'API sincrona sia il processore asincrono del worker.
4. **Un controller OAuth troppo grande** con rendering HTML inline e una chiamata *sync-over-async* su un percorso di richiesta.
5. **Duplicazione dell'infrastruttura trasversale** (tipi `Result`/`ResultStatus`, mapper HTTP, `ProblemDetails`, eccezioni di dominio, `PostgreSqlOptions`/`PostgreSqlRepository`) replicata per modulo, con divergenze già presenti.
6. **Guardie di autenticazione/contesto famiglia ripetute** in ogni action dei controller.

Nessuno di questi punti è "cosmetico": ognuno ha un impatto misurabile su integrità dei dati, affidabilità, sicurezza o manutenibilità, e ognuno è ancorato a file/classi/metodi reali del repository. La codebase ha già una discreta copertura di test (progetto `Kin.KinHub.Core.Test` con test di integrazione e di caratterizzazione), il che rende i refactor proposti eseguibili in sicurezza.

# Punti che richiedono refactor

### 1. Scritture Core non transazionali e N+1 (creazione famiglia e ricetta)
- **Feature/area**: Core – Family & Recipe (persistenza/consistenza).
- **Gravità**: **Alta**.
- **Motivazione tecnica**: `CreateFamilyHandler.HandleAsync` crea `Family`, ogni `FamilyMember` e ogni `FamilyService` con `CreateAsync` separati (ciascuno con il proprio `SaveChanges`) senza transazione; idem `CreateRecipeHandler.HandleAsync` per ricetta + ingredienti + passi.
- **Impatto attuale**: un errore a metà lascia dati **parzialmente inseriti** (es. famiglia senza servizi, ricetta senza ingredienti). Round-trip multipli per operazione.
- **Rischio futuro**: inconsistenza cronica, difficoltà a diagnosticare stati sporchi, degrado di performance con dataset più grandi.
- **File/classi/metodi**: `src/Businesses/Kin.KinHub.Core.Business/FamilyFeature/Commands/CreateFamily/CreateFamilyHandler.cs`; `.../RecipeFeature/Commands/CreateRecipe/CreateRecipeHandler.cs`; `src/Infrastructures/Kin.KinHub.Core.PostgreSql/Common/PostgreSqlRepository.cs`; confronto positivo: `src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Common/EfKinListTransactionExecutor.cs`.
- **Dettaglio**: [msrefactor-write-transaction-consistency.md](./msrefactor-write-transaction-consistency.md)

### 2. Resilienza mancante nelle integrazioni AI del ricettario
- **Feature/area**: Core – Recipe Assistant (integrazione Azure OpenAI).
- **Gravità**: **Alta**.
- **Motivazione tecnica**: `OpenAiRecipeAssistantService` invoca `CompleteChatAsync` e fa `JsonSerializer.Deserialize<…>(json)!` (null-forgiving) senza try/catch, timeout o retry; `KinHubRecipeAssistantManager` non gestisce i fallimenti dell'AI e itera con letture N+1.
- **Impatto attuale**: un errore del servizio o un JSON inatteso diventa un'eccezione non gestita (HTTP 500), senza messaggio controllato né osservabilità dedicata.
- **Rischio futuro**: instabilità sotto carico, difficoltà di diagnosi, esperienza degradata; incoerenza con lo standard di resilienza già presente in KinList.
- **File/classi/metodi**: `src/Infrastructures/Kin.KinHub.Core.OpenAi/RecipeAssistantFeature/Services/OpenAiRecipeAssistantService.cs`; `src/Businesses/Kin.KinHub.Core.Business/RecipeAssistantFeature/Services/KinHubRecipeAssistantManager.cs`; riferimento positivo: `src/Infrastructures/Kin.KinHub.KinList.Ai/Common/TransientExecutionHelper.cs`.
- **Dettaglio**: [msrefactor-ai-recipe-resilience.md](./msrefactor-ai-recipe-resilience.md)

### 3. `KinListService` come God Service con doppia responsabilità
- **Feature/area**: KinList – Business.
- **Gravità**: **Media**.
- **Motivazione tecnica**: `KinListService` (~1075 righe) implementa sia `IKinListService` (CRUD API) sia `IAudioOperationProcessor` (worker), coprendo liste, item, idempotenza, ciclo di vita operazioni audio, mapping e deduplica; logica di deduplica duplicata in due metodi.
- **Impatto attuale**: file critico e condiviso tra API e worker, difficile da testare/estendere.
- **Rischio futuro**: ogni evoluzione tocca un punto ad alto rischio; barriera per sviluppatori junior.
- **File/classi/metodi**: `src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs` (metodi CRUD + `ProcessAudioOperationAsync`, `CreateItemDraftsFromAudioAsync`, `MapAudioOperationAsync`).
- **Dettaglio**: [msrefactor-kinlist-service-decomposition.md](./msrefactor-kinlist-service-decomposition.md)

### 4. `OAuthController` "Fat Controller" + sync-over-async
- **Feature/area**: Identity – OAuth/Authentication (presentazione + sicurezza).
- **Gravità**: **Alta**.
- **Motivazione tecnica**: `OAuthController` (~730 righe) mescola validazione OAuth, gestione cookie di sessione, verifica PKCE, ri-firma dei token **e generazione HTML** della pagina di login (`RenderLoginPage`); `RehydrateLoginResponse` esegue `RefreshTokenAsync(...).GetAwaiter().GetResult()` (blocco sincrono su async) in un percorso di richiesta.
- **Impatto attuale**: rischio di thread starvation/deadlock sotto carico; codice di sicurezza difficile da isolare e testare.
- **Rischio futuro**: regressioni di sicurezza dif­ficili da individuare; scalabilità limitata dell'endpoint di autorizzazione.
- **File/classi/metodi**: `src/Presentations/Kin.KinHub.Identity.Api/AuthenticationFeature/Controllers/OAuthController.cs` (`Authorize`, `AuthorizeAsync`, `RehydrateLoginResponse`, `RenderLoginPage`, `CreateScopedTokenResponse`).
- **Dettaglio**: [msrefactor-oauth-controller.md](./msrefactor-oauth-controller.md)

### 5. Duplicazione dell'infrastruttura trasversale (Result / error handling / persistenza base)
- **Feature/area**: Cross-cutting (tutti i moduli).
- **Gravità**: **Media**.
- **Motivazione tecnica**: `Result<T>`/`ResultStatus` esistono in tre copie (`Core.Business`, `Identity.Business`, `KinList.Business`) **già divergenti** (KinList ha `Code` e `UnprocessableEntity`, gli altri no); tre mapper HTTP (`HttpResultMapper`, `IdentityHttpResultMapper`, `KinListHttpResultMapper`); eccezioni di dominio duplicate (`Core.Domain/Common/Exceptions`, `Identity.Domain/Common/Exceptions`); `PostgreSqlOptions`/`PostgreSqlRepository` duplicati per modulo. Inoltre `RegisterUserHandler` mappa `DomainException` a `UnexpectedError` (500) mascherando errori di validazione.
- **Impatto attuale**: comportamenti HTTP potenzialmente incoerenti tra host; correzioni da applicare in più punti.
- **Rischio futuro**: divergenza crescente del contratto d'errore, onere manutentivo, confusione per chi lavora su più moduli.
- **File/classi/metodi**: `*/Common/Result.cs`, `*/Common/ResultStatus.cs`; `src/Presentations/Kin.KinHub.Shared.Api/Common/HttpResultMapper.cs`, `IdentityHttpResultMapper.cs`; `src/Presentations/Kin.KinHub.KinList.Api/Common/KinListHttpResultMapper.cs`; `*/Common/Exceptions/*`; `RegisterUserHandler.cs`.
- **Dettaglio**: [msrefactor-shared-result-error-handling.md](./msrefactor-shared-result-error-handling.md)

### 6. Guardie di autenticazione / contesto famiglia ripetute nei controller
- **Feature/area**: Cross-cutting – Presentazione (KinRecipe.Api, KinList.Api).
- **Gravità**: **Bassa** (tendente a media per l'estensione).
- **Motivazione tecnica**: ogni action di `ListsController`/`AudioOperationsController` ripete `if (!_currentUser.IsAuthenticated) …` e `if (!_currentUser.HasFamilyContext) …`; i controller di `KinRecipe.Api` ripetono il controllo `IsAuthenticated` benché già protetti da `[Authorize]`.
- **Impatto attuale**: forte duplicazione, rischio di dimenticanze/incoerenze tra endpoint.
- **Rischio futuro**: un endpoint nuovo che dimentica la guardia introduce una falla di autorizzazione; leggibilità ridotta.
- **File/classi/metodi**: `src/Presentations/Kin.KinHub.KinList.Api/KinListFeature/Controllers/ListsController.cs`, `AudioOperationsController.cs`; `src/Presentations/Kin.KinHub.KinRecipe.Api/RecipeFeature/Controllers/*.cs`, `RecipeAssistantFeature/Controllers/RecipeAssistantController.cs`.
- **Dettaglio**: [msrefactor-controller-auth-guards.md](./msrefactor-controller-auth-guards.md)

# Priorità consigliata

1. **#2 Resilienza AI** e **#1 Consistenza transazionale Core** — *prima di tutto*. Sono i due punti che generano **rischi immediati** (500 non gestiti verso l'utente e possibile inconsistenza dei dati). Sono anche a scope contenuto e facilmente testabili: alto beneficio, basso rischio.
2. **#4 OAuthController (sync-over-async)** — subito dopo, perché tocca un percorso di **sicurezza** e uno *sync-over-async* può causare instabilità in produzione. La parte di rischio (deadlock) è isolabile e correggibile per prima, separandola dal refactor estetico del rendering HTML.
3. **#5 Consolidamento Result/error handling** — abilitante per la coerenza del contratto API; conviene farlo prima di espandere le feature ma dopo aver stabilizzato i rischi acuti.
4. **#3 Decomposizione KinListService** — refactor strutturale di manutenibilità: importante ma non urgente; va fatto con test di caratterizzazione già presenti a copertura.
5. **#6 Guardie nei controller** — quick win di igiene, da fare insieme o dopo #5.

# Dipendenze tra refactor

- **#5 → #6**: consolidare i tipi `Result` e i mapper HTTP (#5) rende più semplice e coerente centralizzare le guardie nei controller (#6), perché entrambe producono lo stesso `ProblemDetails`. Conviene fare #5 prima o insieme a #6.
- **#5 abilita #2 e #1 in parte**: una gerarchia d'errore unificata (con uno stato tipo `ServiceUnavailable`/`UnprocessableEntity` disponibile ovunque) semplifica la mappatura dei fallimenti AI (#2) e dei conflitti transazionali (#1). Non è bloccante, ma riduce il rework.
- **#3 è indipendente** ma trae beneficio dall'avere prima i test di caratterizzazione stabili (già in parte presenti: `KinListServiceTests`, `KinListAudioPipelineTests`).
- **#1 e #2 sono indipendenti tra loro** e possono procedere in parallelo (feature/context diversi: persistenza Core vs integrazione OpenAI).
- **#4** è indipendente dagli altri; solo la parte "estrazione rendering HTML" può attendere, mentre la correzione *sync-over-async* è prioritaria e autonoma.

# Rischi se non si interviene

- **Perdita/inconsistenza dei dati (#1)**: creazioni parziali di famiglie/ricette non rilevate portano a stati corrotti difficili da correggere retroattivamente; con la crescita del traffico la probabilità di interruzioni a metà scrittura aumenta.
- **Instabilità e cattiva UX (#2, #4)**: senza resilienza AI, un disservizio di Azure OpenAI si propaga come 500 non gestiti; il *sync-over-async* dell'OAuth può causare esaurimento del thread pool e deadlock proprio sull'endpoint di login, con impatto **di business** diretto (utenti che non riescono ad autenticarsi).
- **Contratto API incoerente (#5)**: la divergenza dei tipi `Result` e dei mapper porta a risposte d'errore non uniformi tra i tre host, complicando client e integrazioni e aumentando i bug di regressione.
- **Costo di manutenzione crescente (#3, #6)**: un God Service e guardie duplicate rallentano ogni evoluzione, alzano la barriera per gli sviluppatori junior e aumentano il rischio che un nuovo endpoint introduca una falla di autorizzazione o una regressione.
- **Debito che si auto-alimenta**: rinviare #5/#6 rende più costosi tutti gli altri interventi, perché ogni nuova feature replica i pattern duplicati esistenti.
