# Report — Refactor architetturale Kin.KinHub (Identity/Core, Shared Kernel, Audio, Migrations, Repository)

Data: 2026-07-09. Branch: `dev`. Metodo: ciclo writer/judge (build `dotnet build` come giudice per-step, massimo 5 fail per step prima di fermarsi — mai raggiunto: il fail count massimo osservato in uno step è stato 4).

## Riepilogo modifiche per step

### Step 1 — Rimozione cartelle orfane
Eliminate `src/Presentations/Kin.KinHub.{Core,KinList,KinRecipe}.Api`: nessun `.csproj`, non referenziate in `.slnx`, retirate in `App.Functions` da un commit precedente a questa sessione. Nessun contenuto tracciato da git è andato perso (verificato con `git ls-files` prima della cancellazione).

### Step 2 — Shared Kernel: eccezioni e result
- Eliminata `SharedDomainValidationException` (`Common/DomainValidationException.cs`): classe morta, zero riferimenti nel resto della codebase.
- `SharedDomainException` spostata da `Common/DomainException.cs` a `Exceptions/DomainException.cs` (nome file allineato al nome classe).
- Tutte le eccezioni concrete (`DomainValidationException`, `DuplicateEntityException`, `EntityNotFoundException`) consolidate in `Exceptions/`.
- Create le cartelle `Results/` (`IResult`, `IResultOfT`, `Result<T>`) e `Enums/` (`ResultStatus`).
- `IResult`/`IResult<T>` verificate: zero implementatori reali in tutta la solution → lasciate come contratti pubblici del kernel (non eliminate, ma segnalate come vestigiali nel rischio residuo).
- Cartella `Common/` eliminata (svuotata).
- Circa 25 file con `global using`/`using` aggiornati in tutta la solution.

### Step 3 — Astrazioni generiche nel Shared Kernel
- `IActivable`, `IAuditable`, `IEntity<T>`, `ISoftDeletable`, `IRepository<TModel,TKey>` spostate da `Core.Domain`/`Identity.Domain` (duplicate byte-quasi-identiche) a `Shared.Kernel/Interfaces/`.
- `BaseEntity<T>`, `BaseActivableEntity<T>`, `BaseDeletableEntity<T>`, `BaseEmbeddingEntity<T>` spostate a `Shared.Kernel/Models/` (erano duplicate in **entrambi** Core.Domain e Identity.Domain, non solo Identity come inizialmente ipotizzato in fase di pianificazione — verificato leggendo il codice reale prima di agire).
- `IRepository<T,K>` riconciliata sul contratto superset di Core (`CreateRangeAsync`+`GetAllAsync` inclusi); la base class `PostgreSqlRepository` di Identity e 4 repository in-memory nei test sono stati aggiornati con i metodi mancanti per continuare a soddisfare l'interfaccia.
- `ICurrentUser` è rimasta l'unica cosa in `Identity.Domain/Common/` (non generica, specifica di Identity).

