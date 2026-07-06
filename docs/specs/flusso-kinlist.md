# Descrizione generale

La macro feature **KinList** implementa le **liste condivise** della famiglia (tipicamente liste della spesa / da fare) con due caratteristiche distintive rispetto al resto del backend:

1. **Robustezza dell'API** — controllo di **concorrenza ottimistica** tramite ETag/`If-Match`, **idempotenza** sulla creazione (header `Idempotency-Key`), soft-delete con restore, e transazioni esplicite.
2. **Pipeline audio asincrona** — l'utente può creare/arricchire una lista **dettando un vocale**: l'audio è caricato su Azure Blob, messo in coda, e un worker di background lo trascrive (Azure AI Speech) e ne estrae gli item (Azure OpenAI), producendo una bozza che l'utente poi conferma.

Cosa fa:

- CRUD su liste (`KinList`) e item (`KinListItem`), con completamento/ripristino item e conferma "bulk".
- Gestione del ciclo di vita delle **operazioni audio** (`AudioProcessingOperation`): richiesta di upload (SAS), completamento upload, polling dello stato, cancellazione.
- Elaborazione asincrona degli audio nel worker, con retry, poison queue e claim atomico dell'operazione.

Perché esiste: fornire un'esperienza collaborativa affidabile su dati condivisi (più membri/dispositivi della stessa famiglia) e un ingresso vocale a bassa frizione.

Parti coinvolte:

- **Presentation** — `ListsController`, `AudioOperationsController` (`KinList.Api/KinListFeature`), `KinListValidators`, `KinListHttpResultMapper`, `IdempotencyRecordCleanupService`, `RemoteFamilyContextResolver`; e l'host worker `KinList.AudioWorker` (`AudioProcessingWorkerService`, `AudioProcessingQueuePump`).
- **Business** — `KinList.Business/KinListFeature`:
  - `KinListService` (527 righe, implementa `IKinListService`) — CRUD liste, item, idempotenza e mapping.
  - `KinListAudioService` (implementa `IKinListAudioService` e `IAudioOperationProcessor`) — ciclo di vita delle operazioni audio e elaborazione worker.
  - `KinListItemDeduplicator` — logica di confronto tra item proposti e item esistenti (usata da `KinListAudioService`).
  - `KinListMapper` — conversione entità↔DTO condivisa tra i due service.
  - Interfacce (`IAudioProcessingQueue`, `IAudioProcessingBlobStorage`, `IKinListAudioDraftGenerator`), telemetria (`KinListAudioTelemetry`), e le implementazioni "Unavailable" di default; `IKinListTransactionExecutor`.
- **Domain** — `KinList.Domain/KinListFeature`: entità `KinList`, `KinListItem`, `AudioProcessingOperation`, `IdempotencyRecord`; enum `AudioProcessingOperationType/Status`; interfacce repository.
- **Infrastructure** — `KinList.PostgreSql` (repository, `EfKinListTransactionExecutor`, `TryStartProcessingAsync` via SQL), `KinList.Ai` (trascrizione + interpretazione), `KinList.AzureStorage` (Blob + Queue).

Dati ricevuti: `CreateKinListRequest`, `UpdateKinListRequest`, `CreateKinListItemRequest`, `BulkConfirmKinListItemsRequest`, `UpdateKinListItemRequest`, `CreateAudioProcessingOperationRequest`. Dati prodotti: `KinListResponse`/`KinListDetailResponse` (con `ETag`), `CreateAudioProcessingOperationResponse` (con `UploadUrl` SAS), `AudioProcessingOperationResponse` (stato + item proposti + duplicati).

Dipendenze: EF Core/Npgsql, Azure Blob/Queue Storage, Azure AI Speech, Azure OpenAI, telemetria OpenTelemetry, il contesto famiglia remoto verso Identity.

# Casi d'uso

