> Stato validazione: PASS
> Iterazioni eseguite: 2

# Obiettivo del refactor

Scomporre il "God Service" `KinListService` in componenti a responsabilità singola, mantenendo invariato il comportamento esterno (API e worker). Oggi una sola classe (~1075 righe) implementa contemporaneamente l'API sincrona delle liste (`IKinListService`) e il processore asincrono usato dal worker audio (`IAudioOperationProcessor`), oltre a mapping e deduplica degli item. L'obiettivo è migliorare **manutenibilità, testabilità e leggibilità** (soprattutto per sviluppatori junior) riducendo il rischio che ogni modifica tocchi un punto critico condiviso tra due processi diversi.

Problema risolto: **alta densità di responsabilità** in un unico file critico e **duplicazione** della logica di deduplica.

# Stato attuale

`KinListService` (`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs`) implementa **due** interfacce:

- `IKinListService` — API sincrona: `GetAllAsync`, `GetByIdAsync`, `CreateAsync` (idempotente), `UpdateAsync`, `DeleteAsync`, `RestoreAsync`, `AddItemAsync`, `BulkConfirmItemsAsync`, `UpdateItemAsync`, `DeleteItemAsync`, `RestoreItemAsync`, `CreateAudioOperationAsync`, `CompleteAudioOperationUploadAsync`, `GetAudioOperationAsync`, `DeleteAudioOperationAsync`, `CreateDraftFromAudioAsync`, `CreateItemDraftsFromAudioAsync`.
- `IAudioOperationProcessor` — usato dal worker: `ProcessAudioOperationAsync`, `MarkAudioOperationFailedAsync`.

Registrazione DI (`src/Businesses/Kin.KinHub.KinList.Business/ServiceCollectionExtensions.cs`):
```
services.AddScoped<IKinListService, KinListService>();
services.AddScoped<IAudioOperationProcessor>(sp => (KinListService)sp.GetRequiredService<IKinListService>());
```
cioè lo stesso oggetto è risolto per entrambe le interfacce (con un cast esplicito).

Responsabilità mescolate nella classe:

- **CRUD liste/item** con transazioni (`_transactionExecutor.ExecuteAsync`), concorrenza ottimistica (`MatchesEtag`, `TouchList`), idempotenza (`ComputeHash`, `IdempotencyRecord`).
- **Ciclo di vita operazioni audio**: `CreateAudioOperationAsync` (SAS + persistenza `AwaitingUpload`), `CompleteAudioOperationUploadAsync` (verifica blob + enqueue), `ProcessAudioOperationAsync` (claim atomico + trascrizione/interpretazione + salvataggio bozza + retry/terminal), `MarkAudioOperationFailedAsync`, `GetAudioOperationAsync`, `DeleteAudioOperationAsync`.
- **Mapping** (`MapSummary`, `MapDetail`, `MapItem`, `MapAudioOperationAsync`) e **deduplica** degli item audio, con logica quasi identica in `CreateItemDraftsFromAudioAsync` e in `MapAudioOperationAsync` (proposte + `ExistingDuplicates`).

Consumatori: `ListsController` e `AudioOperationsController` (`KinList.Api`) usano `IKinListService`; `AudioProcessingWorkerService` (`KinList.AudioWorker`) risolve `IAudioOperationProcessor` da uno scope. Test esistenti: `KinListServiceTests`, `KinListApiIntegrationTests`, `KinListAudioPipelineTests`, `KinListAudioWorkerTests`.

# Problemi individuati

- **God Service / violazione SRP (rischio architetturale + debito tecnico)**: una classe con due ragioni di cambiamento molto diverse (contratto API vs elaborazione asincrona). Ogni evoluzione dell'una rischia di impattare l'altra.
- **Duplicazione della logica di deduplica (debito tecnico + rischio di regressione)**: il confronto con gli item esistenti e la costruzione di proposte/duplicati è ripetuto in `CreateItemDraftsFromAudioAsync` e `MapAudioOperationAsync`; una correzione in un punto può non essere replicata nell'altro.
- **Cast DI fragile (debito tecnico)**: `(KinListService)sp.GetRequiredService<IKinListService>()` accoppia le due interfacce alla stessa classe concreta; è un indizio di responsabilità non separate.
- **Testabilità ridotta (debito tecnico)**: testare il processore audio richiede istanziare l'intero servizio con tutte le dipendenze (repository, blob, coda, generatore, executor), anche per scenari che riguardano solo il worker.
- **Barriera per junior (manutenibilità)**: un file da oltre mille righe con due flussi intrecciati è difficile da comprendere e modificare in sicurezza.
- **Cattura generica in `ProcessAudioOperationAsync`** (`catch (Exception) → RequeueOperationAsync`): qualsiasi eccezione è trattata come transitoria. È un problema correlato di error handling (dettagliato anche in [refactor-points.md](./refactor-points.md)) che conviene affrontare durante la separazione del processore.

