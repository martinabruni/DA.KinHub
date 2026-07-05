> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Eliminare la **ripetizione delle guardie di autenticazione e di contesto famiglia** presenti in quasi ogni action dei controller di `KinList.Api` e `KinRecipe.Api`, centralizzandole in un unico punto (attributi/policy/filtri o metodi base condivisi). L'obiettivo è ridurre il boilerplate, garantire **coerenza** del comportamento di sicurezza e **prevenire falle** dovute a una guardia dimenticata in un endpoint nuovo, mantenendo invariato il comportamento esterno.

Problema risolto: duplicazione di controlli di sicurezza con rischio di incoerenza/omissione.

# Stato attuale

Diversi controller ripetono manualmente controlli che dovrebbero essere trasversali:

- `src/Presentations/Kin.KinHub.KinList.Api/KinListFeature/Controllers/ListsController.cs`: **ogni** action inizia con
  ```
  if (!_currentUser.IsAuthenticated) return ApiProblemDetails.AuthenticationRequired(this);
  if (!_currentUser.HasFamilyContext) return ApiProblemDetails.Forbidden(this, "family_required", "...");
  ```
  ripetuto in `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `RestoreAsync`, `AddItemAsync`, `BulkConfirmItemsAsync`, `UpdateItemAsync`, `DeleteItemAsync`, `RestoreItemAsync` (≈11 volte).
- `src/Presentations/Kin.KinHub.KinList.Api/KinListFeature/Controllers/AudioOperationsController.cs`: usa un helper privato `EnsureAuthenticatedFamilyContext()` chiamato all'inizio di ogni action — una versione migliore, ma comunque locale al controller e non riusabile.
- `src/Presentations/Kin.KinHub.KinRecipe.Api/RecipeFeature/Controllers/*.cs` e `RecipeAssistantFeature/Controllers/RecipeAssistantController.cs`: ripetono `if (!_currentUser.IsAuthenticated) return ApiProblemDetails.AuthenticationRequired(this);` in ogni action, **benché** l'endpoint sia già protetto dalla pipeline (`[Authorize]`/policy) e la maggior parte dei controller `KinList` sia decorata a livello di classe con `[Authorize(Policy = FamilyContextRequirement.PolicyName)]`.

Il contesto famiglia è già gestito a livello di pipeline: `JwtAuthenticationMiddleware` popola `CurrentUser` (incluso `HasFamilyContext`), la policy `FamilyContextRequirement` + `FamilyContextAuthorizationHandler` valuta l'accesso, e `FamilyAuthorizationMiddlewareResultHandler` traduce il fallimento in 401/403/503. Quindi **esiste già** l'infrastruttura per non ripetere i controlli nelle action.

# Problemi individuati

- **Duplicazione massiva (debito tecnico)**: la stessa coppia di guardie è ripetuta decine di volte; ogni nuovo endpoint deve ricordarsene.
- **Rischio di omissione = falla di autorizzazione (rischio di sicurezza)**: se un'action nuova dimentica il controllo `HasFamilyContext`, può esporre dati senza il contesto famiglia richiesto. La centralizzazione elimina questa classe di errore.
- **Ridondanza con la pipeline (debito tecnico)**: nei controller `KinRecipe.Api` il check `IsAuthenticated` è ridondante rispetto a `[Authorize]`; nei controller `KinList.Api` decorati con la policy famiglia, i check inline duplicano ciò che la policy già garantisce.
- **Incoerenza tra controller (manutenibilità)**: `AudioOperationsController` usa un helper, `ListsController` copia-incolla il blocco; due stili per lo stesso scopo.
- **Leggibilità ridotta (manutenibilità)**: le action iniziano tutte con lo stesso preambolo, allungando i metodi e distraendo dalla logica reale.

> Precisazione: questo è prevalentemente **debito tecnico/igiene** con un potenziale risvolto di sicurezza. Il comportamento attuale è corretto perché le policy `[Authorize]` sono comunque applicate; il refactor riduce il rischio *futuro* e la duplicazione, non corregge una falla già presente.

# Come Microsoft farebbe il refactor

Sfruttare l'infrastruttura di autorizzazione **già esistente** invece di aggiungere astrazioni: le guardie devono vivere nella pipeline (policy/attributi/filtri), non nelle action. Approccio incrementale e a rischio molto basso.

1. **Affidarsi alle policy esistenti**: i controller che richiedono il contesto famiglia devono usare `[Authorize(Policy = FamilyContextRequirement.PolicyName)]` a livello di classe (come già fa `ListsController` e `AudioOperationsController`), lasciando che `FamilyAuthorizationMiddlewareResultHandler` produca 401/403/503. Le guardie inline diventano ridondanti e vanno rimosse.
2. **Rimuovere i check `IsAuthenticated` ridondanti** nei controller già protetti da `[Authorize]`.
3. **Dove serve una guardia non esprimibile via policy** (casi residui), estrarre un `ControllerBase` comune o un *action filter* riusabile, invece di duplicare l'helper `EnsureAuthenticatedFamilyContext` in ogni controller.
4. **Coerenza del `ProblemDetails`**: assicurarsi che la rimozione delle guardie inline produca esattamente gli stessi corpi d'errore già emessi (`family_required`, `authentication_required`), verificato dai test.
5. **Test prima**: congelare le risposte 401/403 degli endpoint interessati, poi rimuovere le guardie e verificare l'invarianza.
6. **Rollout progressivo + rollback** per controller.

# Piano operativo

**Step 1 — Contract test sulle risposte di sicurezza.**
- *Cosa*: test che verificano, per un campione di endpoint di `ListsController`/`AudioOperationsController`/controller Recipe, i corpi 401 (`authentication_required`) e 403 (`family_required`).
- *Dove*: `src/Tests/Kin.KinHub.Core.Test` (estendere `FamilyAuthorizationGateTests`, `KinListApiIntegrationTests`).
- *Perché*: garantire l'invarianza del comportamento di sicurezza.
- *Impatto/Rischio*: nessuno sul runtime; basso.
- *Test dopo*: suite verde.

**Step 2 — Verificare/allineare gli attributi di policy a livello di classe.**
- *Cosa*: assicurare che ogni controller che richiede il contesto famiglia sia decorato con `[Authorize(Policy = FamilyContextRequirement.PolicyName)]` (o `[Authorize]` dove basta l'autenticazione).
- *Dove*: controller di `KinList.Api` e `KinRecipe.Api`.
- *Perché*: la pipeline garantisce le guardie prima di entrare nelle action.
- *Impatto previsto*: comportamento invariato (le policy già vengono valutate).
- *Rischio dello step*: basso.
- *Test dopo*: contract test Step 1.

**Step 3 — Rimuovere le guardie inline ridondanti.**
- *Cosa*: eliminare i blocchi `if (!IsAuthenticated) …` / `if (!HasFamilyContext) …` dalle action già coperte dalla policy.
- *Dove*: `ListsController.cs`, controller Recipe/RecipeAssistant.
- *Perché*: rimuovere duplicazione senza cambiare il comportamento.
- *Impatto previsto*: risposte identiche prodotte dalla pipeline invece che dall'action.
- *Rischio dello step*: medio (tocca il percorso di sicurezza); mitigato dai test Step 1.
- *Test dopo*: contract test + integrazione.

**Step 4 — Uniformare i casi residui con un filtro/base condivisa.**
- *Cosa*: dove un controllo non è esprimibile via policy, sostituire l'helper locale con un *action filter* o un `ControllerBase` comune in `Shared.Api`.
- *Dove*: `Shared.Api/Common` + controller interessati.
- *Perché*: un unico punto per l'eventuale logica residua.
- *Impatto/Rischio*: basso/medio.
- *Test dopo*: suite completa.

# Pattern da applicare

- **Policy-based Authorization (già presente, da sfruttare)**.
  - *Problema*: guardie duplicate nelle action. *Dove*: attributi di controller + `FamilyContextRequirement`. *Perché adatto*: la sicurezza è una cross-cutting concern, appartiene alla pipeline. *Non overengineering*: usa l'infrastruttura esistente, non ne aggiunge.
- **Action Filter / Base Controller (solo per casi residui)**.
  - *Problema*: controlli non esprimibili via policy replicati. *Dove*: `Shared.Api`. *Perché adatto*: singolo punto riusabile. *Non overengineering*: introdotto solo se resta logica non coperta dalle policy.

# Anti-pattern da rimuovere

- **Guardie di sicurezza copiate in ogni action** (`ListsController`): sostituite dalla policy a livello di classe.
- **Check `IsAuthenticated` ridondanti** nei controller già protetti da `[Authorize]`: rimossi.
- **Helper di guardia duplicato per controller** (`EnsureAuthenticatedFamilyContext` locale): promosso a filtro/base condivisa.

# Strategia di test

- **Contract/security test (fondamentali)**: per gli endpoint interessati, verificare 401 (`authentication_required`) senza token, 403 (`family_required`) con token ma senza famiglia, e 200 con contesto valido — **prima e dopo** la rimozione delle guardie inline.
- **Integration test**: `FamilyAuthorizationGateTests`, `KinListApiIntegrationTests`, e i test dei controller Recipe.
- **Regression test**: suite completa per assicurare che nessun endpoint cambi status/corpo.
- **Security-focused test**: un test che, per ogni controller che richiede la famiglia, confermi l'assenza di accesso senza contesto famiglia (protezione contro l'omissione futura).
- **Scenari da coprire *prima* di iniziare**: accesso non autenticato, autenticato senza famiglia, autenticato con famiglia, per almeno un endpoint per controller.

# Rischi del refactor

- **Divergenza del corpo d'errore**: la pipeline potrebbe emettere un `ProblemDetails` leggermente diverso da quello dell'action inline — mitigazione: allineare `FamilyAuthorizationMiddlewareResultHandler`/`ApiProblemDetails` e verificare con i contract test (idealmente coordinare con [msrefactor-shared-result-error-handling.md](./msrefactor-shared-result-error-handling.md)).
- **Endpoint non coperto dalla policy**: rimuovere una guardia da un'action non decorata correttamente creerebbe una falla — mitigazione: lo Step 2 verifica gli attributi prima della rimozione (Step 3), con test di sicurezza a conferma.
- **Ordine dei controlli**: alcune action fanno il null-check del body dopo le guardie; spostare le guardie nella pipeline non deve alterare l'ordine percepito degli errori — mitigazione: coprire con test i casi "body nullo + non autenticato".

# Strategia di rollback

- Refactor puramente di presentazione: nessuna migrazione DB, nessun cambio di contratto dati.
- Ogni controller viene modificato in un commit isolato: il **revert** ripristina le guardie inline immediatamente.
- Rollout progressivo per controller/host con monitoraggio dei codici 401/403; in caso di anomalia si ripristina il singolo controller senza impatti sugli altri.

# Checklist finale

- [ ] Contract/security test su 401/403/200 verdi prima delle modifiche.
- [ ] Ogni controller che richiede la famiglia è decorato con la policy `FamilyContextRequirement` a livello di classe.
- [ ] Guardie inline `IsAuthenticated`/`HasFamilyContext` rimosse dalle action coperte dalla policy.
- [ ] Check `IsAuthenticated` ridondanti rimossi dai controller Recipe protetti da `[Authorize]`.
- [ ] Eventuali casi residui gestiti da un filtro/base condivisa in `Shared.Api`.
- [ ] Corpi d'errore 401/403 invariati (contract test).
- [ ] Test di sicurezza confermano l'assenza di accesso senza contesto famiglia.
- [ ] Suite `Kin.KinHub.Core.Test` completa verde.
- [ ] Codici 401/403 verificati in staging dopo il rollout.
