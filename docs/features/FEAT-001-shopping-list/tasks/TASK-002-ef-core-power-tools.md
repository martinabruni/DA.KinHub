---
id: TASK-002
feature: FEAT-001
type: task
status: implemented
priority: high
assignee: developer
created_at: 2026-05-08
related:
  - TASK-001
  - TASK-009
---

# TASK-002 — EF Core Power Tools — Scaffolding Entity

## Descrizione

Dopo aver eseguito lo script SQL (TASK-001), usare **EF Core Power Tools** per rigenerare automaticamente le entity e aggiornare `CoreDbContext`.

## Prerequisiti

- TASK-001 eseguito sul database: le tabelle `kinrecipe."ShoppingListEntity"` e `kinrecipe."ShoppingListItemEntity"` devono esistere nel DB.

## Steps

1. Aprire Visual Studio / Rider
2. Click destro sul progetto `Kin.KinHub.Core.PostgreSql` → **EF Core Power Tools** → **Reverse Engineer**
3. Selezionare le nuove tabelle: `ShoppingListEntity`, `ShoppingListItemEntity`
4. Lasciare le impostazioni di scaffolding esistenti (schema `kinrecipe`, cartella `RecipeFeature/Models/`)
5. Confermare la rigenerazione — verranno creati/aggiornati:
   - `RecipeFeature/Models/ShoppingListEntity.cs`
   - `RecipeFeature/Models/ShoppingListItemEntity.cs`
   - `Common/CoreDbContext.cs` (aggiornato con i nuovi DbSet e configurazioni)
6. Verificare che il build compili senza errori prima di procedere con TASK-009

## ⚠️ Assegnato allo sviluppatore

Questo task richiede esecuzione manuale dell'ambiente di sviluppo locale (connessione al DB, EF Core Power Tools GUI).

## File impattati (generati automaticamente)

- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListEntity.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/RecipeFeature/Models/ShoppingListItemEntity.cs`
- `src/Infrastructures/Kin.KinHub.Core.PostgreSql/Common/CoreDbContext.cs`