# Come Microsoft farebbe il refactor

Refactor strutturale **behavior-preserving**, guidato dai test di caratterizzazione già presenti, con estrazioni incrementali e nessun cambiamento di contratto:

1. **Separare per asse di cambiamento**: due componenti distinti dietro le interfacce esistenti — un `KinListService` per l'API liste/item e un `AudioOperationProcessor` per il ciclo di vita/elaborazione audio. Le interfacce `IKinListService` e `IAudioOperationProcessor` restano invariate (backward compatible), cambia solo l'implementazione e la registrazione DI (niente più cast).
2. **Estrarre i collaboratori condivisi**: un `KinListMapper`/`KinListItemDeduplicator` che centralizzi mapping e deduplica, riusato da entrambi i componenti (elimina la duplicazione).
3. **Interfacce solo dove servono**: non creare un'astrazione per ogni metodo; separare i due componenti principali e i mapper/deduplicator condivisi.
4. **Migliorare l'error handling del processore** contestualmente: distinguere eccezioni transitorie da definitive invece del `catch (Exception)` generico.
5. **Test prima**: usare `KinListServiceTests`/`KinListAudioPipelineTests`/`KinListAudioWorkerTests` come rete; aggiungere test unitari sul deduplicator estratto.
6. **Deploy progressivo + rollback**: ogni estrazione è un commit isolato e reversibile; nessuna migrazione DB.

# Piano operativo

**Step 1 — Consolidare i test di caratterizzazione.**
- *Cosa*: verificare che CRUD (idempotenza, ETag, limiti), ciclo di vita audio (create/complete/process/get/delete) e deduplica siano coperti; colmare i buchi.
- *Dove*: `src/Tests/Kin.KinHub.Core.Test` (`KinListServiceTests`, `KinListAudioPipelineTests`, `KinListAudioWorkerTests`).
- *Perché*: rete di sicurezza per un refactor behavior-preserving.
- *Impatto/Rischio*: nessuno sul runtime; basso.
- *Test dopo*: suite KinList verde.

**Step 2 — Estrarre mapping e deduplica.**
- *Cosa*: creare `KinListMapper` (`MapSummary`/`MapDetail`/`MapItem`) e `KinListItemDeduplicator` (logica di proposte/duplicati) e farli usare dai punti attuali, incluso `MapAudioOperationAsync` e `CreateItemDraftsFromAudioAsync`.
- *Dove*: `KinList.Business/KinListFeature/Services`.
- *Perché*: rimuovere la duplicazione, isolare logica pura e facilmente testabile.
- *Impatto previsto*: comportamento invariato.
- *Rischio dello step*: basso/medio.
- *Test dopo*: unit test del deduplicator + suite KinList.

**Step 3 — Estrarre `AudioOperationProcessor`.**
- *Cosa*: spostare `ProcessAudioOperationAsync`, `MarkAudioOperationFailedAsync`, `CreateAudioOperationAsync`, `CompleteAudioOperationUploadAsync`, `GetAudioOperationAsync`, `DeleteAudioOperationAsync` in una classe dedicata che implementa `IAudioOperationProcessor` (e, se opportuno, la parte "audio" di `IKinListService`), riusando mapper/deduplicator/executor.
- *Dove*: `KinList.Business/KinListFeature/Services` + registrazione in `ServiceCollectionExtensions` (rimuovere il cast).
- *Perché*: separare il flusso worker dal flusso API.
- *Impatto previsto*: invariato; DI più pulita.
- *Rischio dello step*: medio (tocca il flusso worker).
- *Test dopo*: `KinListAudioWorkerTests` + integrazione audio.

**Step 4 — Ridurre `KinListService` all'API liste/item.**
- *Cosa*: lasciare in `KinListService` solo CRUD liste/item; delegare la parte audio al nuovo componente dove necessario.
- *Dove*: `KinListService.cs`.
- *Perché*: file più piccolo e focalizzato.
- *Impatto/Rischio*: medio.
- *Test dopo*: `KinListServiceTests` + `KinListApiIntegrationTests`.

