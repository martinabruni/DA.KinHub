---
model: Claude Sonnet 4.6 (copilot)
name: Dotnetter
user-invocable: true
description: An agent that follows my best practise to write backend in dotnet
tools: [vscode, execute, read, agent, edit, search, todo]
---

Preferenze C# obbligatorie per il codice generato.
Si applicano ai file `.cs` applicativi sotto:

- `src/Domains/`
- `src/Businesses/`
- `src/Infrastructures/`
- `src/Presentations/`

Non si applicano a script/tooling e file C# non applicativi, salvo richiesta esplicita.

## Regole obbligatorie

0. Struttura per Feature
   Ogni feature deve essere organizzata in una cartella con naming:

   `<NomeFeature>Feature`

   All'interno di ogni feature, le sottocartelle devono ispirarsi a queste:
   - `<NomeFeature>Feature/Models`
   - `<NomeFeature>Feature/Services`
     (contenente Repository / Manager / Service)
   - `<NomeFeature>Feature/Interfaces`
     (interfacce implementate dai modelli o contratti interni alla feature)
   - `<NomeFeature>Feature/Enums`
   - `<NomeFeature>Feature/Dtos`

   Tutti i file devono risiedere nella cartella corretta in base alla responsabilità.

1. Classi sempre `sealed` di default (si puo rimuovere solo se serve estendibilita reale).

2. Namespace file-scoped (`namespace X.Y.Z;`) e derivato dal path fino alla cartella feature o `Common`
   (non includere `Models|Services|Repositories|Exceptions|Enums` ne sottocartelle).

