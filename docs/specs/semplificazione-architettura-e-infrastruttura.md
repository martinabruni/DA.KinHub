# Semplificazione di architettura e infrastruttura

## 1. Descrizione generale

Questo documento censisce e semplifica **architettura applicativa (.NET)** e **infrastruttura (Bicep / CI-CD)** della soluzione **Kin.KinHub** (root del repository, file di soluzione `Kin.KinHub.Core.slnx`). L'obiettivo è ridurre duplicazione, rimuovere anti-pattern e allineare il codice e l'IaC alle best practice Microsoft, mantenendo intatto il comportamento funzionale.

> **Contesto progetto.** KinHub è un'applicazione personale a uso familiare, con un singolo manutentore e un budget cloud **massimo di 30 €/mese**. Le leve infrastrutturali corrette a questa scala sono il **free tier**, lo **scale-to-zero** e il **tier Burstable**, non l'alta disponibilità enterprise. Lo standard WAF di riferimento prevalente è quindi **Cost Optimization**, che a questa scala precede Reliability e Security enterprise. Le raccomandazioni cloud costose (Private Endpoint, HA/General Purpose, geo-ridondanza, `minReplicas ≥ 1`) sono state deliberatamente escluse: sono dettagliate nella sezione 7.

L'ambito copre due assi:

- **Applicazione .NET** — i progetti sotto `src/` (Domain, Business, Infrastructure, Presentation, Shared), con particolare attenzione al contesto KinList e alle eccezioni di dominio condivise.
- **Infrastruttura come codice e pipeline** — i template Bicep sotto `ops/iac/` e i workflow GitHub Actions sotto `.github/workflows/`.

