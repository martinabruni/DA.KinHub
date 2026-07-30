# BUG-FEAT-003-001 - Errore 500 nella lettura degli item KinList

- **Feature interessata**: FEAT-003 `lista-condivisa-paginata`
- **Tipo**: correzione backend, query EF Core
- **Stato**: risolto
- **Breaking change prodotto**: no

## Segnalazione

La richiesta `GET /api/kinlist/items` restituiva:

```json
{
  "title": "Internal server error",
  "status": 500,
  "detail": "The request could not be completed.",
  "instance": "/api/kinlist/items",
  "code": "internal.unexpected",
  "traceId": "0d65ad199a086f051d030caf2c000c14",
  "correlationId": "9c423a8c-67a1-4f28-b273-fda908d28f8f"
}
```

## Causa

La proiezione di `ActiveKinListItemRepository` accedeva a `item.Name.Value` direttamente dentro la query LINQ. `Name` è un value object configurato con un value converter EF Core; l'accesso alla proprietà interna del value object non è traducibile in SQL e generava un'eccezione inattesa durante l'esecuzione della query.

## Correzione

La query ora proietta `item.Name`, lasciando applicare a EF Core il converter configurato. Il valore testuale viene letto solo dopo la materializzazione della query, nel mapping verso il contratto della pagina.

## Verifica

- Build Release della soluzione.
- Test di integrazione eseguiti: 26 passati, 5 ignorati perché richiedono PostgreSQL disponibile.
- La verifica PostgreSQL reale della query resta parte della suite Testcontainers quando il runtime Docker è disponibile.
