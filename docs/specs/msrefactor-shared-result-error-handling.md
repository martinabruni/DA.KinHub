> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Uniformare e consolidare l'**infrastruttura trasversale di gestione esiti ed errori** oggi duplicata per ogni modulo: i tipi `Result<T>`/`ResultStatus`, i mapper HTTP (`HttpResultMapper`, `IdentityHttpResultMapper`, `KinListHttpResultMapper`), le eccezioni di dominio e i mattoni di persistenza base (`PostgreSqlOptions`, `PostgreSqlRepository`). L'obiettivo è avere **un contratto d'errore coerente** su tutti gli host (stesso `ProblemDetails`, stessi codici, stessi status) e ridurre l'onere manutentivo, correggendo al contempo una mappatura errata (`DomainException` → 500 in registrazione) che nasconde errori di validazione.

Problema risolto: **divergenza già in atto** del contratto d'errore tra i tre contesti e duplicazione che moltiplica il costo di ogni correzione.

# Stato attuale

Ci sono **tre implementazioni separate** di `Result<T>`/`ResultStatus`, una per contesto:

- `src/Businesses/Kin.KinHub.Core.Business/Common/Result.cs` e `ResultStatus.cs`
- `src/Businesses/Kin.KinHub.Identity.Business/Common/Result.cs` e `ResultStatus.cs`
- `src/Businesses/Kin.KinHub.KinList.Business/Common/Result.cs` e `ResultStatus.cs`

Sono **già divergenti**: il `Result<T>` di KinList espone una proprietà `Code` e uno stato `UnprocessableEntity` con factory che accettano un `code` (es. `ValidationError(string message, string code = "validation_error")`), mentre le versioni di Core e Identity non hanno né `Code` né `UnprocessableEntity`.

Ci sono **tre mapper HTTP**:

- `src/Presentations/Kin.KinHub.Shared.Api/Common/HttpResultMapper.cs` (usa `Kin.KinHub.Core.Business.Common.Result`)
- `src/Presentations/Kin.KinHub.Shared.Api/Common/IdentityHttpResultMapper.cs`
- `src/Presentations/Kin.KinHub.KinList.Api/Common/KinListHttpResultMapper.cs`

Tutti convergono verso `ProblemDetails` (via `ApiProblemDetails` in `Shared.Api`), ma con logiche di switch separate e potenzialmente disallineate (es. la gestione di `UnprocessableEntity` esiste solo lato KinList).

Le **eccezioni di dominio** sono duplicate:

- `src/Domains/Kin.KinHub.Core.Domain/Common/Exceptions/*` (`DomainException`, `DomainValidationException`, `DuplicateEntityException`, `EntityNotFoundException`)
- `src/Domains/Kin.KinHub.Identity.Domain/Common/Exceptions/*` (stesse classi)
- (KinList.Domain non ha lo stesso set completo)

Anche i mattoni di persistenza sono duplicati: `PostgreSqlOptions` e `PostgreSqlRepository` esistono in `Core.PostgreSql`, `Identity.PostgreSql` e (per Options) `KinList.PostgreSql`.

Infine, in `src/Businesses/Kin.KinHub.Identity.Business/AuthenticationFeature/Commands/Register/RegisterUserHandler.cs` il `catch (DomainException)` mappa a `Result.UnexpectedError(...)` → HTTP 500, anche quando la causa è una `DomainValidationException` (es. password non valida), che dovrebbe essere un 400.

# Problemi individuati

- **Divergenza del contratto d'errore (rischio architetturale + regressione)**: tre `Result` diversi e tre mapper significano che lo stesso concetto d'errore può produrre risposte HTTP diverse tra host; l'assenza di `UnprocessableEntity`/`Code` fuori da KinList impedisce ai client di trattare gli errori in modo uniforme.
- **Duplicazione ad alto costo (debito tecnico)**: correggere il mapping o aggiungere uno stato richiede modifiche in più punti, con rischio di dimenticanze.
- **Mappatura errata `DomainException` → 500 (bug reale)**: `RegisterUserHandler` restituisce un errore server (500) per una condizione di **validazione** (400), degradando UX e diagnosticabilità e potenzialmente inquinando gli alert su errori 5xx.
- **Eccezioni di dominio duplicate (debito tecnico)**: due gerarchie identiche complicano un handling uniforme e la condivisione di logica.
- **Persistenza base duplicata (debito tecnico)**: `PostgreSqlRepository`/`PostgreSqlOptions` replicati; una modifica al comportamento base (es. concorrenza, mapping) va propagata manualmente.
- **Barriera per junior (manutenibilità)**: non è ovvio quale `Result`/mapper usare quando si lavora su più moduli.

# Come Microsoft farebbe il refactor