Il metodo applicato per ogni finding è: **audit** (individuazione con riferimento `file:riga` e estratto fedele) → **soluzione in stile Microsoft** (con lo standard di riferimento citato e uno snippet target) → **roadmap** (ondate prioritizzate con dipendenze e criteri d'accettazione).

> **Avvertenza sui documenti stali.** Alcuni piani di refactoring precedenti (`msrefactor`) risultano **già implementati** nel codice: pertanto ogni finding di questo documento è stato **riverificato leggendo il codice attuale**. I finding non più validi sono stati scartati e sono elencati esplicitamente nella sezione 7. I finding la cui formulazione originale era imprecisa sono stati corretti e annotati.

## 2. Metodologia e criteri Microsoft di riferimento

Ogni soluzione proposta cita esplicitamente lo standard Microsoft (o il principio di ingegneria) pertinente:

| Standard / Principio | Uso in questo documento |
| --- | --- |
| **.NET Framework Design Guidelines** | Progettazione di tipi (eccezioni, API pubbliche, costruttori). |
| **Dependency Injection best practices in .NET** | Costruttori senza logica, nessuna composizione manuale, dipendenze esplicite. |
| **Options pattern (`IOptions<T>`)** | Configurazione tipizzata e validata, iniettata invece che composta a mano. |
| **Clean Architecture .NET** | Direzione delle dipendenze, separazione delle interfacce, kernel condiviso. |
| **Azure Well-Architected Framework (WAF) — Cost Optimization** | Pilastro prevalente a questa scala: free tier, scale-to-zero, Burstable. |
| **Azure Well-Architected Framework (WAF) — Operational Excellence** | Processi ripetibili, IaC modulare, log minimi. |
| **Cloud Adoption Framework (CAF)** | Naming & tagging delle risorse. |
| **Bicep best practices** | Modularizzazione, parametri, evitare `any()`, parameter file per ambiente. |
| **Azure Verified Modules (AVM)** | Moduli riusabili e mantenuti per le risorse Azure. |
| **DRY / SRP / SOLID** | Rimozione di duplicazione e responsabilità multiple. |
| **NuGet Central Package Management (CPM)** | Versioni pacchetti centralizzate in `Directory.Packages.props`. |

## 3. Sintesi esecutiva

| ID | Area | Asse | Severità | Effort | Impatto |
| --- | --- | --- | --- | --- | --- |
| [APP-01](#app-01--eccezioni-di-dominio-duplicate-tra-contesti) | App | Duplicazione | Media | S | Consolidamento eccezioni in `Shared.Kernel` |
| [APP-02](#app-02--costruttore-a-12-parametri-con-composizione-di-default) | App | Anti-pattern | Alta | M | `KinListService` testabile, DI pulita |
| [APP-03](#app-03--facade-che-delega-i-metodi-audio-leaky-abstraction) | App | Anti-pattern | Media | M | Separazione interfacce liste/audio |
| [APP-04](#app-04--normalizetext--normalizedistinctitems-ripetuti-in-3-file) | App | Duplicazione | Bassa | S | Helper di normalizzazione unico |
| [APP-05](#app-05--logica-etag-dentro-il-mapper) | App | Anti-pattern | Bassa | S | `IEtagProvider` iniettabile |
| [APP-06](#app-06--telemetria-statica-per-il-correlation-id) | App | Anti-pattern | Bassa | S | `ICorrelationIdProvider` iniettabile |
| [APP-07](#app-07--convenzioni-di-progetto-non-centralizzate) | App | Complicazione | Media | S | `Directory.Build.props` + `.editorconfig` |
| [APP-08](#app-08--result-triplicato-tra-i-business) | App | Duplicazione | Alta | M | `Result<T>` unico in `Shared.Kernel`; fix divergenza codici |
| [APP-09](#app-09--switch-di-mappatura-http-duplicato) | App | Duplicazione | Media | S | Unico entry point per mapper HTTP Core/Identity |
| [APP-10](#app-10--guard-clause--controllo-ownership-ancora-inline) | App | Duplicazione | Media | S | Estendere `ValidateListMutation` ai metodi restore |
| [APP-11](#app-11--catchexception-che-ingoia-lo-stack-trace) | App | Anti-pattern | Media | S | Log strutturato + rimozione catch generico |
| [APP-12](#app-12--inconsistenza-di-stile-diffusa) | App | Inconsistenza | Media | S | `sealed`, DTO init/required, null-forgiving, via analyzer |
| [APP-13](#app-13--versioni-pacchetti-disallineate-nessun-central-package-management) | App | Manutenibilità | Media-Alta | S | `Directory.Packages.props` (CPM) |
| [APP-14](#app-14--divergenza-architetturale-tra-contesti) | App | Inconsistenza | Alta | L | Convergenza incrementale a un pattern unico |
| [APP-15](#app-15--metodi-orchestratori-lunghi--logica-non-commentata) | App | Leggibilità | Bassa | S | Decomposizione + commento invarianti |
| [IAC-01](#iac-01--deploy_dev-e-deploy_prod-quasi-identici) | IaC | Duplicazione | Alta | M | Job di deploy riusabile |
| [IAC-02](#iac-02--mainbicep-monolitico-senza-moduli) | IaC | Anti-pattern | Bassa | L | Modularizzazione Bicep (manutenibilità) |
| [IAC-03](#iac-03--role-definition-id-come-costanti-non-documentate) | IaC | Complicazione | Bassa | S | Costanti documentate |
| [IAC-04](#iac-04--anyproperties-sulle-static-web-apps) | IaC | Anti-pattern | Media | S | Ripristino type-safety |
| [IAC-06](#iac-06--listkeys-in-template-invece-di-managed-identity) | IaC | Gap best-practice | Media | M | Managed identity end-to-end (gratuito) |
| [IAC-07](#iac-07--postgresql-firewall-aperto-e-configurazione-non-adatta-al-budget) | IaC | Gap best-practice | Bassa | S | Restrizione regola firewall `0.0.0.0` |
| [IAC-08](#iac-08--nessun-parameter-file-per-ambiente-versionato) | IaC | Complicazione | Media | S | `*.bicepparam` per ambiente |
| [IAC-09](#iac-09--assenza-di-diagnostic-settings) | IaC | Gap best-practice | Bassa | S | Log minimi verso Log Analytics (free tier) |

> IAC-05 (Private Endpoint) rimosso: fuori budget — vedi sezione 7.

Legenda effort: **S** = ore, **M** = 1-2 giorni, **L** = più giorni.

## 4. Architettura applicativa (.NET)

### APP-01 — Eccezioni di dominio duplicate tra contesti

**Problema.** Le quattro eccezioni base sono definite **due volte in modo identico** (varia solo il namespace) tra i due domini.

`src/Domains/Kin.KinHub.Core.Domain/Common/Exceptions/DomainException.cs:3`

```csharp
namespace Kin.KinHub.Core.Domain.Common;

public abstract class DomainException : SharedDomainException
{
    protected DomainException(string message) : base(message) { }
}
```

`src/Domains/Kin.KinHub.Identity.Domain/Common/Exceptions/DomainException.cs:3` — **byte-per-byte uguale** salvo `namespace Kin.KinHub.Identity.Domain.Common;`.

Lo stesso vale per `DomainValidationException`, `EntityNotFoundException` e `DuplicateEntityException`. Esempio (`.../Core.Domain/.../EntityNotFoundException.cs:5` e `.../Identity.Domain/.../EntityNotFoundException.cs:5` sono identici):

```csharp
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
```

**Perché è un problema.** Duplicazione (violazione **DRY**). Esiste già un kernel condiviso (`Kin.KinHub.Shared.Kernel`) che ospita `SharedDomainException` / `SharedDomainValidationException`: le sottoclassi `DomainException`, `EntityNotFoundException` e `DuplicateEntityException` non aggiungono nulla di specifico al contesto e potrebbero vivere una sola volta. Ogni modifica al messaggio o alla firma va oggi replicata in due punti, con rischio di derive.

**Soluzione Microsoft-style.** Secondo le **.NET Framework Design Guidelines** (definire una gerarchia di eccezioni chiara e non ridondante) e il principio di **Clean Architecture** del *shared kernel*, si consolidano le eccezioni non specifiche nel kernel. `EntityNotFoundException` e `DuplicateEntityException` diventano tipi condivisi; ogni contesto continua a definire solo eccezioni realmente specifiche del dominio (se presenti).

```csharp
// src/Shared/Kin.KinHub.Shared.Kernel/Exceptions/EntityNotFoundException.cs
namespace Kin.KinHub.Shared.Kernel;

public sealed class EntityNotFoundException : SharedDomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
```

**Impatto.** Layer: Domain (tutti i contesti) + i punti che catturano queste eccezioni negli handler. *Breaking change* di namespace: i `using` che referenziano `Kin.KinHub.Core.Domain.Common` / `Kin.KinHub.Identity.Domain.Common` per queste eccezioni vanno aggiornati a `Kin.KinHub.Shared.Kernel`. Test da aggiornare: quelli che verificano il tipo/messaggio delle eccezioni catturate.

### APP-02 — Costruttore a 12+ parametri con composizione di default

**Problema.** `KinListService` ha un costruttore con **12 parametri**, di cui tre opzionali ricostruiti a mano con `?? new …`, e riceve dipendenze (`audioOperationRepository`, `audioDraftGenerator`, `blobStorage`, `audioQueue`, `deduplicator`) **solo per poterne comporre un altro servizio**.

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs:22`

```csharp
public KinListService(
    IKinListRepository listRepository,
    IKinListItemRepository itemRepository,
    IIdempotencyRecordRepository idempotencyRepository,
    IAudioProcessingOperationRepository audioOperationRepository,
    IKinListTransactionExecutor transactionExecutor,
    IKinListAudioDraftGenerator audioDraftGenerator,
    IAudioProcessingBlobStorage blobStorage,
    IAudioProcessingQueue audioQueue,
    KinListOptions options,
    IKinListMapper? mapper = null,
    IKinListItemDeduplicator? deduplicator = null,
    IKinListAudioService? audioService = null)
{
    ...
    _mapper = mapper ?? new KinListMapper();
    _audioService = audioService ?? new KinListAudioService(
        listRepository, itemRepository, audioOperationRepository,
        audioDraftGenerator, blobStorage, audioQueue,
        deduplicator ?? new KinListItemDeduplicator(), options);
    ...
}
```

**Perché è un problema.** Anti-pattern che viola **DI best practices** e **SRP**. Il *service locator manuale* nel costruttore (`?? new ...`) è **codice morto in produzione**: il container registra già ogni dipendenza reale — si veda `ServiceCollectionExtensions.cs:20-25`, dove `IKinListMapper`, `IKinListItemDeduplicator`, `IKinListAudioService` sono tutti registrati e `KinListService` è risolto dal container. I fallback esistono solo per i test manuali, ma rendono il costruttore ingannevole (dipendenze "opzionali" che in realtà sono sempre presenti) e trascinano 5 dipendenze usate solo per ricostruire `KinListAudioService`.

**Soluzione Microsoft-style.** Le **Dependency Injection best practices in .NET** prescrivono costruttori che **si limitano ad assegnare dipendenze già risolte**, senza logica di composizione né `new`. Poiché `IKinListAudioService` è già registrato, va **iniettato direttamente**, eliminando le 5 dipendenze di transito e i tre parametri opzionali.

```csharp
public KinListService(
    IKinListRepository listRepository,
    IKinListItemRepository itemRepository,
    IIdempotencyRecordRepository idempotencyRepository,
    IKinListTransactionExecutor transactionExecutor,
    IKinListAudioService audioService,
    IKinListMapper mapper,
    KinListOptions options)
{
    _listRepository = listRepository;
    _itemRepository = itemRepository;
    _idempotencyRepository = idempotencyRepository;
    _transactionExecutor = transactionExecutor;
    _audioService = audioService;
    _mapper = mapper;
    _options = options;
}
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change* per i consumer (la registrazione in `ServiceCollectionExtensions` continua a funzionare, anzi si semplifica). Test da aggiornare: i test unitari che costruiscono `KinListService` passando `null` per i parametri opzionali dovranno iniettare esplicitamente i *mock* di `IKinListAudioService`/`IKinListMapper`.

### APP-03 — Facade che delega i metodi audio (leaky abstraction)

**Problema.** `IKinListService` espone **6 metodi audio** che sono puri *passthrough* verso `IKinListAudioService`.

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Interfaces/IKinListService.cs:18-23` dichiara `CreateAudioOperationAsync`, `CompleteAudioOperationUploadAsync`, `GetAudioOperationAsync`, `DeleteAudioOperationAsync`, `ProcessAudioOperationAsync`, `MarkAudioOperationFailedAsync`, e l'implementazione delega senza logica:

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs:419`

```csharp
public Task<Result<CreateAudioProcessingOperationResponse>> CreateAudioOperationAsync(...) =>
    _audioService.CreateAudioOperationAsync(request, familyId, userId, cancellationToken);

public Task<Result<AudioProcessingOperationResponse>> CompleteAudioOperationUploadAsync(...) =>
    _audioService.CompleteAudioOperationUploadAsync(operationId, familyId, cancellationToken);
// ... altri 6 metodi identici come delega pura
```

**Perché è un problema.** Anti-pattern di *leaky abstraction* / facade degenerata: viola **SRP** (`IKinListService` ha due responsabilità: gestione liste **e** ciclo di vita audio) e **Interface Segregation Principle** (**SOLID**). I controller che gestiscono solo liste dipendono comunque da un'interfaccia gonfiata di metodi audio, e viceversa.

**Soluzione Microsoft-style.** Applicando **ISP** e **Clean Architecture** (interfacce piccole e coese, iniettate dove servono), si separano i due contratti. `IKinListService` mantiene solo le operazioni sulle liste; `IKinListAudioService` è già l'interfaccia audio (già registrata in DI, `ServiceCollectionExtensions.cs:22`) e va iniettata **direttamente** nei controller audio, rimuovendo i metodi di delega.

```csharp
// Controller liste
public KinListController(IKinListService listService) { ... }

// Controller/worker audio
public AudioController(IKinListAudioService audioService) { ... }
```

**Impatto.** Layer: Business + Presentation (controller e AudioWorker). *Breaking change* interno: i controller che oggi chiamano i metodi audio via `IKinListService` vanno ripuntati su `IKinListAudioService`. Test da aggiornare: i test dei controller e i test che verificano i metodi audio via `IKinListService`.

### APP-04 — NormalizeText / NormalizeDistinctItems ripetuti in 3 file

**Problema.** La coppia di helper di normalizzazione è **triplicata**.

- `src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs:512-519`
- `src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListAudioService.cs:393-400`
- `src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListItemDeduplicator.cs:61`

```csharp
private static List<string> NormalizeDistinctItems(IEnumerable<string> items) =>
    items
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Select(NormalizeText)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

private static string NormalizeText(string text) => text.Trim();
```

`KinListItemDeduplicator.cs:61` ridefinisce la stessa `NormalizeText`.

**Perché è un problema.** Duplicazione (violazione **DRY**). La normalizzazione è una **regola di dominio** (come si confrontano/deduplicano gli item): se cambia (es. normalizzazione accenti/maiuscole), va aggiornata in tre punti, con rischio di comportamenti divergenti tra la creazione lista, la pipeline audio e la deduplicazione.

**Soluzione Microsoft-style.** Estrarre un unico helper coeso (una *utility* statica interna o un servizio) secondo **DRY** e **SRP**, così che la regola di normalizzazione abbia una sola fonte di verità.

```csharp
// src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/KinListItemNormalizer.cs
internal static class KinListItemNormalizer
{
    public static string Normalize(string text) => text.Trim();

    public static IReadOnlyList<string> NormalizeDistinct(IEnumerable<string> items) =>
        items.Where(i => !string.IsNullOrWhiteSpace(i))
             .Select(Normalize)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList();
}
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change* pubblico. Test da aggiornare: eventuali test unitari sui singoli metodi privati (da ripuntare sull'helper).

### APP-05 — Logica ETag dentro il mapper

**Problema.** La generazione dell'ETag vive nel mapper e viene richiamata dal service per il confronto `If-Match`.

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListMapper.cs:67`

```csharp
public string ToEtag(Guid version) => $"\"{version:D}\"";
```

`KinListService.cs:495`

```csharp
private bool MatchesEtag(Guid version, string ifMatch) =>
    string.Equals(_mapper.ToEtag(version), ifMatch.Trim(), StringComparison.Ordinal);
```

**Perché è un problema.** Anti-pattern (**SRP**): il `KinListMapper` è responsabile della mappatura dominio→DTO; il formato dell'ETag (una policy di concorrenza HTTP) è una responsabilità distinta. Il service dipende dal mapper per una funzione che non è mappatura.

**Perché la severità è bassa.** L'ETag è comunque già dietro un'astrazione (`IKinListMapper.ToEtag`), non è codice sparso: il refactoring è cosmetico e va valutato solo se si tocca comunque l'area.

**Soluzione Microsoft-style.** Introdurre un piccolo contratto dedicato secondo **SOLID/ISP**, iniettato sia nel mapper (per popolare `ETag` nei DTO) sia nel service (per il confronto).

```csharp
public interface IEtagProvider
{
    string ToEtag(Guid version);
    bool Matches(Guid version, string ifMatch);
}
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change* esterno. Test da aggiornare: quelli che verificano il formato ETag.

### APP-06 — Telemetria statica per il correlation id

**Problema.** La risoluzione del `correlationId` è un metodo statico su una classe statica.

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListAudioTelemetry.cs:10`

```csharp
public static class KinListAudioTelemetry
{
    public static string ResolveCorrelationId(string? fallback = null) =>
        Activity.Current?.Id ?? fallback ?? Guid.NewGuid().ToString("D");
    ...
}
```

**Perché è un problema.** Anti-pattern (dipendenza statica **non iniettabile**): rende impossibile sostituire la sorgente del correlation id nei test (es. per ottenere un id deterministico) e accoppia il Business a `System.Diagnostics.Activity` in modo nascosto.

**Perché la severità è bassa.** `ActivitySource` è correttamente statico (è la prassi OpenTelemetry). Solo `ResolveCorrelationId` merita l'iniezione; il resto della classe può restare com'è.

**Soluzione Microsoft-style.** Estrarre un `ICorrelationIdProvider` iniettabile (registrato *singleton*), lasciando l'`ActivitySource` statico. Coerente con le **DI best practices in .NET** (dipendenze esplicite e sostituibili).

```csharp
public interface ICorrelationIdProvider
{
    string Resolve(string? fallback = null);
}

internal sealed class ActivityCorrelationIdProvider : ICorrelationIdProvider
{
    public string Resolve(string? fallback = null) =>
        Activity.Current?.Id ?? fallback ?? Guid.NewGuid().ToString("D");
}
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change* esterno. Test da aggiornare: i test della pipeline audio possono iniettare un provider deterministico.

### APP-07 — Convenzioni di progetto non centralizzate

**Problema.** Non esiste alcun `Directory.Build.props` né `.editorconfig` alla **root** del repository. Le convenzioni (`TargetFramework`, `Nullable`, `ImplicitUsings`, regole di stile/analyzer) sono ripetute nei singoli `.csproj`. Gli unici `.editorconfig` presenti sono file **generati** sotto `obj/` (`*.GeneratedMSBuildEditorConfig.editorconfig`) o dentro `node_modules/`, non convenzioni di progetto versionate.

**Perché è un problema.** Complicazione inutile e duplicazione: le proprietà comuni vanno ripetute per progetto; l'assenza di `.editorconfig` significa nessuna regola di stile/analyzer condivisa e applicata in build.

**Soluzione Microsoft-style.** Adottare i meccanismi MSBuild raccomandati da Microsoft: un **`Directory.Build.props`** alla root con le proprietà comuni, e un **`.editorconfig`** con le regole di stile e la severità degli analyzer.

```xml
<!-- Directory.Build.props (root) -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

**Impatto.** Layer: build/solution. *Breaking change* di build possibile: con `TreatWarningsAsErrors` gli avvisi esistenti vanno risolti (introdurre gradualmente). Si aggancia ad APP-12 (stile) e APP-13 (CPM).

### APP-08 — `Result<T>` triplicato tra i Business

**Problema.** Tre copie quasi identiche della classe `Result<T>`:

- `src/Businesses/Kin.KinHub.Core.Business/Common/Result.cs`
- `src/Businesses/Kin.KinHub.Identity.Business/Common/Result.cs`
- `src/Businesses/Kin.KinHub.KinList.Business/Common/Result.cs`

mentre `IResult<T>` e `ResultStatus` vivono già in `src/Shared/Kin.KinHub.Shared.Kernel`.

Oltre alla duplicazione, c'è una **divergenza di comportamento**: la factory `Unauthorized` usa codice `"forbidden"` in Core e KinList (righe 29/19 rispettivamente), ma `"authentication_required"` in Identity (riga 29):

```csharp
// Core.Business/Common/Result.cs:29 e KinList.Business/Common/Result.cs:19
public static Result<T> Unauthorized(string message, string code = "forbidden") => ...

// Identity.Business/Common/Result.cs:29
public static Result<T> Unauthorized(string message, string code = "authentication_required") => ...
```

Questa divergenza non è accidentale — riflette la distinzione semantica HTTP 401 (autenticazione mancante) vs 403 (autorizzazione negata) — ma oggi è codificata in tre tipi distinti anziché in un unico punto con la variante esplicitamente parametrizzata.

**Perché è un problema.** Duplicazione (violazione **DRY**) e inconsistenza: qualsiasi modifica alla struttura di `Result<T>` (es. aggiunta di una nuova factory, cambio della firma) va applicata in tre file. La divergenza del codice `Unauthorized` è un dettaglio non documentato che può portare risposte HTTP errate se un caller usa il tipo sbagliato.

**Soluzione Microsoft-style.** Consolidare `Result<T>` in `Shared.Kernel` con factory parametrizzate, secondo **Clean Architecture** (shared kernel) e **DRY**. La distinzione 401/403 diventa esplicita nel tipo o in una factory dedicata:

```csharp
// src/Shared/Kin.KinHub.Shared.Kernel/Common/Result.cs
namespace Kin.KinHub.Shared.Kernel.Common;

public sealed class Result<T> : IResult<T>
{
    public bool IsSuccess => Status is ResultStatus.Success;
    public ResultStatus Status { get; private init; }
    public string? Code { get; private init; }
    public T? Value { get; private init; }
    object? IResult.Value => Value;
    public string? Message { get; private init; }

    private Result() { }

    public static Result<T> Success(T value) => new() { Status = ResultStatus.Success, Value = value };
    public static Result<T> NotFound(string message, string code = "not_found") => new() { Status = ResultStatus.NotFound, Message = message, Code = code };
    public static Result<T> Conflict(string message, string code = "conflict") => new() { Status = ResultStatus.Conflict, Message = message, Code = code };
    public static Result<T> ValidationError(string message, string code = "validation_error") => new() { Status = ResultStatus.ValidationError, Message = message, Code = code };
    public static Result<T> UnprocessableEntity(string message, string code = "unprocessable_entity") => new() { Status = ResultStatus.UnprocessableEntity, Message = message, Code = code };

    // Forbidden (403): l'utente autenticato non ha il permesso
    public static Result<T> Forbidden(string message, string code = "forbidden") => new() { Status = ResultStatus.Unauthorized, Message = message, Code = code };
    // Unauthenticated (401): autenticazione mancante o non valida
    public static Result<T> Unauthenticated(string message, string code = "authentication_required") => new() { Status = ResultStatus.Unauthorized, Message = message, Code = code };

    public static Result<T> ServiceUnavailable(string message, string code = "service_unavailable") => new() { Status = ResultStatus.ServiceUnavailable, Message = message, Code = code };
    public static Result<T> UnexpectedError(string message, string code = "unexpected_error") => new() { Status = ResultStatus.UnexpectedError, Message = message, Code = code };
}
```

**Impatto.** Layer: tutti i Business + Shared.Kernel. *Breaking change* di namespace: i `using` che referenziano `Kin.KinHub.Core.Business.Common`, `Kin.KinHub.Identity.Business.Common`, `Kin.KinHub.KinList.Business.Common` per `Result<T>` vanno aggiornati a `Kin.KinHub.Shared.Kernel.Common`. I call-site di `Result<T>.Unauthorized` vanno disambiguati in `Forbidden` o `Unauthenticated` secondo il contesto. Da fare insieme ad APP-09. Test da aggiornare: quelli che verificano codici errore su `Unauthorized`.

### APP-09 — Switch di mappatura HTTP duplicato

**Problema.** Il pattern `switch` su `ResultStatus` che mappa verso `IActionResult` è duplicato in due partial class:

- `src/Presentations/Kin.KinHub.Shared.Api/Common/HttpResultMapper.cs:9-19` — mappatura per `Core.Business.Common.Result<T>` (Unauthorized → 403 Forbidden)
- `src/Presentations/Kin.KinHub.Shared.Api/Common/IdentityHttpResultMapper.cs:9-19` — mappatura per `Identity.Business.Common.Result<T>` (Unauthorized → 401 Unauthorized)

Le due partial class differiscono soltanto nel tipo del parametro e nel `case Unauthorized` (rispettivamente `ObjectResult{StatusCode=403}` vs `UnauthorizedObjectResult`).

`src/Presentations/Kin.KinHub.KinList.Api/Common/KinListHttpResultMapper.cs` ha già risolto il problema delegando a `SharedHttpResultMapper` con il parametro `unauthorizedIsForbidden`, ma i due partial non hanno ancora ricevuto lo stesso trattamento.

**Perché è un problema.** Duplicazione (**DRY**): la logica di mappatura deve essere modificata in due punti se cambia la struttura della risposta (es. aggiunta di un nuovo `ResultStatus`). Il consolidamento di APP-08 (`Result<T>` in `Shared.Kernel`) rende ancora più evidente questa ridondanza: una volta che `Result<T>` è unico, il tipo del parametro non è più un discriminante.

**Soluzione Microsoft-style.** Una volta completato APP-08, i due partial class possono essere sostituiti da overload che delegano a `SharedHttpResultMapper`, sul modello già adottato da `KinListHttpResultMapper`:

```csharp
// HttpResultMapper.cs — dopo APP-08
public static IActionResult ToActionResult<T>(Result<T> result) =>
    SharedHttpResultMapper.ToActionResult(result, unauthorizedIsForbidden: true);  // Core/KinList: Unauthorized → 403

public static IActionResult ToActionResult<T>(IdentityResult result) =>
    SharedHttpResultMapper.ToActionResult(result, unauthorizedIsForbidden: false); // Identity: Unauthorized → 401
```

Oppure, dopo la convergenza ad APP-14, un'unica firma tipizzata per `IResult<T>` con il flag esplicito.

**Impatto.** Layer: Presentation. Dipende da APP-08. Nessun *breaking change* esterno (i codici HTTP restano invariati). Test da aggiornare: smoke test sui codici di risposta per i casi `Unauthorized`.

### APP-10 — Guard clause / controllo ownership ancora inline

**Problema.** Il pattern null-check + validazione `FamilyId` + ETag è **parzialmente estratto** in helper privati coesi in `KinListService`:

- `ValidateListMutation` (riga 475-493) — usato da `UpdateAsync`, `DeleteAsync`, `AddItemAsync`, `BulkConfirmItemsAsync`, `UpdateItemAsync`, `DeleteItemAsync`.
- `GetItemForMutationAsync` (riga ~447-473) — usato da `UpdateItemAsync`, `DeleteItemAsync`.

**Ma** i seguenti metodi **aggirano l'helper** replicando le stesse tre guard inline:

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs:209-226` (`RestoreAsync`):

```csharp
var list = await _listRepository.GetByIdAsync(listId, ct);
if (list is null)
    return Result<KinListDetailResponse>.NotFound("List not found.");
if (list.FamilyId != familyId)
    return Result<KinListDetailResponse>.Unauthorized("...");
if (!MatchesEtag(list.Version, ifMatch))
    return Result<KinListDetailResponse>.Conflict("...", "etag_conflict");
```

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListService.cs:383-408` (`RestoreItemAsync`) — stesse tre guard, più la null-check sull'item.

`src/Businesses/Kin.KinHub.KinList.Business/KinListFeature/Services/KinListAudioService.cs` — pattern `null + FamilyId` ripetuto in `CompleteAudioOperationUploadAsync` (righe 125-133), `GetAudioOperationAsync` (165-175), `DeleteAudioOperationAsync` (188-198): ogni metodo ricarica l'operazione e controlla `FamilyId` inline.

**Perché è un problema.** La pattern già corretta (`ValidateListMutation`) esiste ma non viene applicata uniformemente: chi legge `RestoreAsync` non vede lo stesso idioma degli altri metodi. Se il comportamento del null-check cambia (es. `IsDeleted` va trattato diversamente nel restore), va aggiornato in più punti.

**Soluzione Microsoft-style.** Estendere `ValidateListMutation` per gestire il caso `list.IsDeleted == true` come `NotFound` o come caso legittimo (a seconda della semantica del restore), e farlo usare anche da `RestoreAsync` / `RestoreItemAsync`. Per `KinListAudioService` estrarre un helper privato analogo (`ValidateAudioOperationOwnership`):

```csharp
private async Task<(AudioProcessingOperation? Op, Result<AudioProcessingOperationResponse>? Error)>
    LoadOwnedOperationAsync(Guid operationId, Guid familyId, CancellationToken ct)
{
    var operation = await _audioOperationRepository.GetByIdAsync(operationId, ct);
    if (operation is null)
        return (null, Result<AudioProcessingOperationResponse>.NotFound("Audio operation not found."));
    if (operation.FamilyId != familyId)
        return (null, Result<AudioProcessingOperationResponse>.Unauthorized("..."));
    return (operation, null);
}
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change* pubblico. Test: i casi di `RestoreAsync` e i metodi audio ownership devono restare coperti.

### APP-11 — `catch (Exception)` che ingoia lo stack trace

**Problema.** `KinHubServiceService` avvolge ogni metodo pubblico in un `try/catch (Exception)` generico che restituisce solo `ex.Message`, perdendo lo stack trace e non producendo alcun log strutturato.

`src/Businesses/Kin.KinHub.Core.Business/FamilyFeature/Services/KinHubServiceService.cs:41`

```csharp
catch (Exception ex)
{
    return Result<IReadOnlyList<KinHubServiceDto>>.UnexpectedError(ex.Message);
}
```

Le stesse righe si ripetono a `:77` e `:138`. Analogamente in `KinListAudioService.ProcessAudioOperationAsync` (riga ~306):

```csharp
catch (Exception ex)
{
    operation = await RequeueOperationAsync(operation, cancellationToken);
    return Result<AudioProcessingOperationResponse>.ServiceUnavailable(ex.Message, "audio_processing_unexpected_error");
}
```

**Perché è un problema.** Anti-pattern: `ex.Message` senza stack trace rende il troubleshooting impraticabile in produzione. Non c'è alcun log strutturato, quindi l'eccezione sparisce dopo essere stata avvolta nel `Result`. Viola il principio di **Operational Excellence** (WAF): le eccezioni inattese devono essere osservabili.

**Nota.** Per `KinListAudioService` il `catch` generico è in parte giustificato (il processore audio non deve crashare il worker), ma **richiede comunque logging strutturato** dell'eccezione originale prima di restituire il result di errore.

**Soluzione Microsoft-style.** Rimuovere il `catch (Exception)` da `KinHubServiceService` (i metodi del repository non devono lanciare eccezioni di dominio non gestite — lasciar propagare e gestire a livello di middleware globale, o catturare solo le eccezioni attese). Per `KinListAudioService` aggiungere logging strutturato tramite `ILogger<T>` iniettato:

```csharp
// KinHubServiceService — rimuovere il try/catch; la pipeline gestisce le eccezioni non attese
public async Task<Result<IReadOnlyList<KinHubServiceDto>>> GetAllServicesAsync(CancellationToken ct = default)
{
    var services = await _kinHubServiceRepository.GetAllAsync();
    return Result<IReadOnlyList<KinHubServiceDto>>.Success(services.Select(MapToDto).ToList());
}

// KinListAudioService — aggiungere log prima di swallowarlo
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing audio operation {OperationId}", operation.Id);
    operation = await RequeueOperationAsync(operation, cancellationToken);
    return Result<AudioProcessingOperationResponse>.ServiceUnavailable("...", "audio_processing_unexpected_error");
}
```

**Impatto.** Layer: Business (Core + KinList). Nessun *breaking change* pubblico. Test: i test di `KinHubServiceService` che oggi verificano la gestione dell'eccezione vanno aggiornati.

### APP-12 — Inconsistenza di stile diffusa

**Problema.** Diverse convenzioni sono applicate in modo non uniforme nell'intera codebase:

1. **`sealed`**: applicato alle service class (`KinListService`, `KinHubServiceService`) ma assente su alcuni handler e repository (es. `GetFamilyHandler` non sealed, `RecipeStepRepository` non sealed). Manca una regola coerente.
2. **DTO mutabilità**: Core/Identity usano `init` + `required` sui DTO di response; KinList (`KinListContracts.cs`) usa `set` e valori di default, rendendo i DTO mutabili dopo la costruzione.
3. **Null-forgiving `!`**: overuse in diversi punti dove la nullabilità è già garantita dalla guard clause precedente, oppure usato per sopprimere warning invece di correggere il tipo.
4. **Expression-bodied vs block**: mix non sistematico tra lambda a singola espressione e metodi a blocco, anche per metodi identicamente semplici.

**Perché è un problema.** Inconsistenza (**manutenibilità**): un codice disomogeneo aumenta il cognitive load, rallenta il code review e favorisce l'introduzione di nuove inconsistenze per imitazione locale. Si aggancia ad APP-07 (enforcing via `.editorconfig` + analyzer).

**Soluzione Microsoft-style.** Definire le regole in `.editorconfig` (APP-07) con `dotnet_diagnostic` per le regole analyzer rilevanti (es. `CA1852` per `sealed`, `IDE0002` per null-forgiving non necessario) e applicarle via `EnforceCodeStyleInBuild`. Allineare i DTO di KinList a `init` + `required` per coerenza con gli altri contesti.

**Impatto.** Layer: trasversale. Nessun *breaking change* funzionale. Da introdurre gradualmente per evitare di bloccare la build con `TreatWarningsAsErrors`.

### APP-13 — Versioni pacchetti disallineate, nessun Central Package Management

**Problema.** Le versioni dei pacchetti NuGet sono definite nei singoli `.csproj` con disallineamenti rilevati:

| Pacchetto | Progetto | Versione |
| --- | --- | --- |
| `Mapster` | `Kin.KinHub.Core.Business.csproj:17` | `7.*` |
| `Mapster` | `Kin.KinHub.Core.PostgreSql.csproj:16` | `10.0.7` |
| `Mapster` | `Kin.KinHub.Identity.PostgreSql.csproj:15` | `10.0.7` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `Kin.KinHub.Core.Business.csproj:18` | `9.0.4` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `Kin.KinHub.Identity.Business.csproj:18` | `8.0.2` |

Il disallineamento di `Mapster` tra Business (`7.*`) e Infrastructure (`10.0.7`) è particolarmente rischioso: se i due layer vengono risolti con versioni diverse nel processo di build, i type-mapper generati possono produrre comportamenti divergenti o errori di runtime difficili da diagnosticare.

**Perché è un problema.** Manutenibilità e rischio: senza un punto centrale, l'aggiornamento di un pacchetto richiede modifiche in più `.csproj`, con alta probabilità di dimenticare un progetto e creare inconsistenze. Contro le **NuGet Central Package Management best practices**.

**Soluzione Microsoft-style.** Introdurre `Directory.Packages.props` alla root con la lista di versioni centralizzata (si aggancia ad APP-07):

```xml
<!-- Directory.Packages.props (root) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Mapster" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.4" />
    <!-- ... tutte le altre versioni ... -->
  </ItemGroup>
</Project>
```

Nei singoli `.csproj` rimuovere l'attributo `Version` dai `PackageReference`.

**Impatto.** Layer: build/solution. *Breaking change* di build: verificare che la versione unificata di `Mapster` (`10.0.7`) sia compatibile con `Core.Business` (attualmente `7.*` — il salto è major, potenzialmente breaking). Test: eseguire la suite completa dopo la migrazione CPM.

### APP-14 — Divergenza architetturale tra contesti

**Problema.** I tre contesti del dominio usano pattern architetturali significativamente diversi:

| Aspetto | Core / Identity | KinList |
| --- | --- | --- |
| Coordinamento | Handler CQRS (`ICreateXHandler` + `HandleAsync`) | Facade service (`KinListService`) |
| Repository | Base `IRepository<T,K>` con `FindByIdAsync` ecc. | Repository specifici senza base comune |
| Mapping | Mapster configurato | Mapper manuale (`KinListMapper`) |
| Gestione errori | `try/catch` su eccezioni di dominio → Result | `Result<T>` return senza eccezioni di dominio |
| Eccezioni dominio | `EntityNotFoundException`, `DomainValidationException` | Non usate |

**Perché è un problema.** Inconsistenza e manutenibilità (**Alta**): un singolo manutentore che si sposta tra contesti deve tenere in testa due modelli mentali diversi per fare la stessa cosa. L'onboarding (anche proprio) è più lento. Il rischio di applicare il pattern sbagliato al contesto sbagliato cresce con il tempo. Questa è la radice di molte delle altre inconsistenze censite (APP-08, APP-09, APP-12).

**Decisione.** Scegliere un pattern target unico e pianificare la **convergenza incrementale**, un contesto alla volta, mantenendo la build verde a ogni passo. La raccomandazione è di convergere verso il pattern **KinList** (facade service + Result<T> senza eccezioni di dominio) per i nuovi contesti, per i seguenti motivi:
- È più semplice e diretto (meno layer) per una codebase a singolo manutentore.
- Evita la proliferazione di eccezioni di dominio che devono essere catturate a ogni livello.
- Si aggancia naturalmente ad APP-08 (Result unificato in Shared.Kernel).

**Roadmap di convergenza (incrementale):**

1. **Fase 0 — Definizione**: documentare il pattern target in CLAUDE.md o in un ADR dedicato. Nessuna modifica al codice.
2. **Fase 1 — Condivisione tipi base**: completare APP-01 (eccezioni) e APP-08 (Result) in Shared.Kernel. Entrambi i contesti usano gli stessi tipi base.
3. **Fase 2 — Allineamento Core**: migrare `KinHubServiceService` da `try/catch` a Result diretto (APP-11). Valutare se mantenere gli handler CQRS o semplificarli a service facade.
4. **Fase 3 — Allineamento Identity**: identica a Fase 2 per il contesto Identity.
5. **Fase 4 — Revisione**: dopo la convergenza, verificare che i tre contesti usino la stessa struttura e aggiornare questo documento.

**Impatto.** Layer: trasversale, tutti i contesti. Effort **L** (più giorni, distribuiti nel tempo). Nessun *breaking change* esterno se fatto incrementalmente. Criterio: build verde a ogni fase; test di regressione per i contratti HTTP.

### APP-15 — Metodi orchestratori lunghi + logica non commentata

**Problema.** Due metodi superano abbondantemente le 50 righe e contengono logica non documentata:

- `KinListService.CreateAsync` (~82 righe, `KinListService.cs:89-171`): mescola deduplicazione, idempotency check (righe 100-118), creazione lista, creazione item, serializzazione response per l'idempotency record.
- `KinListAudioService.ProcessAudioOperationAsync` (~98 righe, `KinListAudioService.cs:212-310`): include la claim atomica dell'operazione (`TryStartProcessingAsync`, riga ~235), parsing audio, normalizzazione, gestione fallback e requeue.

La logica dell'idempotency check (righe 100-118 di `KinListService`) e il double-check post-`TryStartProcessingAsync` (righe ~236-250 di `KinListAudioService`) sono invarianti non ovvi (race condition, idempotenza under-concurrent-writes) che non hanno alcun commento.

**Perché è un problema.** Leggibilità: un metodo da 98 righe con 4 exit-point distinti richiede uno sforzo cognitivo elevato per essere compreso e modificato in sicurezza.

**Soluzione Microsoft-style.** Decomporre in sotto-metodi privati con nomi espliciti, e aggiungere un commento breve solo sugli invarianti non ovvi:

```csharp
// TryStartProcessingAsync è atomica (ottimistic lock sul DB).
// Se restituisce null, l'operazione è già stata claimata da un altro worker.
var claimedOperation = await _audioOperationRepository.TryStartProcessingAsync(...);
```

**Impatto.** Layer: Business (KinList). Nessun *breaking change*. Test: nessuna modifica attesa se la logica non cambia.

## 5. Infrastruttura & CI/CD

> **Contesto budget.** Questo è un progetto personale a uso familiare con budget **≤ 30 €/mese**. Le scelte infrastrutturali corrette a questa scala sono **free tier**, **scale-to-zero** e **tier Burstable**. `minReplicas: 0` e il tier Burstable per PostgreSQL **non sono difetti**: sono le scelte corrette per questo budget e questa scala; il cold-start è un compromesso accettabile per un'app familiare. Lo standard WAF di riferimento prevalente è **Cost Optimization**. Le raccomandazioni cloud costose sono state deliberatamente escluse e sono elencate nella sezione 7.

### IAC-01 — deploy_dev e deploy_prod quasi identici

**Problema.** I due job di deploy in `.github/workflows/backend.yml` sono **quasi identici** (~270 righe ciascuno), differenziandosi essenzialmente per `environment:` e `needs:`.

- `deploy_dev` — `.github/workflows/backend.yml:248-517` (`environment: dev`)
- `deploy_prod` — `.github/workflows/backend.yml:519-788` (`environment: prod`)

La funzione bash `resolve_rollout_image()` è **copiata due volte**: `backend.yml:344-363` e `backend.yml:615-634`. Anche gli step *Validate deployment inputs*, *Deploy infrastructure*, *Run KinList expand/contract migration* e *Roll out backend revisions* sono duplicati byte-per-byte, inclusa la lunga query KQL (`backend.yml:480` e `backend.yml:751`).

**Perché è un problema.** Duplicazione massiva (violazione **DRY**) e rischio **Operational Excellence** (WAF): una correzione (es. alla logica di migrazione o alla query di diagnostica) va applicata in due punti; è facile che dev e prod divergano silenziosamente.

**Soluzione Microsoft-style.** Estrarre un **reusable workflow** (`workflow_call`) parametrizzato per ambiente, invocato da `deploy_dev` e `deploy_prod`. È la pratica GitHub Actions raccomandata e sostiene il pilastro **Operational Excellence** del **WAF** (processi di rilascio ripetibili e a fonte unica).

```yaml
# .github/workflows/deploy-backend.yml (reusable)
on:
  workflow_call:
    inputs:
      environment: { required: true, type: string }
# ... tutti gli step una sola volta, environment: ${{ inputs.environment }}

# backend.yml
deploy_dev:
  uses: ./.github/workflows/deploy-backend.yml
  with: { environment: dev }
deploy_prod:
  needs: [deploy_dev]
  uses: ./.github/workflows/deploy-backend.yml
  with: { environment: prod }
```

**Impatto.** Area: CI/CD. Nessun *breaking change* runtime sull'app. Criterio di verifica: un deploy dev+prod completo deve produrre lo stesso risultato di oggi.

### IAC-02 — main.bicep monolitico senza moduli

**Problema.** `ops/iac/main.bicep` è un **unico file da 1.678 righe** che dichiara *tutte* le risorse inline: Key Vault, PostgreSQL, OpenAI, Speech, Storage, Log Analytics, App Insights, 4 Static Web Apps, Container Apps Environment, 4 Container Apps, il job di migrazione e ~11 role assignment. **Zero `module`** (verificato: nessuna dichiarazione `module` nel file). Le 4 Static Web Apps (`main.bicep:455-524`) e i Container Apps sono ripetuti in blocchi quasi identici.

**Perché è un problema.** Anti-pattern di IaC monolitica: difficile da leggere e da modificare in punti puntuali senza rischiare side effect. Contro le **Bicep best practices** (composizione via moduli). Per un singolo manutentore la manutenibilità è il valore principale.

> **Nota di contesto.** La severità è stata declassata da Alta a **Bassa**: la modularizzazione è gratuita e utile, ma non urgente per un singolo manutentore su una codebase stabile. Va pianificata come miglioramento di manutenibilità quando si tocca comunque l'area IaC.

**Soluzione Microsoft-style.** Applicare le **Bicep best practices**: scomporre in moduli per dominio (`data`, `ai`, `frontend`, `compute`, `observability`) e sostituire i blocchi ripetuti con un modulo iterato (`for`).

```bicep
// main.bicep
var staticWebApps = [
  { name: coreStaticWebAppName, origin: coreFrontendOrigin }
  { name: identityStaticWebAppName, origin: identityFrontendOrigin }
  { name: kinRecipeStaticWebAppName, origin: kinRecipeFrontendOrigin }
  { name: kinListStaticWebAppName, origin: kinListFrontendOrigin }
]

module swa 'modules/static-web-app.bicep' = [for app in staticWebApps: {
  name: 'swa-${app.name}'
  params: { name: app.name, location: staticWebAppLocation }
}]
```

**Impatto.** Area: IaC. Nessun *breaking change* funzionale se i nomi risorsa restano invariati. Criterio di verifica: `az deployment group what-if` non deve mostrare modifiche indesiderate dopo la modularizzazione.

### IAC-03 — Role definition ID come costanti non documentate

**Problema.** Gli ID dei ruoli built-in sono **già estratti in variabili** ma **non documentati**.

`ops/iac/main.bicep:236-241`

```bicep
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageQueueDataMessageProcessorRoleId = '8a0f0c08-91a1-4084-bc3d-661d67233fed'
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'
var cognitiveServicesOpenAiUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
```

> **Nota di correzione al finding grezzo.** Il finding originale parlava di "13 UUID hardcoded" da estrarre in costanti. Alla riverifica gli ID sono **6 costanti `var` già centralizzate** (non sparse ai punti d'uso, che li referenziano via `subscriptionResourceId(...)`). Il problema residuo è quindi minore: mancano **documentazione** (quale ruolo built-in) e `@description`. Severità confermata bassa.

**Perché è un problema.** Complicazione minore: un GUID nudo non è auto-esplicativo; senza commento o descrizione è difficile verificare che corrisponda al ruolo atteso.

**Soluzione Microsoft-style.** Aggiungere un commento con il nome del ruolo built-in accanto a ogni GUID (allineato alla documentazione Azure RBAC built-in roles), o spostarli in un modulo `roles.bicep` decorato.

```bicep
// Key Vault Secrets User — https://learn.microsoft.com/azure/role-based-access-control/built-in-roles
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
```

**Impatto.** Area: IaC. Nessun *breaking change* (solo commenti/documentazione).

### IAC-04 — any({...}) sulle Static Web Apps

**Problema.** Le 4 Static Web Apps avvolgono le proprietà in `any({...})`, perdendo il *type-checking* di Bicep.

`ops/iac/main.bicep:462` (e identicamente `:480`, `:498`, `:516`)

```bicep
properties: any({
  allowConfigFileUpdates: true
  branch: staticSitesBranch
  deploymentAuthPolicy: staticSitesDeploymentAuthPolicy
  provider: staticSitesProvider
  publicNetworkAccess: 'Enabled'
  repositoryUrl: staticSitesRepositoryUrl
  stagingEnvironmentPolicy: 'Disabled'
})
```

**Perché è un problema.** Anti-pattern (perdita di *type-safety*): `any()` disabilita la validazione dello schema, mascherando refusi o proprietà non valide fino al deploy. Contro le **Bicep best practices** (evitare `any()` salvo necessità reale).

**Soluzione Microsoft-style.** Rimuovere `any()` e affidarsi allo schema tipizzato della risorsa; se una proprietà valida non è nello schema della versione API, aggiornare l'`apiVersion` invece di sopprimere il controllo. Idealmente estrarre il tutto nel modulo `static-web-app.bicep` di IAC-02.

**Impatto.** Area: IaC. Nessun *breaking change* atteso; potrebbe emergere un errore di compilazione se una proprietà non è supportata (da correggere all'origine).

### IAC-06 — listKeys() in-template invece di managed identity

**Problema.** Il template legge chiavi di accesso a runtime con `listKeys()`:

- OpenAI — `main.bicep:411` `openAiAccount.listKeys().key1`
- Speech — `main.bicep:427` `speechAccount.listKeys().key1`
- Log Analytics — `main.bicep:535` `logAnalyticsWorkspace.listKeys().primarySharedKey`

**Perché è un problema.** Gap **Security** (WAF): l'uso di chiavi condivise contraddice la strategia *managed identity end-to-end*. Le identity gestite e i role assignment sono **già presenti** nel template (`main.bicep:1374, 1384, 1404` assegnano `Cognitive Services User` / `Cognitive Services OpenAI User`), quindi le chiavi sono ridondanti per OpenAI/Speech. **La managed identity è gratuita** e strettamente migliore: nessuna chiave da ruotare, nessun segreto in chiaro nelle variabili d'ambiente.

> **Priorità elevata nonostante il budget.** Questo hardening non ha costo aggiuntivo e riduce la superficie di attacco: è la scelta corretta a qualsiasi scala.

**Soluzione Microsoft-style.** Completare il modello **managed identity end-to-end** (Security, WAF): le app usano `DefaultAzureCredential` con i role assignment esistenti; rimuovere il passaggio di chiavi via `listKeys()` nelle variabili d'ambiente. Per Log Analytics preferire l'ingestion basata su identity/DCR ove possibile.

**Impatto.** Area: IaC + configurazione app. *Breaking change*: le app devono già essere configurate per l'autenticazione via identity (verificare che l'infrastruttura AI dell'app supporti la credenziale). Test: smoke test end-to-end delle integrazioni AI dopo la rimozione delle chiavi.

### IAC-07 — PostgreSQL: firewall aperto e configurazione adatta al budget

**Problema.** La regola firewall apre l'accesso a tutte le risorse Azure — `main.bicep:318-324`:

```bicep
name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
properties: {
  startIpAddress: '0.0.0.0'
  endIpAddress: '0.0.0.0'
}
```

Questa regola consente la connessione da qualunque risorsa Azure nella sottoscrizione Microsoft (non solo dalla propria), rappresentando un vettore di accesso non necessario.

**Cosa NON è un problema a questo budget.** La configurazione attuale di PostgreSQL — tier `Burstable` (`Standard_B1ms`), `highAvailability.mode: 'Disabled'`, `geoRedundantBackup: 'Disabled'`, `backupRetentionDays: 7` — e i Container Apps con `minReplicas: 0` (`main.bicep:858, 1023, 1311, 1592`) **sono scelte corrette** per questo scenario. Il cold-start da scale-to-zero è un compromesso accettabile. HA zonale e General Purpose costerebbero >100 €/mese aggiuntivi — fuori budget.

**Soluzione Microsoft-style.** L'unica modifica raccomandata è **restringere la regola firewall**: rimuovere la regola `0.0.0.0` e sostituirla con i range IP espliciti dei Container Apps (o usare VNet integration senza Private Endpoint, che può essere a basso costo). L'hardening minimo gratuito:

```bicep
// Rimuovere la regola AllowAllAzureServices
// Sostituire con gli IP effettivi dell'ambiente Container Apps, o usare
// l'integrazione VNet del Container Apps Environment (inclusa nel piano)
```

**Impatto.** Area: IaC. Nessun impatto funzionale se gli IP sono configurati correttamente. Basso effort (modifica al Bicep). Può richiedere il recupero degli IP di uscita dei Container Apps.

### IAC-08 — Nessun parameter file per ambiente versionato

**Problema.** Sotto `ops/iac/` esistono **solo** file *sample*: `main.sample.bicepparam` e `managed-identities.sample.bicepparam`. Non c'è alcun `*.bicepparam` per ambiente (dev/prod) versionato; i valori sono passati inline dal workflow tramite `deploy_parameters=(...)` (`backend.yml:384-419`).

> **Nota di riverifica.** Il commit recente "Add Bicep parameter files" ha aggiunto i file **`.sample.bicepparam`** (template), non i parameter file per ambiente. Il finding resta quindi **valido**: mancano i file per-ambiente.

**Perché è un problema.** Complicazione e gap **Operational Excellence** (WAF): la configurazione d'ambiente vive dispersa nelle variabili del workflow, non in file dichiarativi versionati e revisionabili.

**Soluzione Microsoft-style.** Adottare i **Bicep parameter files** per ambiente (`main.dev.bicepparam`, `main.prod.bicepparam`), con i valori non segreti versionati e i segreti risolti da Key Vault (`getSecret`). Il workflow passa `--parameters main.<env>.bicepparam`.

```bicep
// main.dev.bicepparam
using './main.bicep'
param location = 'westeurope'
param minReplicas = 0      // scale-to-zero: corretto per dev e prod a questo budget

// main.prod.bicepparam
using './main.bicep'
param location = 'westeurope'
param minReplicas = 0      // idem: cold-start accettabile per app familiare
```

**Impatto.** Area: IaC + CI/CD. Nessun *breaking change* runtime. Criterio: `what-if` invariato a parità di valori.

### IAC-09 — Assenza di diagnostic settings

**Problema.** Nessuna risorsa `Microsoft.Insights/diagnosticSettings` è dichiarata in `main.bicep` (verificato: nessuna occorrenza). Le risorse (Key Vault, PostgreSQL, Storage, OpenAI, Speech) non instradano log/metriche verso il Log Analytics workspace che pure è creato nel template.

**Perché è un problema (a costo contenuto).** Gap **Operational Excellence** (WAF): senza diagnostic settings mancano audit log centralizzati. Il Log Analytics workspace è già creato e include un **free tier di ingestion (5 GB/giorno)** sufficiente per i log di audit di un'app familiare. L'ingestion ha un costo solo oltre questa soglia.

> **Priorità bassa / cost-aware.** Aggiungere solo i log di audit minimi (categoria `audit` per Key Vault, log di connessione per PostgreSQL). Evitare `AllMetrics` su tutte le risorse per non superare il free tier involontariamente.

**Soluzione Microsoft-style.** Aggiungere `diagnosticSettings` per le risorse chiave con categoria `audit` (non `AllMetrics`), instradando al Log Analytics workspace esistente. Da realizzare idealmente come modulo riusabile insieme a IAC-02.

```bicep
resource kvDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-law'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [ { categoryGroup: 'audit', enabled: true } ]
    // metrics omesse per contenere l'ingestion nel free tier
  }
}
```

**Impatto.** Area: IaC. Nessun *breaking change*. Impatto su costo: contenuto se limitato ad audit. Criterio: log visibili in Log Analytics dopo il deploy.

## 6. Roadmap prioritizzata

### Ondata 1 — Consolidamenti a costo zero (quick wins)

| ID | Dipendenze | Criteri d'accettazione |
| --- | --- | --- |
| APP-04 | — | Un solo helper di normalizzazione; i 3 file lo usano; test verdi. |
| APP-08 | — | `Result<T>` unico in `Shared.Kernel`; factory `Forbidden`/`Unauthenticated` distinte; namespace aggiornati; test verdi. |
| APP-09 | APP-08 | Partial class `HttpResultMapper` delegano a `SharedHttpResultMapper`; codici HTTP invariati. |
| APP-10 | — | `RestoreAsync`/`RestoreItemAsync` usano `ValidateListMutation`; helper audio estratto; test verdi. |
| APP-07 | — | `Directory.Build.props` + `.editorconfig` a root; solution compila; proprietà rimosse dai `.csproj`. |
| APP-12 | APP-07 | Regole `sealed`/`init`/null-forgiving in `.editorconfig`; applicate via analyzer; nessun nuovo avviso. |
| APP-13 | APP-07 | `Directory.Packages.props` a root; versione Mapster unificata a `10.0.7`; build verde. |
| IAC-03 | — | Ogni GUID di ruolo ha commento; `az bicep build` verde. |
| IAC-04 | — | `any()` rimosso dalle 4 Static Web Apps; `az bicep build` verde. |
| IAC-08 | — | `main.dev.bicepparam` / `main.prod.bicepparam` versionati; `what-if` invariato. |
| IAC-06 | — | Chiavi `listKeys()` rimosse per OpenAI/Speech; app funzionanti via managed identity. |
| IAC-07 | IAC-08 | Regola firewall `0.0.0.0` rimossa; sostituita con IP espliciti o VNet integration. |

### Ondata 2 — Strutturali (refactoring architetturale, breaking interni)

| ID | Dipendenze | Criteri d'accettazione |
| --- | --- | --- |
| APP-01 | APP-08 | Eccezioni consolidate in `Shared.Kernel`; namespace aggiornati; test verdi. |
| APP-02 | APP-03 | Costruttore di `KinListService` senza `?? new`; `IKinListAudioService` iniettato; test aggiornati. |
| APP-03 | — | `IKinListService` senza metodi audio; controller audio su `IKinListAudioService`. |
| APP-05 | — | `IEtagProvider` iniettato; mapper senza logica ETag. |
| APP-06 | — | `ICorrelationIdProvider` iniettabile; `ActivitySource` invariato. |
| APP-11 | — | `catch (Exception)` rimosso da `KinHubServiceService`; `ILogger` iniettato in `KinListAudioService`. |
| APP-15 | — | Metodi lunghi decomposti; invarianti di race condition commentati. |
| IAC-01 | IAC-08 | Reusable workflow unico; dev+prod deployano con lo stesso codice. |
| IAC-02 | IAC-04 | Moduli per dominio; blocchi ripetuti sostituiti da `for`; `what-if` invariato. |
| IAC-09 | IAC-02 | Diagnostic settings (audit) verso Log Analytics per Key Vault e PostgreSQL. |

> APP-03 va completato **prima o insieme** ad APP-02 (l'iniezione diretta di `IKinListAudioService` presuppone la separazione delle interfacce). IAC-01 beneficia di IAC-08 (parametri per ambiente) per non reintrodurre valori inline.

### Ondata 3 — Convergenza architetturale (incrementale)

| ID | Dipendenze | Criteri d'accettazione |
| --- | --- | --- |
| APP-14 — Fase 0 | — | Pattern target documentato in CLAUDE.md o ADR. |
| APP-14 — Fase 1 | APP-01, APP-08 | Tipi base condivisi (eccezioni + Result) già completati nelle onde precedenti. |
| APP-14 — Fase 2 | APP-11 | `KinHubServiceService` e handler Core migrati a Result senza catch generico; build verde. |
| APP-14 — Fase 3 | APP-14 Fase 2 | Contesto Identity allineato; un solo pattern visibile nei tre contesti. |

> La convergenza architetturale è **incrementale**: nessuna fase è un big-bang. La build deve restare verde dopo ogni fase.

## 7. Rischi e non-goal

**Non-goal (cosa NON si tocca).**

- **Comportamento funzionale**: nessun refactoring deve cambiare i contratti HTTP, i codici `ResultStatus`, il contratto ETag/`If-Match`/`Idempotency-Key` o la semantica delle risposte.
- **Frontend** (`*.React`): fuori ambito.
- **Schema del database e migrazioni**: non toccati da questo documento (le modifiche PostgreSQL di IAC-07 riguardano solo il firewall, non lo schema).
- **Nomi delle risorse Azure**: mantenuti invariati durante la modularizzazione (IAC-02) per evitare ricreazioni.
- **Alta disponibilità e geo-ridondanza PostgreSQL** (HA/ZoneRedundant, `geoRedundantBackup: 'Enabled'`): fuori budget (>100 €/mese aggiuntivi). `highAvailability.mode: 'Disabled'` è la scelta corretta.
- **General Purpose tier PostgreSQL**: fuori budget. Il tier Burstable (`Standard_B1ms`) è la scelta corretta.
- **`minReplicas: 1` sui Container Apps**: fuori budget. `minReplicas: 0` (scale-to-zero) è la scelta corretta per questa scala; il cold-start è un compromesso accettabile.
- **Private Endpoint e VNet privata**: fuori budget (~7 €/mese per endpoint × n servizi). L'accesso pubblico con managed identity + firewall ristretto + autenticazione forte è un compromesso accettabile per un'app personale.
- **Central Package Management (CPM) — `Directory.Packages.props`**: incluso nell'Ondata 1 (APP-13), quindi non un non-goal ma pianificato.

**Rischi.**

- **APP-01 / APP-02 / APP-03 / APP-08** sono *breaking change interni*: richiedono aggiornamento coordinato di `using`, registrazioni DI e test. Da fare a ondate, con build verde a ogni passo.
- **APP-13 (Mapster 7→10)**: il salto di major version può introdurre breaking change nelle configurazioni mapper esistenti. Verificare con la suite di test prima di unificare.
- **APP-07** con `TreatWarningsAsErrors` può bloccare la build finché gli avvisi esistenti non sono risolti: introdurre gradualmente.
- **IAC-07 firewall**: restringere la regola `0.0.0.0` richiede conoscere gli IP di uscita dei Container Apps; verificare con `what-if` prima del deploy.

> **Avvertenza documenti stali.** Come per gli altri file in `docs/specs/`, dove un dettaglio non è deducibile con certezza dal codice (es. la topologia di rete effettiva o valori runtime degli ambienti) va considerato **non deducibile con certezza dalla codebase** e verificato sugli ambienti reali. I piani `msrefactor` precedenti risultano in parte già implementati: questo documento riflette lo stato del codice al momento della riverifica.

## 8. Appendice — Checklist convenzioni Microsoft mancanti

| Convenzione | Stato | Riferimento Microsoft |
| --- | --- | --- |
| `.editorconfig` a root | **Mancante** (solo file generati in `obj/`) | .NET code-style / EnforceCodeStyleInBuild |
| `Directory.Build.props` a root | **Mancante** | MSBuild — proprietà condivise |
| `Directory.Packages.props` (Central Package Management) | **Mancante** — pianificato APP-13 | NuGet CPM |
| Analyzer / `TreatWarningsAsErrors` | Non centralizzati | .NET analyzers |
| `Result<T>` unico in `Shared.Kernel` | **Mancante** — pianificato APP-08 | Clean Architecture / DRY |
| Parameter file Bicep per ambiente | **Mancante** (solo `*.sample.bicepparam`) — pianificato IAC-08 | Bicep parameter files |
| Moduli Bicep / AVM | **Mancante** (`main.bicep` monolitico) — pianificato IAC-02 (bassa priorità) | Azure Verified Modules |
| Diagnostic settings (audit) | **Mancante** — pianificato IAC-09 (cost-aware) | WAF — Operational Excellence |
| Managed identity end-to-end | **Parziale** (coesiste con `listKeys()`) — pianificato IAC-06 | WAF — Security |
| Restrizione firewall PostgreSQL | **Mancante** (`0.0.0.0` aperto) — pianificato IAC-07 | WAF — Security |
| Naming & tagging risorse | Da verificare/documentare | Cloud Adoption Framework |
| Reusable workflow CI/CD | **Mancante** (job duplicati) — pianificato IAC-01 | WAF — Operational Excellence |
| Private Endpoint / rete privata | **Non applicabile a questo budget** (~7 €/mese per endpoint) | WAF — Security (escluso per Cost Optimization) |
| HA/General Purpose PostgreSQL | **Non applicabile a questo budget** (>100 €/mese) | WAF — Reliability (escluso per Cost Optimization) |
| `minReplicas ≥ 1` Container Apps | **Non applicabile a questo budget** | WAF — Reliability (escluso per Cost Optimization) |
