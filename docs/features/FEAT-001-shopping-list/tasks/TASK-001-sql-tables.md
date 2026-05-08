---
id: TASK-001
feature: FEAT-001
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-002
---

# TASK-001 — SQL Tables

## Descrizione

Aggiungere le due nuove tabelle in fondo a `scripts/create-postgres-schema.sql`:

- `kinrecipe."ShoppingListEntity"` — hard delete, nessuna `IsDeleted`
- `kinrecipe."ShoppingListItemEntity"` — FK su `ShoppingListEntity` con `ON DELETE CASCADE`

## Dettagli

### ShoppingListEntity

| Colonna | Tipo | Note |
|---|---|---|
| `"Id"` | `uuid` | PK, default `gen_random_uuid()` |
| `"Name"` | `varchar(200)` | NOT NULL |
| `"FamilyId"` | `uuid` | NOT NULL, FK su families |
| `"CreatedAt"` | `timestamp` | NOT NULL, default `now()` |
| `"UpdatedAt"` | `timestamp` | NOT NULL, default `now()` |

### ShoppingListItemEntity

| Colonna | Tipo | Note |
|---|---|---|
| `"Id"` | `uuid` | PK, default `gen_random_uuid()` |
| `"Name"` | `varchar(200)` | NOT NULL |
| `"IsChecked"` | `boolean` | NOT NULL, default `false` |
| `"ShoppingListId"` | `uuid` | NOT NULL, FK → ShoppingListEntity ON DELETE CASCADE |
| `"CreatedAt"` | `timestamp` | NOT NULL, default `now()` |
| `"UpdatedAt"` | `timestamp` | NOT NULL, default `now()` |

## File impattati

- `scripts/create-postgres-schema.sql`

## Note

- Eseguire lo script sul DB prima di procedere con lo scaffolding EF Core Power Tools.
- Le colonne usano PascalCase quotate per compatibilità con Mapster (zero-config mapping).
- Aggiungere un indice su `"ShoppingListId"` nella tabella item per performance query.
