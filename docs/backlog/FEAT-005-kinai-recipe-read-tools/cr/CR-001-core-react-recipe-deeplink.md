---
id: CR-001
feature: FEAT-005
type: change-request
status: planned
priority: high
created_at: 2026-05-21
related:
  - TASK-003
  - TASK-004
---

# CR-001 - Deep link assoluto verso la SWA Core React

## Richiesta

Quando KinAi restituisce una ricetta o un riferimento navigabile, il messaggio deve includere un link diretto verso la pagina dettaglio della ricetta sulla Static Web App di `Kin.KinHub.Core.React`.

## Vincoli

- non usare path relativi alla SWA KinAi;
- la base URL deve essere fornita dalla variabile ambiente `VITE_CORE_URL`;
- il route finale deve seguire il path gia' in uso in Core React: `/recipe-books/{bookId}/recipes/{recipeId}`.

## Impatto

- configurazione build/deploy KinAi;
- composizione output tool lato backend;
- rendering messaggi chat lato frontend.