### Step 4 — Repository standardizzati
- `PostgreSqlOptions` e `PostgreSqlRepository<TEntity,TDomain,TKey>` consolidate in `Shared.Kernel/Options/` e `Shared.Kernel/Repositories/` (nuovo riferimento a `Microsoft.EntityFrameworkCore`/`Microsoft.EntityFrameworkCore.Relational`/`Mapster` in `Shared.Kernel.csproj`).
- `Core.PostgreSql`, `Identity.PostgreSql`, `KinRecipe.PostgreSql` ora ereditano dalla base condivisa.
- `KinRecipe.PostgreSql` ha perso il project reference a `Core.PostgreSql` (accoppiamento cross-dominio evitabile) e la registrazione DI di `IFamilyRepository`/`FamilyRepository` di Core, diventata responsabilità di `App.Functions` dopo lo Step 5.
- **Eccezione intenzionale**: tutti e 4 i repository di `KinList.PostgreSql` (`KinListRepository`, `KinListItemRepository`, `AudioProcessingOperationRepository`, `IdempotencyRecordRepository`) sono stati **lasciati bespoke**, non forzati sulla base condivisa. Motivo verificato leggendo il codice: usano mapping manuale (non Mapster), `GetByIdAsync` nullable invece di throw-on-missing, `UpdateAsync(model)` con id incorporato invece di `UpdateAsync(key, model)`, nessun hard delete (soft-delete via update), più un `UPDATE ... WHERE Status = Queued` per claim atomico. Adattarli alla base comune avrebbe richiesto cambi di comportamento (nullable→throw) non giustificati dal refactor.
- Effetto collaterale positivo: rimuovendo la registrazione errata di `IFamilyRepository` da `KinRecipe.PostgreSql`, si è anche chiuso un gap di DI pre-esistente in cui `App.Functions` non aveva mai `CoreDbContext` registrato correttamente (risolto strutturalmente allo Step 5).
- Fix collaterale necessario: due pacchetti centrali (`Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`) sono stati alzati a `10.0.4` in `Directory.Packages.props`, perché `Microsoft.EntityFrameworkCore` 10.0.4 (ora transitivamente presente ovunque tramite `Shared.Kernel`) li richiede a quella versione minima.

### Step 5 — Separazione Identity/Core (lo step più grande)
**Violazione trovata**: `Identity.Api` ospitava in-process `FamilyController`/`ServicesController`/`AccessController` (route `api/families`, `api/services`, `api/access/family-context`), usando direttamente `Core.Business`/`Core.PostgreSql`. Il middleware JWT risolveva il family-context per ogni richiesta autenticata chiamando `Core.Business` in-process. Al contrario, `App.Functions` (che ospita KinList/KinRecipe) non aveva mai `CoreDbContext` cablato: per l'ownership famiglia chiamava Identity.Api **via HTTP** (`RemoteFamilyOwnershipService`/`RemoteFamilyContextResolver`) — l'esatto opposto della direzione di dipendenza desiderata.