**Step 5 — Migliorare l'error handling del processore.**
- *Cosa*: nel processo audio distinguere eccezioni transitorie (requeue) da definitive (fail), invece del `catch (Exception)` generico.
- *Dove*: nuovo `AudioOperationProcessor`.
- *Perché*: evitare retry inutili di errori non transitori prima del poison.
- *Impatto previsto*: meno retry sprecati.
- *Rischio dello step*: basso/medio.
- *Test dopo*: test su fallimento transitorio vs definitivo.

# Pattern da applicare

- **Single Responsibility / Extract Class**.
  - *Problema*: due assi di cambiamento in una classe. *Dove*: API liste vs processore audio. *Perché adatto*: separa i cicli di vita. *Non overengineering*: due componenti principali, non frammentazione eccessiva.
- **Mapper dedicato**.
  - *Problema*: mapping sparso. *Dove*: `KinListMapper`. *Perché adatto*: funzioni pure testabili. *Non overengineering*: raccoglie codice già esistente.
- **Deduplicator riusabile (rimozione duplicazione)**.
  - *Problema*: logica di deduplica duplicata. *Dove*: `KinListItemDeduplicator`. *Perché adatto*: una sola fonte di verità. *Non overengineering*: elimina codice, non ne aggiunge.

# Anti-pattern da rimuovere

- **God Service**: `KinListService` diviso per responsabilità.
- **Duplicazione di logica**: deduplica centralizzata.
- **Cast DI fragile**: registrazioni separate per `IKinListService` e `IAudioOperationProcessor`.
- **Cattura generica delle eccezioni** nel processo audio: sostituita da distinzione transitorio/definitivo.

# Strategia di test

- **Unit test**: `KinListItemDeduplicator` (item nuovi vs duplicati, case-insensitive, item completati), `KinListMapper` (ordinamento, conteggi, ETag), `AudioOperationProcessor` (claim atomico, transitorio→requeue, terminale→failed) con fake di repository/blob/coda/generatore.
- **Integration test**: `KinListApiIntegrationTests` per il CRUD liste (idempotenza, ETag, limiti) e `KinListAudioPipelineTests`/`KinListAudioWorkerTests` per il flusso audio end-to-end.
- **Regression test**: intera suite KinList prima e dopo ogni step (behavior-preserving).
- **Contract test**: verificare che le risposte (`KinListDetailResponse`, `AudioProcessingOperationResponse`, header ETag/Retry-After) restino identiche.
- **Scenari da coprire *prima* di iniziare**: creazione idempotente (replay + conflict), mutazione con ETag errato (409), ciclo audio completo (create→complete→process→succeeded), fallimento terminale e transitorio.

# Rischi del refactor

- **Regressione comportamentale durante l'estrazione**: mitigata dalla natura behavior-preserving e dai test di caratterizzazione dello Step 1.
- **Rottura della registrazione DI**: separare le due interfacce potrebbe cambiare l'istanza risolta — mitigazione: mantenere lifetime `Scoped` coerenti e testare la composizione (avvio host + worker).
- **Cambio del comportamento di retry (Step 5)**: distinguere le eccezioni può alterare quando un'operazione va in poison — mitigazione: test espliciti sui due percorsi e rollout monitorato.
- **Scope condivisi tra worker e processore**: verificare che `IServiceScopeFactory.CreateScope()` nel worker risolva correttamente il nuovo componente.

# Strategia di rollback

- Ogni step è un commit isolato e reversibile; nessuna migrazione DB coinvolta.
- La separazione può essere rilasciata gradualmente: prima l'estrazione di mapper/deduplicator (bassissimo rischio), poi il processore.
- In caso di problemi, **revert** del commit dell'estrazione ripristina la classe unica; poiché le interfacce pubbliche non cambiano, i consumatori (controller/worker) non richiedono modifiche.
- Deploy progressivo con monitoraggio delle metriche della pipeline audio (`KinListAudioTelemetry`) e dell'error rate API.

# Checklist finale

- [ ] Test di caratterizzazione CRUD + audio consolidati e verdi prima delle modifiche.
- [ ] Mapping estratto in `KinListMapper`.
- [ ] Deduplica centralizzata in `KinListItemDeduplicator` (nessuna logica duplicata residua).
- [ ] `AudioOperationProcessor` estratto; `IAudioOperationProcessor` registrato senza cast.
- [ ] `KinListService` ridotto all'API liste/item.
- [ ] Error handling del processore distingue transitorio/definitivo.
- [ ] Contratti/response e header invariati (contract/regression test).
- [ ] Suite KinList completa verde (unit + integration + worker).
- [ ] Composizione DI verificata su avvio API e worker.
- [ ] Telemetria pipeline audio verificata in staging.