Consolidamento **incrementale e backward-compatible**, evitando un "big bang". Principio guida: un unico *kernel* condiviso per gli esiti e per la traduzione HTTP, riducendo la duplicazione ma senza forzare astrazioni inutili tra i bounded context.

1. **Definire un `Result<T>` condiviso** (con `Status`, `Code`, `Message`, `Value`) come *superset* compatibile che includa `UnprocessableEntity` e `Code` (già richiesti da KinList). Collocarlo in un progetto/kernel condiviso referenziato dai Business (es. un `Kin.KinHub.Shared.Kernel` o l'attuale `Shared.Api` se appropriato, valutando la direzione delle dipendenze).
2. **Un solo mapper HTTP** (`ProblemDetails`-based) che copra tutti gli stati, deprecando i tre mapper attuali dietro *shim* che delegano al mapper unico (backward compatibility durante la transizione).
3. **Correggere subito il bug di mappatura** in `RegisterUserHandler`: distinguere `DomainValidationException` (→ `ValidationError`/400) da `DomainException` generica (→ `UnexpectedError`/500). Questo è indipendente dal consolidamento e va rilasciato per primo.
4. **Unificare le eccezioni di dominio** in un unico set condiviso (o mantenere per-contesto ma con gerarchia/base comune), così l'handling è uniforme.
5. **Consolidare la persistenza base** (`PostgreSqlRepository`/`PostgreSqlOptions`) in un componente condiviso solo se non rompe l'isolamento dei DbContext; se l'isolamento è un requisito, documentare la scelta e almeno unificare `PostgreSqlOptions`.
6. **Migrazione graduale, un modulo alla volta**, con test di contratto che congelano le risposte HTTP prima e dopo.
7. **Deploy progressivo + rollback** per host.

> Nota: non tutte le duplicazioni vanno necessariamente eliminate. In DDD un certo grado di duplicazione tra bounded context può essere accettabile per preservare l'autonomia. La priorità è **eliminare la divergenza del contratto d'errore pubblico** e il **bug di mappatura**, non forzare un'unificazione totale se compromette l'isolamento.

# Piano operativo

**Step 1 — Contract test sulle risposte d'errore (rete di sicurezza).**
- *Cosa*: test che fissano lo `status`/`code`/corpo `ProblemDetails` per gli errori chiave di ogni host (NotFound, Conflict, ValidationError, Unauthorized→403, ServiceUnavailable, UnprocessableEntity).
- *Dove*: `src/Tests/Kin.KinHub.Core.Test` (integrazione sui tre host).
- *Perché*: congelare il contratto prima di consolidare.
- *Impatto/Rischio*: nessuno sul runtime; basso.
- *Test dopo*: suite completa.

**Step 2 — Correggere il bug di mappatura in registrazione (indipendente, prioritario).**
- *Cosa*: in `RegisterUserHandler` mappare `DomainValidationException` → `ValidationError` (400) e solo le altre `DomainException` → `UnexpectedError` (500).
- *Dove*: `RegisterUserHandler.cs` (ed eventuali handler con lo stesso pattern).
- *Perché*: evitare 500 per errori di validazione.
- *Impatto previsto*: risposte più corrette; meno rumore sugli alert 5xx.
- *Rischio dello step*: basso.
- *Test dopo*: test di registrazione con input non valido → 400.

**Step 3 — Introdurre il `Result<T>` condiviso (superset).**
- *Cosa*: definire il tipo unificato con `Code` e `UnprocessableEntity`; adeguare i factory.
- *Dove*: nuovo kernel condiviso (o `Shared.Api` se coerente con le dipendenze).
- *Perché*: base unica per gli esiti.
- *Impatto previsto*: nessuno finché i moduli non migrano.
- *Rischio dello step*: basso.
- *Test dopo*: build + suite.

**Step 4 — Un unico mapper HTTP + shim di compatibilità.**
- *Cosa*: implementare un mapper unico su `ProblemDetails`; far delegare i tre mapper esistenti a quello unico.
- *Dove*: `Shared.Api/Common`.
- *Perché*: coerenza del contratto, riduzione duplicazione.
- *Impatto previsto*: risposte identiche (verificate dai contract test).
- *Rischio dello step*: medio.
- *Test dopo*: contract test Step 1.

**Step 5 — Migrare i moduli al `Result` condiviso, uno alla volta.**
- *Cosa*: sostituire i `Result` per-modulo con quello condiviso (Core, poi Identity, poi KinList), rimuovendo gli shim quando un host è completamente migrato.
- *Dove*: i tre Business + i controller.
- *Perché*: eliminare la divergenza.
- *Impatto/Rischio*: medio; procedere per host.
- *Test dopo*: suite per host migrato.

**Step 6 — Unificare eccezioni di dominio e `PostgreSqlOptions`.**
- *Cosa*: consolidare le eccezioni duplicate e almeno le Options di persistenza; valutare `PostgreSqlRepository` condiviso preservando l'isolamento dei DbContext.
- *Dove*: `Domains/*/Common/Exceptions`, `Infrastructures/*/Common`.
- *Perché*: ridurre duplicazione residua.
- *Rischio dello step*: medio.
- *Test dopo*: suite completa + test di persistenza.

# Pattern da applicare

- **Result/Operation Result (unificato)**.
  - *Problema*: esiti applicativi incoerenti. *Dove*: kernel condiviso. *Perché adatto*: un solo contratto tra Business e Presentation. *Non overengineering*: unifica tipi già esistenti, non ne aggiunge di nuovi.
- **Centralized error mapping (Problem Details / RFC 9457)**.
  - *Problema*: tre mapper divergenti. *Dove*: mapper HTTP unico. *Perché adatto*: risposte d'errore standard e coerenti. *Non overengineering*: `ProblemDetails` è già lo standard usato in `ApiProblemDetails`.
- **Shared Kernel (DDD)**.
  - *Problema*: duplicazione trasversale. *Dove*: `Result`, eccezioni, Options. *Perché adatto*: elementi realmente comuni ai contesti. *Non overengineering*: limitato al kernel tecnico, non al modello di dominio dei singoli contesti.

# Anti-pattern da rimuovere

- **Duplicazione dei tipi `Result`/`ResultStatus`** con divergenza: unificati.
- **Mapper HTTP multipli e disallineati**: sostituiti da uno solo (con shim temporanei).
- **Mappatura d'errore fuorviante** (`DomainException` → 500 per validazione): corretta.
- **Eccezioni di dominio e Options di persistenza duplicate**: consolidate.

# Strategia di test

- **Contract test (fondamentali)**: per ogni host, congelare `status`/`code`/campi `ProblemDetails` degli errori chiave prima del refactor e verificarli invariati dopo.
- **Unit test**: mapper unico che, dato ogni `ResultStatus` (incluso `UnprocessableEntity`), produca lo status e il codice attesi.
- **Regression test**: suite completa `Kin.KinHub.Core.Test` (include integrazione dei tre host).
- **Security-adjacent test**: verificare che gli errori di autorizzazione restino `403` (Unauthorized→403) e che gli errori di validazione non diventino 500.
- **Scenari da coprire *prima* di iniziare**: un esempio per ciascuno stato d'errore su ciascun host; il caso di registrazione con password non valida (deve diventare 400).

# Rischi del refactor

- **Cambiamenti involontari nel contratto HTTP**: il rischio principale — mitigato dai contract test dello Step 1 che devono restare verdi.
- **Direzione delle dipendenze del kernel condiviso**: introdurre un progetto condiviso non deve creare riferimenti circolari o violare la Clean Architecture — mitigazione: collocare il kernel a un livello referenziabile dai Business senza dipendere dall'infrastruttura.
- **Migrazione ampia**: toccare tutti i moduli è invasivo — mitigazione: procedere per host, con shim di compatibilità, mai tutto insieme.
- **Perdita di isolamento tra contesti**: unificare troppo (es. `PostgreSqlRepository`) può accoppiare i contesti — mitigazione: limitare il consolidamento agli elementi realmente tecnici e documentare le scelte.

# Strategia di rollback

- Gli shim di compatibilità (Step 4) permettono di tornare ai mapper originali con un revert senza toccare i controller.
- La migrazione per-host consente di fermarsi/ripristinare un singolo modulo senza impattare gli altri.
- Il fix del bug (Step 2) è isolato e reversibile con un singolo revert.
- Nessuna migrazione DB è richiesta (salvo eventuale consolidamento Options, comunque additivo); rollback = revert dei commit interessati. Deploy progressivo per host con monitoraggio del rapporto 4xx/5xx.

# Checklist finale

- [ ] Contract test sugli errori dei tre host verdi prima delle modifiche.
- [ ] Bug `RegisterUserHandler` corretto: validazione → 400, non 500.
- [ ] `Result<T>` condiviso (con `Code` e `UnprocessableEntity`) introdotto.
- [ ] Mapper HTTP unico attivo; mapper per-modulo delegano ad esso.
- [ ] Moduli migrati al `Result` condiviso (Core, Identity, KinList) e shim rimossi.
- [ ] Eccezioni di dominio e `PostgreSqlOptions` consolidati (o scelta documentata di mantenerli separati).
- [ ] Contratto d'errore invariato per i client (contract/regression test).
- [ ] Nessun errore di validazione mappato a 5xx.
- [ ] Suite `Kin.KinHub.Core.Test` completa verde.
- [ ] Rapporto 4xx/5xx verificato in staging dopo il rollout.