- **Creazione lista (idempotente)** — *Attore*: utente autenticato con contesto famiglia. *Input*: `CreateKinListRequest` + header `Idempotency-Key` obbligatorio. *Output*: 201 `KinListDetailResponse` + header `ETag`. *Condizioni/errori*: chiave assente → 400; stessa chiave con payload diverso → 409 `idempotency_conflict`; stessa chiave con stesso payload → **replay** della risposta memorizzata; troppi item → 400 `list_item_limit_exceeded`.
- **Lettura liste / lista** — *Output*: elenco ordinato (non completate prima) o dettaglio. *Errore*: lista di un'altra famiglia → 403; inesistente/eliminata → 404.
- **Aggiornamento/eliminazione/ripristino lista** — *Input*: header `If-Match` obbligatorio (ETag). *Errore*: `If-Match` assente → 400; ETag non combaciante → 409 `etag_conflict`.
- **Aggiunta / update / delete / restore item, bulk confirm** — analoghi, con `If-Match` e limiti (`MaxItemsPerList`, `MaxItemsPerBulkConfirm`).
- **Operazione audio: creazione** — *Output*: 202 Accepted con `UploadUrl` (SAS) + header `Location`/`Retry-After`. *Validazioni*: tipo operazione (`NewList`/`AppendItems`), MIME ammesso (con `NormalizeMimeType` e validazione robusta tramite `MediaTypeHeaderValue.TryParse`), dimensione dichiarata nei limiti; per `AppendItems` la lista deve esistere ed essere della famiglia.
- **Operazione audio: completamento upload** — verifica che il blob esista e sia entro i limiti, marca `Queued` e **accoda** il messaggio.
- **Operazione audio: polling** — restituisce lo stato; se scaduta e non terminale, la marca `Expired`.
- **Operazione audio: cancellazione** — cancella il blob e marca `Cancelled`.
- **Elaborazione asincrona (worker)** — consuma la coda, claim atomico, trascrizione + interpretazione, salva la bozza o marca fallita; con retry e poison queue.

# Flusso implementativo

## 1. Punto di ingresso

- CRUD liste: `ListsController` su `api/lists`, protetto a livello di classe da `[Authorize(Policy = FamilyContextRequirement.PolicyName)]` — nessuna guardia inline per azione.
- Operazioni audio (lato API): `AudioOperationsController` su `api/audio-operations` (`CreateAsync`, `CompleteUploadAsync`, `GetAsync`, `DeleteAsync`).
- Elaborazione asincrona (worker): `AudioProcessingWorkerService.ExecuteAsync` (un `BackgroundService`) che riceve messaggi dalla coda tramite `IAudioProcessingQueuePump`.

L'host API è bootstrappato da `AddKinHubKinListApi`; il worker da `Program.cs` di `KinList.AudioWorker` che registra Business + PostgreSql + Ai + AzureStorage e il `BackgroundService`.

## 2. Validazione iniziale

- `ListsController` è protetto a livello di classe da `[Authorize(Policy = FamilyContextRequirement.PolicyName)]`; le action leggono direttamente gli header obbligatori: `Idempotency-Key` (creazione) o `If-Match` (mutazioni), senza guardie `IsAuthenticated`/`HasFamilyContext` ripetute per ogni action.
- Validazione FluentValidation via `IRequestValidator<T>` (`KinListValidators`).
- Per le operazioni audio: `CreateAudioOperationAsync` valida tipo operazione, MIME tramite `NormalizeMimeType` (normalizza i parametri MIME) + `MediaTypeHeaderValue.TryParse` (validazione robusta contro `AllowedAudioMimeTypes`) e dimensione (`MaxAudioBytes`); per `AppendItems` valida esistenza/possesso della lista.

## 3. Orchestrazione applicativa

- Il controller chiama `IKinListService` (`KinListService`) per le operazioni su liste e item.
- Il controller chiama `IKinListAudioService` (`KinListAudioService`) per le operazioni audio.
- Le mutazioni di lista/item (in `KinListService`) sono eseguite dentro `IKinListTransactionExecutor.ExecuteAsync(...)` (una transazione EF con strategia di retry) e seguono lo schema: carica → `ValidateListMutation` (esistenza + possesso + ETag) → applica → `TouchList` (nuova `Version`, timestamp) → salva → rimappa tramite `KinListMapper`.
- **Creazione idempotente** (`KinListService.CreateAsync`): normalizza gli item (trim + distinct via `KinListItemDeduplicator`), calcola un `requestHash` (SHA-256 di title+items), elimina i record idempotenti scaduti, poi cerca un record attivo con la stessa chiave: se esiste con stesso hash → **replay** della `ResponseJson`; se con hash diverso → 409; altrimenti crea lista + item + salva un `IdempotencyRecord` con la risposta serializzata (TTL `IdempotencyRetentionHours`).
- **Operazioni audio** (`KinListAudioService`): `CreateAudioOperationAsync` genera un `blobName = {familyId}/{operationId}`, crea un **SAS di upload** e persiste l'operazione in stato `AwaitingUpload`; `CompleteAudioOperationUploadAsync` verifica il blob, passa a `Queued` e chiama `_audioQueue.EnqueueAsync`.

## 4. Logica di dominio