**Interventi**:
- `FamilyController`→`FamilyFunctions`, `ServicesController`→`ServicesFunctions` migrati in `App.Functions/FamilyFeature` come Azure Function HTTP-trigger (stesso pattern di `RecipeBookFunctions`), stessi 5 validator FluentValidation portati 1:1.
- `App.Functions` ora registra `AddKinHubCorePostgreSqlInfrastructure()` + `AddKinHubFamilyBusiness()` realmente (prima solo `AddKinHubCoreBusiness()` con `NoOpCoreTransactionExecutor`, dato che non c'era mai stato un `CoreDbContext` reale). Aggiunto project reference a `Core.PostgreSql`.
- Nuovo `CoreFamilyContextResolver` (in-process, adattato dal vecchio `IdentityFamilyContextResolver` di Identity.Api) sostituisce sia `RemoteFamilyOwnershipService` sia `RemoteFamilyContextResolver` (entrambi eliminati).
- `Identity.Api`: rimossi interamente `FamilyFeature/`, `AccessFeature/`, `IdentityFamilyContextResolver`, tutta `Common/Authorization/` (6 file: `IFamilyContextResolver`, `FamilyContextRequirement`, `FamilyContextAuthorizationHandler`, `FamilyAuthorizationMiddlewareResultHandler`, `FamilyContextOutcome`, `FamilyContextResolution`). `JwtAuthenticationMiddleware` conservato ma sfrondato: fa solo `PopulateCurrentUserFromPrincipal`, non risolve più il family-context (verificato che nient'altro in Identity.Api dipendesse da quella risoluzione). Rimossi i project reference a `Core.Business`/`Core.Domain`/`Core.PostgreSql`.
- `FamilyContextApiOptions` (config per l'ex chiamata HTTP) eliminata; relativa configurazione IaC (`FamilyContextApi__BaseUrl`/`TimeoutSeconds`, parametro `familyContextApiTimeoutSeconds`) rimossa da `main.bicep`, `modules/compute.bicep`, `main.sample.bicepparam`.
- **Frontend**: `KinRecipe.React` aveva già un client `apiClient` verso App.Functions — `ServicesProvider.tsx` è stato ripuntato da `identityApiClient` a `apiClient`. `Core.React` e `Identity.React` avevano un solo client (verso Identity.Api): è stato aggiunto un secondo client `kinHubApiClient` (verso `VITE_KINHUB_API_URL`) **senza toccare gli altri 8 file per frontend** che già usano `apiClient` per altre chiamate verso Identity.Api — scelta deliberata per limitare il rischio, dato che non è stato possibile verificare i frontend con un browser/test runner in questa sessione.
- **IaC**: verificato (non serviva modificare) che il CORS del Function App in `compute.bicep` includesse già tutti e 4 gli origin frontend — nessuna modifica necessaria lì.
- Test: eliminati 2 file di test che esercitavano gli ex resolver HTTP (`FamilyContextResolverTests.cs`, `RemoteFamilyOwnershipServiceTests.cs`), aggiunto `CoreFamilyContextResolverTests.cs` per il nuovo resolver in-process, riscritto `AppFunctionsFamilyContextResolverRegistrationTests.cs` per asserire la nuova risoluzione (`CoreFamilyContextResolver` invece di `RemoteFamilyOwnershipService`).

### Step 6 — Refactor audio queue processing, rimozione AudioWorker
**Verificato prima di agire**: `AudioProcessingWorkerService`/il progetto `AudioWorker` (Dockerfile, Program.cs) non erano deployati in produzione (nessuna build/push CI, nessuna risorsa bicep dedicata — un commento nel bicep conferma che l'identity è stata consolidata in App.Functions). L'unica cosa viva era `AudioQueueMessageProcessor`, riusata dalla vera Azure Function tramite un project reference Presentation→Presentation innaturale.

- `AudioQueueMessageProcessor` → rinominata `AudioProcessingQueueConsumer`, spostata in `KinList.Business/KinListFeature/Services/`, dietro una nuova interfaccia `IAudioProcessingQueueConsumer`.
- `AudioQueueMessageDisposition` spostato in `KinList.Business/KinListFeature/Enums/`.
- `IAudioProcessingQueue` rinominata `IAudioProcessingQueuePublisher` (e l'implementazione `AzureQueueAudioProcessingQueue` → `AzureQueueAudioProcessingQueuePublisher`), per rendere esplicita e simmetrica la coppia publisher/consumer.
- Nuovo `AudioQueueMessageSerializer` centralizza le `JsonSerializerOptions` prima duplicate identicamente nel publisher e nel consumer.
- `AudioQueueFunctions` (la vera Function con `[QueueTrigger]`) ora dipende da `IAudioProcessingQueueConsumer`, non dalla classe concreta.
- Progetto `Kin.KinHub.KinList.AudioWorker` **eliminato interamente** (cartella, `.csproj`, `Dockerfile`, entry `.slnx`, project reference da `App.Functions` e `Core.Test`).
- Test: `KinListAudioWorkerTests.cs` riscritto per testare `AudioProcessingQueueConsumer` direttamente (disposition restituita, non più "messaggio cancellato dalla coda" — quella era responsabilità del worker standalone eliminato, non della Function reale che delega la cancellazione al runtime Azure Functions). I test specifici del polling loop di `AudioProcessingWorkerService` e un test di idempotenza legato al fallimento della `DeleteMessageAsync` manuale sono stati **eliminati** (testavano un comportamento — cancellazione manuale del messaggio — che non esiste più: né la Function reale né il nuovo consumer chiamano `DeleteMessageAsync`, se ne occupa il runtime Azure Functions).

### Step 7 — Rimozione Migrations Runner
**Verificato prima di agire**: `scripts/verify-migration-iac.sh`, richiamato da `.github/workflows/backend.yml`, asseriva staticamente che nessun `MigrateAsync` esistesse fuori dal runner, e che `deploy-backend.yml` eseguisse il runner prima dello zip-deploy. Rimuovere il progetto senza aggiornare questo contratto avrebbe rotto la CI silenziosamente.

- Nuovo `DbContextMigrationExtensions.ApplyPendingMigrationsAsync<TContext>` in `Shared.Kernel/Extensions/` (logging strutturato di nome contesto/durata/esito, stesso comportamento di `MigrationRunnerService` ma riusabile per singolo `DbContext`).
- `Identity.Api/Program.cs`: applica `IdentityDbContext` allo startup, dietro il flag `RunMigrationsOnStartup` (default `true`).
- `App.Functions/Program.cs`: applica `CoreDbContext`, `KinListDbContext`, `KinRecipeDbContext` allo startup, stesso flag.
- Progetto `Kin.KinHub.Migrations.Runner` eliminato (cartella, entry `.slnx`, project reference da `Core.Test`, `MigrationRunnerServiceTests.cs`).
- `scripts/verify-migration-iac.sh` aggiornato **nello stesso passaggio**: il nuovo invariante verificato è "nessun `MigrateAsync` fuori da `Identity.Api/Program.cs` e `App.Functions/Program.cs`", più la presenza del flag `RunMigrationsOnStartup` e della chiamata `ApplyPendingMigrationsAsync` in entrambi. Rimossi i controlli sull'ordine del vecchio step CI (non più applicabile) e il check ormai stale su `FamilyContextApi__BaseUrl` (Step 5). Gli altri controlli del file (redirect KinRecipe→KinList, wiring Key Vault, rimozione `KINLIST_CORE_API_BASE_URL`) **non sono stati toccati**, sono fuori scope. **Lo script è stato effettivamente eseguito e passa.**
- `.github/workflows/deploy-backend.yml`: rimossi i 3 step "Open/Close temporary Postgres firewall rule" + "Run database migrations" (il runner dell'agente CI non ha più bisogno di accesso diretto a Postgres, dato che le migration ora avvengono dentro l'app all'avvio, nella rete privata Azure).

### Step 8 — Riorganizzazione cartelle e namespace
Riorganizzazione sistematica applicata a Business, Infrastructure (nessuna interfaccia/enum fuori posto trovata) e Presentation di tutti i domini, dopo un inventario esaustivo via agente di ricerca:
- ~68 file interfaccia spostati in cartelle `Interfaces/` dedicate per feature (o `Common/Interfaces/`, `Common/Authorization/Interfaces/`, `Common/Validators/Interfaces/` dove il raggruppamento esistente meritava di essere preservato).
- 6 file in `Identity.Business` (`UpdateUserPasswordHandler.cs` e affini) avevano interfaccia e classe concreta nello stesso file: l'interfaccia è stata estratta in un file dedicato in `Interfaces/`, la classe è rimasta al suo posto.
- 2 enum di `KinList.Domain` (`AudioProcessingOperationStatus`, `AudioProcessingOperationType`) spostati da `Models/` a `Enums/`.
- **Confermato empiricamente**: in questa codebase il namespace non dipende dal percorso file (le cartelle `Interfaces/`/`Enums/` esistenti prima del refactor usano lo stesso namespace del genitore, non un segmento aggiuntivo) — quindi lo spostamento fisico non ha richiesto **nessuna** modifica di `using`/`global using`, salvo i 6 file splittati. La build è passata al primo tentativo per questo step.

### Step 9 — Validazione finale
Build completa della solution: **0 errori**. Nessun riferimento pendente a progetti rimossi (`AudioWorker`, `Migrations.Runner`, `*.Api` orfani) in `.csproj`/`.slnx`. Bicep compila (`az bicep build`). **La suite di test (`dotnet test`) non è stata eseguita in questa sessione**, su richiesta esplicita dell'utente ("non eseguire test, richiede troppo tempo per conoscere il verdetto") — il giudizio per-step si è basato solo su `dotnet build`.

## Comandi di validazione eseguiti

- `dotnet build Kin.KinHub.Core.slnx` — rieseguito ad ogni step (giudice del ciclo writer/judge), esito finale: **Build succeeded, 0 Error(s)**.
- `az bicep build --file ops/iac/main.bicep --stdout` — OK.
- `bash scripts/verify-migration-iac.sh` — **Migration / IaC contract checks passed.**
- `git status` — nessun file inatteso, nessuna cartella orfana in `src/`.

## Rischi residui

- **Test non eseguiti**: `dotnet test` non è stato lanciato in questa sessione. Il codice compila e i test sono stati aggiornati/riscritti in modo coerente con i cambi, ma non c'è conferma runtime che tutti passino. **Raccomandazione: eseguire `dotnet test` prima di mergiare.**
- **Frontend non verificati a runtime**: le modifiche a `Core.React`/`Identity.React`/`KinRecipe.React` (nuovo client `kinHubApiClient`/ripuntamento di `apiClient`) non sono state testate in un browser. Raccomandazione: avviare i 3 frontend in locale e verificare il flusso famiglia/servizi end-to-end contro `App.Functions` prima del deploy.
- **`IResult`/`IResult<T>` in Shared.Kernel**: confermate senza implementatori reali in tutta la solution. Lasciate come contratti pubblici (potrebbero essere usate da consumer esterni non presenti in questa solution); se risultano davvero morte, sono candidate a eliminazione in un futuro passaggio.
- **Migrazione allo startup su Azure Functions in scala**: se il Function App scala a più istanze concorrenti, ogni istanza tenterà `MigrateAsync` all'avvio. EF Core/Npgsql gestiscono normalmente questa concorrenza tramite lock sulla tabella delle migration history, ma non è stato possibile validarlo in un ambiente realmente concorrente in questa sessione.
- **CORS per i nuovi client frontend**: verificato che `compute.bicep` includa già gli origin di tutti e 4 i frontend nel CORS del Function App, ma non è stata fatta una chiamata reale dal browser per confermarlo a runtime.

## Punti lasciati intenzionalmente invariati (e perché)

- **`RecipeAssistantIntegrationException`** (`KinRecipe.Domain`) non deriva da `SharedDomainException`: rappresenta un fallimento di integrazione esterna (provider AI), categoria semantica diversa da una violazione di invariante di dominio. Non è un duplicato dell'eccezione condivisa.
- **Entità di `Core.Domain`** (`Family`, `FamilyMember`, ecc.) continuano a implementare le interfacce (`IEntity<T>`, `IAuditable`, …) tramite le stesse `Base*Entity<T>` ora in `Shared.Kernel` — nessun cambio di comportamento richiesto qui, solo la posizione delle classi base è cambiata.
- **`KinList.PostgreSql`**: i suoi 4 repository restano bespoke (vedi Step 4) — cambiarli avrebbe richiesto modifiche di comportamento (nullable-vs-throw, hard-vs-soft delete) fuori scope per un refactor "senza cambiare comportamento business".
- **`KinRecipe.Business` → `Core.Business`/`Core.Domain` (project reference)**: KinRecipe dipende da Core per motivi di business legittimi (accesso famiglia per il controllo di proprietà su ricette/frigorifero). Non è lo stesso tipo di violazione di Identity→Core (che era un accoppiamento presentation-layer evitabile); non è stato toccato perché fuori dallo scope esplicito della richiesta (solo Identity/Core).
- **Documentazione (`docs/specs/*.md`)**: `flusso-autenticazione.md`, `flusso-gestione-famiglie.md` e `panoramica-kinhub.md` sono stati aggiornati per le parti toccate da **questo** refactor (routing Family/Services, rimozione AudioWorker/Migrations Runner, direzione delle dipendenze Identity/Core). Questi documenti però contenevano **già prima di questa sessione** riferimenti stale a un progetto `Shared.Api` e a host separati `KinRecipe.Api`/`KinList.Api` (consolidati in `App.Functions` da un commit precedente a questa sessione) — quella staleness pre-esistente **non è stata corretta integralmente**, servirebbe un nuovo passaggio di refresh completo (come quello già fatto il 2026-07-05, citato nella memoria di progetto) per riallineare tutta la documentazione allo stato attuale, non solo le parti toccate qui.
