---
id: TASK-012
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-008
  - TASK-009
  - TASK-011
---

# TASK-012 — DI Registration

## Descrizione

Registrare i nuovi servizi e repository nelle rispettive `ServiceCollectionExtensions.cs`.

## Modifiche richieste

### Core.PostgreSql/ServiceCollectionExtensions.cs

Aggiungere:
```csharp
services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
services.AddScoped<IShoppingListItemRepository, ShoppingListItemRepository>();
```

### Core.Business/ServiceCollectionExtensions.cs

Aggiungere:
```csharp
services.AddScoped<IShoppingListService, KinHubShoppingListService>();
services.AddScoped<IShoppingListItemService, KinHubShoppingListItemService>();
```

## Note

- Verificare che i namespace siano corretti (file-scoped `Microsoft.Extensions.DependencyInjection`).
- I validator FluentValidation devono essere già registrati automaticamente tramite `AddValidatorsFromAssembly` — verificare che copra anche i nuovi validator.

## File impattati

- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/ServiceCollectionExtensions.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ServiceCollectionExtensions.cs`