- **Concorrenza ottimistica**: ogni `KinList`/`KinListItem` ha una `Version` (GUID) esposta come ETag `"{version}"`. Le mutazioni richiedono `If-Match` combaciante (`MatchesEtag`); ogni modifica rigenera la `Version` (`TouchList`/nuovo GUID sull'item). Un client con ETag obsoleto riceve 409.
- **Soft-delete + restore**: liste e item hanno `IsDeleted`; le letture filtrano i cancellati; `restore` li riporta visibili (gli item ripristinati tornano non completati con nuovo `ActivationOrder`).
- **Ordinamento/attivazione item**: `ActivationOrder` (long) determina l'ordine; `GetNextActivationOrderAsync` calcola il massimo+1; il completamento di un item lo sposta in fondo alla sezione completati.
- **Idempotenza**: `IdempotencyRecord` (chiave + familyId + userId + hash + risposta + scadenza) garantisce che un retry di rete non crei liste duplicate.
- **Macchina a stati dell'operazione audio**: `AwaitingUpload → Queued → Processing → Succeeded/Failed`, più `Expired`/`Cancelled`. Le transizioni sono difese: `CompleteAudioOperationUploadAsync` è idempotente per stati già avanzati; `ProcessAudioOperationAsync` rifiuta stati non `Queued`.

## 5. Accesso ai dati

- Repository in `KinList.PostgreSql/KinListFeature`: `KinListRepository`, `KinListItemRepository`, `IdempotencyRecordRepository`, `AudioProcessingOperationRepository`, con mapping **manuale** entità↔dominio; `KinListDbContext`.
- Le letture usano `AsNoTracking()` con ordinamenti espliciti; le scritture ricaricano l'entità tracciata e ne aggiornano i campi.
- **Transazioni**: `EfKinListTransactionExecutor` usa `Database.CreateExecutionStrategy()` + `BeginTransactionAsync/CommitAsync`; il fallback fuori dall'infrastruttura reale è `NoOpKinListTransactionExecutor` (esegue senza transazione).
- **Claim atomico dell'operazione**: `AudioProcessingOperationRepository.TryStartProcessingAsync` esegue una singola `UPDATE … SET Status=Processing, AttemptCount=AttemptCount+1 … WHERE Id=@id AND Status=Queued`. Se 0 righe aggiornate, un altro worker l'ha già presa (nessun doppio processing).
- **Pulizia**: `IdempotencyRecordCleanupService` (BackgroundService nell'API) elimina periodicamente i record idempotenti scaduti.

## 6. Integrazioni esterne

- **Azure Blob Storage** (`AzureBlobAudioProcessingBlobStorage`): `CreateUploadTargetAsync` genera un SAS **User Delegation** (permessi Create+Write, TTL configurabile) così il client carica l'audio direttamente su Blob senza passare i byte dall'API; `GetBlobAsync`/`OpenReadAsync`/`DeleteIfExistsAsync` per verifica, lettura e pulizia.
- **Azure Queue Storage** (`AzureQueueAudioProcessingQueue` / `AudioProcessingQueuePump`): coda di processing + **poison queue**; il messaggio ha un `ContractVersion` e un `CorrelationId`; visibilità rinnovata periodicamente dal worker.
- **Azure AI Speech** (`AzureSpeechKinListTranscriber`): trascrizione con `TranscriptionClient` (Managed Identity via `DefaultAzureCredential` o API key), locali candidati configurabili, timeout e retry sui transitori (`TransientExecutionHelper`).
- **Azure OpenAI** (`AzureOpenAiKinListAudioPromptInterpreter` tramite `AzureSpeechOpenAiKinListAudioDraftGenerator`): dal transcript estrae titolo + item (interpretazione con prompt), producendo `ParsedKinListAudioDraft`.
- **Identity (HTTP)**: `RemoteFamilyContextResolver` risolve il `familyId` come nelle altre API.
- **Azure Monitor/OpenTelemetry**: tracing della pipeline audio (`KinListAudioTelemetry.ActivitySource`, propagazione del correlation context sul messaggio di coda).

## 7. Gestione errori

- **API sincrona**: `KinListHttpResultMapper` traduce `Result<T>` (che qui ha stati aggiuntivi come `UnprocessableEntity`) in HTTP; gli errori includono un `code` applicativo (`etag_conflict`, `idempotency_conflict`, `list_item_limit_exceeded`, `invalid_audio_mime_type`, …).
- **Elaborazione asincrona** (`KinListAudioService.ProcessAudioOperationAsync`): distingue **fallimenti terminali** (`ValidationError`/`UnprocessableEntity`/`Unauthorized`/`NotFound`/`Conflict` → `IsTerminalAudioFailure`) che marcano l'operazione `Failed`, dai **fallimenti transitori** (es. servizio non disponibile) che richiamano `RequeueOperationAsync` (torna `Queued`) e restituiscono `ServiceUnavailable`, così il messaggio viene ritentato. Eccezioni inattese che non rientrano nei casi noti provocano requeue controllato anziché propagazione cieca.
- **Worker** (`AudioProcessingWorkerService.ProcessMessageAsync`): payload non deserializzabile o `ContractVersion` non supportata → **poison queue**; superamento di `AudioProcessingMaxDequeues` → marca `Failed` e sposta in poison; esito `Succeeded`/`Failed` → cancella il messaggio; altri esiti → lascia il messaggio (verrà ritentato). La **visibilità** del messaggio è rinnovata da `RenewVisibilityAsync` finché il processing è in corso.
- **Timeout trascrizione**: `AzureSpeechKinListTranscriber.ExecuteWithTimeoutAsync` mappa il timeout in `ServiceUnavailable` (`audio_processing_timeout`), quindi ritentabile.

## 8. Output finale

- Mutazioni lista/item: entità aggiornate in transazione, nuova `Version`/ETag restituito nell'header `ETag` (`ApplyEtag`), corpo `KinListDetailResponse`.
- Creazione operazione audio: 202 Accepted, `AudioProcessingOperation` in `AwaitingUpload`, `UploadUrl` (SAS) + `Location`/`Retry-After`.
- Completamento upload: operazione `Queued` + **messaggio accodato**.
- Elaborazione riuscita: operazione `Succeeded` con `Title`, `ProposedItemsJson`, `DetectedLanguage`, `PromptVersion`; il blob viene cancellato; per `AppendItems` la risposta di polling arricchisce gli item con proposte e **duplicati** rispetto alla lista esistente (tramite `KinListItemDeduplicator`).
- Elaborazione fallita/terminale: `Failed` con `ErrorCode`/`ErrorMessage`; messaggio cancellato o spostato in poison.

# Pattern correttamente implementati

- **Optimistic Concurrency (ETag/If-Match)** — *File*: `KinListService` (`MatchesEtag`, `TouchList`, `ToEtag`) + `ListsController` (`ReadIfMatch`, `ApplyEtag`). *Perché corretto*: ogni entità versiona un GUID esposto come ETag; le mutazioni falliscono con 409 se la `Version` non combacia, prevenendo *lost update* tra dispositivi. Implementazione coerente su liste e item.

- **Idempotency Pattern** — *File*: `KinListService.CreateAsync` + `IdempotencyRecordRepository` + `IdempotencyRecordCleanupService`. *Perché corretto*: chiave client + hash del payload; stessa chiave/stesso payload → replay, payload diverso → conflitto; i record scadono e vengono ripuliti. Risolve i retry di rete sulle creazioni.

- **Transaction Script + Unit of Work esplicito** — *File*: `EfKinListTransactionExecutor` (interfaccia `IKinListTransactionExecutor`). *Perché corretto*: ogni caso d'uso mutante è avvolto in una transazione con execution strategy (retry sui transitori DB); a differenza di Family/Recipe — che ora usano `EfCoreTransactionExecutor` — qui l'esecutore è specifico del contesto KinList.

- **Claim atomico / Competing Consumers** — *File*: `AudioProcessingOperationRepository.TryStartProcessingAsync` (UPDATE condizionale su `Status=Queued`). *Perché corretto*: garantisce che una sola esecuzione elabori l'operazione anche con più worker/più dequeue, senza lock applicativi.

- **Retry + Poison Queue + Dead-lettering** — *File*: `AudioProcessingWorkerService` (dequeue limit → poison), `KinListAudioService.RequeueOperationAsync`, `TransientExecutionHelper`. *Perché corretto*: separa fallimenti terminali (non ritentare) da transitori (ritentare) e isola i messaggi "velenosi", evitando loop infiniti.

- **Adapter / Strategy sull'infrastruttura** — interfacce `IAudioProcessingBlobStorage`, `IAudioProcessingQueue`, `IKinListAudioDraftGenerator`, `IKinListSpeechTranscriber` con implementazioni Azure e **Null Object** di default (`Unavailable…`, `NoOp…`). *Perché corretto*: il Business è testabile senza Azure e degrada in modo controllato se l'infrastruttura non è configurata.

- **Pipeline (trascrizione → interpretazione)** — *File*: `AzureSpeechOpenAiKinListAudioDraftGenerator.ParseAsync` compone `IKinListSpeechTranscriber` + `IKinListAudioPromptInterpreter`. *Correttezza*: passi disaccoppiati e sostituibili; il decorator `TelemetryKinListAudioDraftGenerator` aggiunge osservabilità senza toccare la logica.

- **Decomposizione del service** — `KinListService` (CRUD liste/item/idempotenza) + `KinListAudioService` (ciclo di vita audio + elaborazione worker) + `KinListItemDeduplicator` + `KinListMapper`. *Correttezza*: ogni classe ha una singola responsabilità; l'elaborazione audio (consumata dal worker) è separata dal servizio CRUD (consumato dall'API).

- **Options Pattern con validazione** — `KinListOptions.Validate()`, `AudioStorageOptions.Validate()` invocati all'avvio del worker/API.

> I valori operativi (limiti item, TTL SAS, numero massimo di dequeue, locali di trascrizione, nomi di coda/container) provengono dalla configurazione e non sono deducibili con certezza dalla codebase analizzata.
