> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Garantire che le **creazioni multi-entità del contesto Core** (famiglia con membri e servizi; ricetta con ingredienti e passi) siano **atomiche**: o vengono scritte tutte le righe, o nessuna. Oggi queste operazioni eseguono più `SaveChanges` separati senza transazione, quindi un errore intermedio può lasciare dati parzialmente inseriti. L'obiettivo è portare il contesto Core allo stesso livello di consistenza già presente nel contesto KinList (`EfKinListTransactionExecutor`), eliminando al contempo le scritture in loop (N+1) dove possibile.

Problema risolto: **inconsistenza e possibile "perdita logica" di dati** (entità aggregate incomplete) e round-trip eccessivi verso il database.

# Stato attuale

Il contesto Core usa un repository base generico `PostgreSqlRepository<TEntity, TDomain, TKey>` (`src/Infrastructures/Kin.KinHub.Core.PostgreSql/Common/PostgreSqlRepository.cs`) in cui **ogni** `CreateAsync`/`UpdateAsync`/`DeleteAsync` chiama internamente `Context.SaveChangesAsync()`. Non esiste un'astrazione di Unit of Work né un executor di transazioni nel contesto Core.

Le creazioni aggregate sono negli handler applicativi:

- `CreateFamilyHandler.HandleAsync` (`src/Businesses/Kin.KinHub.Core.Business/FamilyFeature/Commands/CreateFamily/CreateFamilyHandler.cs`):
  1. `FindByUserIdAsync` per verificare che l'utente non abbia già una famiglia;
  2. `_familyRepository.CreateAsync(family)`;
  3. `_familyMemberRepository.CreateAsync(ownerMember)`;
  4. `foreach` sui membri aggiuntivi → un `CreateAsync` per ciascuno;
  5. `_kinHubServiceRepository.GetAllAsync()` e poi `foreach` sui servizi → un `CreateAsync` (`FamilyService`) per ciascuno.
  Ogni `CreateAsync` è una `INSERT` + `SaveChanges` distinta; non c'è transazione che avvolga i passi 2–5.

- `CreateRecipeHandler.HandleAsync` (`src/Businesses/Kin.KinHub.Core.Business/RecipeFeature/Commands/CreateRecipe/CreateRecipeHandler.cs`):
  1. `_recipeBookAccessService.GetAccessibleRecipeBookAsync(...)` (controllo accesso);
  2. `_recipeRepository.AddAsync(recipe)`;
  3. `foreach` ingredienti → `_recipeIngredientRepository.AddAsync(...)`;
  4. `foreach` passi → `_recipeStepRepository.AddAsync(...)`;
  5. mapping in `RecipeResponse`.
  Anche qui nessuna transazione unica.

Riferimento positivo già presente nel repo: il contesto KinList incapsula l'atomicità in `IKinListTransactionExecutor` con implementazione `EfKinListTransactionExecutor` (`src/Infrastructures/Kin.KinHub.KinList.PostgreSql/Common/EfKinListTransactionExecutor.cs`), che usa `Database.CreateExecutionStrategy()` + `BeginTransactionAsync`/`CommitAsync`. Il fallback fuori dall'infrastruttura è `NoOpKinListTransactionExecutor` (registrato con `TryAddScoped`), che esegue senza transazione: nei tre host reali la registrazione EF avviene per prima (`AddKinHubKinListPostgreSqlInfrastructure` prima di `AddKinHubKinListBusiness`), quindi in produzione viene usato l'executor EF; il NoOp resta però un default silenzioso e fragile rispetto all'ordine di registrazione.

# Problemi individuati