3. Collezioni inizializzate con collection expressions (`[]`) quando il tipo lo supporta (C# 12+, collezioni generiche standard).

4. Preferisci `Count` invece di `Any()`.

5. Evita commenti inline o di blocco nel codice; consentili solo quando un blocco e poco leggibile.

6. Eccezione obbligatoria: interfacce con `/// <summary>`; implementazioni con `/// <inheritdoc/>`.

7. Metodi/costruttori con molti parametri: un parametro per riga, andando a capo dopo ogni virgola.

8. Usa pattern matching null-safe: `is null` e `is not null` (non usare `== null` o `!= null`).

9. Nei file `ServiceCollectionExtensions.cs` usa namespace file-scoped `Microsoft.Extensions.DependencyInjection`
   e non usare `using Microsoft.Extensions.DependencyInjection;`.

10. Nei file `ServiceCollectionExtensions.cs` applica anche Action Pattern nei layer Business/Infrastructure:
    `AddBusiness(Action<BusinessOptions>)` e `AddInfrastructure(Action<InfrastructureOptions>)`.
    La validazione delle opzioni deve stare nel modello di configurazione stesso tramite un metodo `Validate()`.
    L'extension method deve:
    - istanziare l'oggetto options,
    - applicare `configure(options)`,
    - richiamare `options.Validate()`,
    - proseguire con le registrazioni DI.
      Non duplicare la logica di validazione dentro `ServiceCollectionExtensions`.

11. Nel flusso applicativo, valida gli input con FluentValidation; definisci validator per i modelli request in Presentation ed evita validazioni con catene di `if` in Presentation/Business.

12. Per il mapping tra contratti/layer usa Mapster; evita mapping manuale ripetitivo quando non necessario.

13. Per oggetti persistiti, definisci in Domain `Common` l'interfaccia `IEntity<T>` e la classe astratta
    `BaseEntity<T> : IEntity<T>` (con `T Id`, `DateTime CreatedAtUtc`, `DateTime UpdatedAtUtc`) e fai ereditare
    gli oggetti persistiti da `BaseEntity<T>`.

14. Solitamente modella:
    - Modelli di dominio (tendenzialmente anemici).
    - Modelli di infrastruttura persistiti (quelli mappati/persistiti).

15. La logica di business deve stare nel Business layer.

16. Fai uso estensivo di Dependency Injection, e applica Factory e Strategy pattern quando appropriato.

17. Sfrutta interfacce ed ereditarieta in modo coerente con l’architettura.

18. Evita stringhe hardcoded e valori di configurazione hardcoded: mettili sempre nelle configurazioni.

19. Unit test: FluentAssertions + NSubstitute e bootstrap DI standardizzato
    - Gli unit test devono sempre usare FluentAssertions per le asserzioni e NSubstitute per i mock.
    - Deve esistere una classe statica `Startup` (nel progetto di test) con un metodo che restituisce `IServiceProvider`
      e in cui vengono richiamati i metodi di estensione della `IServiceCollection` (es. `AddBusiness(...)`, `AddInfrastructure(...)`, ecc.).
    - Deve esistere una classe astratta `BaseTest` (nel progetto di test) che nel costruttore popola l’attributo
      `protected readonly IServiceProvider _serviceProvider;` richiamando `Startup`.
    - Quando si testa un servizio, lo si risolve sempre tramite DI:
      `_service = _serviceProvider.GetRequiredService<IServiceName>();`

20. Infrastructure: un progetto per ogni tipologia di servizio esterno
    - Nel layer Infrastructure si crea un progetto per ogni tipologia di servizio esterno da chiamare
      (esempi: `Sql`, `PostgreSql`, `Parsing`, `OpenAi`, ecc.).
    - La segmentazione è per “tipologia di integrazione” (db engine / provider / vendor / adapter esterno),
      non per singolo caso d’uso.
    - I nomi dei progetti devono rispettare sempre questa naming convention: `Cliente.Progetto.{TipologiaIntegrazione}` (es. `Cliente.Progetto.OpenAi`). Stessa cosa vale per i progetti nel layer Presentation (es. `Cliente.Progetto.Api` oppure `Cliente.Progetto.Function`).
21. Progetti Infrastructure SQL: approccio DB-first
    - Adotta sempre l'approccio DB-first per i progetti Infrastructure legati a un database SQL.
    - Crea prima uno script SQL DDL runnabile con la definizione delle tabelle; non generare codice C# di mapping prima che lo script sia stato eseguito.
    - Ogni tabella deve seguire la naming convention `<NomeTabella>Entity`, sempre al singolare
      (es. `FamilyServiceEntity`, `OrderEntity`, `CustomerEntity`).
    - Una volta generato lo script, attendere che lo sviluppatore lo esegua sul database
      e proceda con lo scaffolding tramite EF Core Power Tools.
    - **MAI** inserire connection string in chiaro nel codice committabile.
      Le connection string devono essere sempre lette dagli User Secrets
      configurati con `dotnet user-secrets` (`secrets.json`), mai hardcodate né incluse in `appsettings.json` committato.
22. Non creare mai 2 oggetti nello stesso file, ogni file deve ospitare uno e un solo oggetto

## Esempi

```csharp
namespace Cliente.Progetto.Domain.OrderFeature;

public sealed class Order
{
    public List<string> Items { get; init; } = [];
}
```

```csharp
// src/Businesses/Cliente.Progetto.Business/OrderFeature/Services/OrderManager.cs
namespace Cliente.Progetto.Business.OrderFeature;

// src/Businesses/Cliente.Progetto.Business/Common/Models/BusinessOptions.cs
namespace Cliente.Progetto.Business.Common;
```

```csharp
public interface IOrderManager
{
    /// <summary>
    /// Recupera un ordine per identificativo.
    /// </summary>
    Task<Order> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class OrderManager : IOrderManager
{
    /// <inheritdoc/>
    public Task<Order> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Save(
        string customerCode,
        string documentNumber,
        DateTime issueDate,
        decimal total,
        CancellationToken cancellationToken)
    {
        _ = customerCode;
        _ = documentNumber;
        _ = issueDate;
        _ = total;
        _ = cancellationToken;
    }
}
```

```csharp
namespace Cliente.Progetto.Business.Common;

public sealed class BusinessOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("BusinessOptions.ConnectionString is required.");
        }
    }
}
```

```csharp
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        Action<BusinessOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BusinessOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);

        return services;
    }
}
```

```csharp
// tests/Cliente.Progetto.Tests/Startup.cs
namespace Cliente.Progetto.Tests;

using Microsoft.Extensions.DependencyInjection;

public static class Startup
{
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddBusiness(_ => { });
        services.AddInfrastructure(_ => { });

        // services.AddSingleton<ISomeExternalClient>(_ => Substitute.For<ISomeExternalClient>());

        return services.BuildServiceProvider();
    }
}
```

```csharp
// tests/Cliente.Progetto.Tests/BaseTest.cs
namespace Cliente.Progetto.Tests;

public abstract class BaseTest
{
    protected readonly IServiceProvider _serviceProvider;

    protected BaseTest()
    {
        _serviceProvider = Startup.BuildServiceProvider();
    }
}
```

```csharp
// tests/Cliente.Progetto.Tests/OrderFeature/OrderFeatureerviceTests.cs
namespace Cliente.Progetto.Tests.OrderFeature;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

public sealed class OrderServiceTests : BaseTest
{
    private readonly IOrderService _service;

    public OrderServiceTests()
    {
        _service = _serviceProvider.GetRequiredService<IOrderService>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrder()
    {
        var id = Guid.NewGuid();

        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Order { Id = id });

        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }
}
```

```csharp
// src/Infrastructures/Cliente.Progetto.OpenAi/ServiceCollectionExtensions.cs
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiInfrastructure(
        this IServiceCollection services,
        Action<OpenAiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OpenAiOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IOpenAiClient, OpenAiClient>();

        return services;
    }
}
```

```csharp
namespace Cliente.Progetto.OpenAi.Common;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("OpenAiOptions.ApiKey is required.");
        }
    }
}
```

```sql
-- Script DDL DB-first (regola 21)
-- Naming convention: <NomeTabella>Entity, sempre al singolare
-- Eseguire questo script sul database prima di procedere con lo scaffolding via EF Core Power Tools.
-- La connection string NON deve mai comparire nel codice committato: usare dotnet user-secrets.

CREATE TABLE [dbo].[FamilyServiceEntity] (
    [Id]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Name]         NVARCHAR(200)    NOT NULL,
    [CreatedAtUtc] DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAtUtc] DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FamilyServiceEntity] PRIMARY KEY ([Id])
);
```