- **Rischio di perdita dati / inconsistenza (rischio architetturale + perdita dati)**: in `CreateFamilyHandler` un'eccezione dopo il passo 2 lascia una famiglia senza (tutti i) servizi o senza membri; in `CreateRecipeHandler` una ricetta può restare senza ingredienti/passi. Sono aggregati che dovrebbero essere consistenti per definizione.
- **N+1 in scrittura (rischio di scalabilità/performance)**: N `INSERT` con N round-trip per membri, servizi, ingredienti, passi. Con cataloghi/ricette più grandi il costo cresce linearmente.
- **Astrazione di persistenza che nasconde il confine transazionale (debito tecnico)**: `PostgreSqlRepository` fa `SaveChanges` per ogni chiamata, impedendo di comporre più scritture in un'unica unità di lavoro dal Business.
- **Incoerenza tra contesti (debito tecnico)**: KinList è transazionale, Core no; due standard diversi nello stesso repository confondono chi lavora su più moduli.
- **Default silenzioso `NoOpKinListTransactionExecutor` (rischio di regressione)**: non è un bug attivo (l'ordine di registrazione lo evita), ma è un *foot-gun*: un host futuro che registri il Business prima dell'infrastruttura eseguirebbe le mutazioni KinList **senza** transazione senza alcun errore.
- **Non è un bug reale ma va chiarito**: il controllo "una famiglia per utente" in `CreateFamilyHandler` si basa su `FindByUserIdAsync` + eventuale `DuplicateEntityException`; senza un vincolo transazionale/unico esplicito resta una piccola finestra di race in caso di richieste concorrenti dello stesso utente.

# Come Microsoft farebbe il refactor

Approccio pragmatico, incrementale e backward-compatible, allineato alle linee guida EF Core (transazioni e `IExecutionStrategy`) e ai principi di aggregato del DDD (un aggregato = un confine di consistenza = una transazione):

1. **Riusare il pattern già esistente** invece di inventarne uno nuovo: introdurre nel contesto Core un `ICoreTransactionExecutor` speculare a `IKinListTransactionExecutor`, con implementazione EF basata su execution strategy. Questo mantiene coerenza interna e riduce il carico cognitivo.
2. **Rendere il confine transazionale esplicito nel Business**: avvolgere l'intera creazione dell'aggregato in `ExecuteAsync(...)`, così l'atomicità è una decisione applicativa visibile, non un effetto collaterale del repository.
3. **Introdurre inserimenti batch**: aggiungere metodi `AddRange`/creazione in blocco ai repository di collezione (membri, servizi, ingredienti, passi) per eliminare l'N+1, con un solo `SaveChanges` dentro la transazione.
4. **Backward compatibility**: le firme pubbliche dei service facciata (`IFamilyService`, `IRecipeService`) e i contratti API restano invariati; cambia solo l'implementazione interna degli handler.
5. **Test prima delle modifiche rischiose**: aggiungere test che verifichino il rollback (nessuna riga scritta in caso di errore) prima di introdurre la transazione.
6. **Rendere sicuro il default KinList**: in produzione fallire *fast* (o loggare un warning esplicito) se viene risolto `NoOpKinListTransactionExecutor`, così un'errata composizione DI è immediatamente evidente.
7. **Deploy progressivo e rollback**: cambiamento isolato per handler, dietro il normale ciclo di rilascio; rollback = revert del singolo handler.

# Piano operativo

**Step 1 — Test di caratterizzazione (rete di sicurezza).**
- *Cosa*: aggiungere test che (a) verificano la creazione completa "happy path" e (b) simulano un errore a metà creazione e asseriscono che **nessuna** riga sia stata persistita.
- *Dove*: `src/Tests/Kin.KinHub.Core.Test` (nuovi test per Family e Recipe, sul modello di `KinListServiceTests`).
- *Perché*: catturare il comportamento attuale e proteggere dal rischio di regressione.
- *Impatto previsto*: nessuno sul runtime; solo copertura.
- *Rischio dello step*: basso.
- *Test dopo lo step*: esecuzione dell'intera suite `Kin.KinHub.Core.Test`.

**Step 2 — Introdurre `ICoreTransactionExecutor`.**
- *Cosa*: definire l'interfaccia in `Core.Business/Common` e l'implementazione EF in `Core.PostgreSql/Common` (copia adattata di `EfKinListTransactionExecutor`), registrandola nell'infrastruttura Core.
- *Dove*: `Kin.KinHub.Core.Business`, `Kin.KinHub.Core.PostgreSql`, `ServiceCollectionExtensions` dei rispettivi progetti.
- *Perché*: fornire il confine transazionale componibile.
- *Impatto previsto*: nessun cambiamento comportamentale finché non viene usato.
- *Rischio dello step*: basso.
- *Test dopo lo step*: build + suite esistente (nessuna regressione).

**Step 3 — Avvolgere `CreateFamilyHandler` nella transazione + batch.**
- *Cosa*: eseguire i passi 2–5 dentro `ExecuteAsync`; introdurre `CreateRangeAsync` per `FamilyMember` e `FamilyService`.
- *Dove*: `CreateFamilyHandler.cs` + repository famiglia/servizi in `Core.PostgreSql/FamilyFeature`.
- *Perché*: atomicità + eliminazione N+1.
- *Impatto previsto*: comportamento esterno invariato in caso di successo; in caso di errore, rollback completo (miglioramento).
- *Rischio dello step*: medio (tocca un flusso di creazione core).
- *Test dopo lo step*: test di rollback dello Step 1 + integrazione famiglia.

**Step 4 — Avvolgere `CreateRecipeHandler` nella transazione + batch.**
- *Cosa/Dove/Perché*: analogo allo Step 3 su ricetta + ingredienti + passi.
- *Impatto previsto/Rischio*: medio; comportamento esterno invariato in successo.
- *Test dopo lo step*: test di rollback ricetta + integrazione ricette.

**Step 5 — Irrobustire il default transazionale KinList.**
- *Cosa*: in `AddKinHubKinListApi`/worker, verificare che l'executor risolto non sia il NoOp in ambiente non-Development (fail-fast o log di warning esplicito).
- *Dove*: `ServiceCollectionExtensions` di `KinList.Api` e `Program.cs` del worker.
- *Perché*: eliminare il foot-gun del default silenzioso.
- *Impatto previsto*: nessuno nei setup corretti; errore chiaro nei setup errati.
- *Rischio dello step*: basso.
- *Test dopo lo step*: test di composizione DI (host si avvia con l'executor EF).

**Step 6 — Valutare un vincolo di unicità per "una famiglia per utente".**
- *Cosa*: aggiungere (se non presente) un indice unico su `Family.UserId` via migrazione, e mappare la violazione a `Conflict`.
- *Dove*: `Core.PostgreSql/Migrations` + `CreateFamilyHandler` (gestione dell'eccezione di unicità).
- *Perché*: chiudere la finestra di race a livello di database.
- *Impatto previsto*: irrilevante a regime; protegge da doppie creazioni concorrenti.
- *Rischio dello step*: medio (migrazione DB); vedi rischi/rollback.
- *Test dopo lo step*: test di creazione concorrente + `ExpandContractMigrationTests`.

# Pattern da applicare

- **Unit of Work / Transaction Script esplicito** (`ICoreTransactionExecutor`).
  - *Problema*: comporre più scritture come un'unica unità atomica.
  - *Dove*: handler di creazione aggregata del Core.
  - *Perché adatto*: replica un pattern già validato in KinList; confine di consistenza esplicito.
  - *Perché non è overengineering*: non introduce un framework, solo un'interfaccia sottile con una singola implementazione EF, coerente col resto del codebase.

- **Aggregate boundary (DDD)**: trattare `Family` (con membri e servizi iniziali) e `Recipe` (con ingredienti e passi) come aggregati creati atomicamente.
  - *Perché non è overengineering*: non richiede refactor del modello, solo il rispetto del confine in fase di scrittura.

- **Batch insert** (`AddRange` + un `SaveChanges`).
  - *Problema*: N+1 in scrittura. *Dove*: repository di collezione. *Perché adatto*: riduce round-trip senza cambiare semantica.

# Anti-pattern da rimuovere

- **Scritture aggregate non transazionali** in `CreateFamilyHandler`/`CreateRecipeHandler`: sostituite dall'esecuzione dentro l'executor transazionale.
- **N+1 in scrittura** (loop di `CreateAsync`/`AddAsync`): sostituito da inserimenti batch.
- **Default silenzioso pericoloso** (`NoOpKinListTransactionExecutor` risolto inavvertitamente in produzione): reso esplicito/fail-fast.
- **`SaveChanges` implicito per singola chiamata** che nasconde il confine transazionale al Business: mitigato consentendo la composizione dentro la transazione (senza necessariamente riscrivere l'intero repository base in questa fase).

# Strategia di test

- **Unit test (Business)**: mock dei repository/executor per verificare che gli handler invochino le scritture dentro `ExecuteAsync` e che, in caso di eccezione simulata, non venga effettuato il commit.
- **Integration test (con DB reale/di test)**: creazione famiglia/ricetta "happy path" (tutte le righe presenti) e "failure path" (nessuna riga presente dopo un errore forzato). Riutilizzare l'infrastruttura di `Kin.KinHub.Core.Test`.
- **Regression test**: eseguire l'intera suite esistente (`FamilyAuthorizationGateTests`, test ricette, `ServiceCharacterizationTests`) per assicurare invarianza del comportamento esterno.
- **Concurrency test**: due creazioni famiglia concorrenti per lo stesso utente → una sola famiglia (dopo Step 6).
- **Migration test**: `ExpandContractMigrationTests` per l'eventuale indice unico.
- **Scenari da coprire *prima* di iniziare**: happy path di creazione famiglia e ricetta, e almeno un percorso d'errore intermedio.

# Rischi del refactor

- **Cambiamento del comportamento in caso d'errore**: prima potevano restare righe parziali, ora ci sarà rollback totale. È il comportamento desiderato, ma eventuali client/processi che si basavano su stati parziali vanno verificati (improbabile ma da controllare).
- **Execution strategy + transazioni utente**: con Npgsql/EF la combinazione di retry strategy e transazioni esplicite richiede di eseguire tutta l'operazione dentro `ExecuteAsync` (già fatto così in KinList) — mitigazione: seguire esattamente il pattern esistente.
- **Migrazione con indice unico (Step 6)**: se esistono già dati con `UserId` duplicati la migrazione fallisce — mitigazione: script di verifica/bonifica prima di applicare l'indice, migrazione "expand/contract".
- **Regressioni sui percorsi di creazione**: mitigate dai test dello Step 1 introdotti prima delle modifiche.

# Strategia di rollback

- Ogni step è un commit isolato e reversibile: il **revert** dell'handler ripristina il comportamento precedente senza migrazioni pendenti (Step 3–5).
- Per lo **Step 6 (migrazione)**: prevedere una *down migration* che rimuova l'indice unico; poiché è additivo, il rollback del codice applicativo non richiede il rollback dello schema. Rilascio progressivo (prima l'indice, monitorare, poi la logica che vi si appoggia).
- Deploy progressivo per host: rilasciare prima l'API meno critica e monitorare error rate/telemetria prima di procedere.

# Checklist finale

- [ ] Test di caratterizzazione (happy path + rollback) aggiunti e verdi prima delle modifiche.
- [ ] `ICoreTransactionExecutor` + implementazione EF introdotti e registrati.
- [ ] `CreateFamilyHandler` esegue creazione famiglia/membri/servizi in un'unica transazione.
- [ ] `CreateRecipeHandler` esegue creazione ricetta/ingredienti/passi in un'unica transazione.
- [ ] Inserimenti batch al posto dei loop di `CreateAsync`/`AddAsync`.
- [ ] Default transazionale KinList reso fail-fast/loggato fuori da Development.
- [ ] (Opzionale) Indice unico su `Family.UserId` con migrazione expand/contract e gestione `Conflict`.
- [ ] Suite completa `Kin.KinHub.Core.Test` verde (unit + integration + migration).
- [ ] Nessuna modifica ai contratti pubblici delle API verificata (contract/regression test).
- [ ] Telemetria/log verificati su un ambiente di staging prima del rollout completo.
