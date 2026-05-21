---
id: BUG-001
feature: FEAT-002
type: bug
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-001
  - TASK-002
  - TASK-003
---

# BUG-001 — Global API Error Toast Missing or Inconsistent for HTTP >= 400

## Descrizione

Le chiamate API che restituiscono uno status code >= 400 non mostrano sistematicamente un toast di errore all'utente. Il comportamento attuale è incompleto e incoerente tra diversi punti del frontend.

## Comportamento atteso

Ogni risposta con status >= 400 deve mostrare un toast di errore con:
- il messaggio fornito dal backend (campo `message` nel body della risposta), oppure
- un messaggio generico localizzato specifico per il codice di stato

## Comportamento attuale

### QueryCache (App.tsx)
```ts
queryCache: new QueryCache({
  onError: (error) => {
    if (isHttpStatus(error, 404) || isHttpStatus(error, 401) || isHttpStatus(error, 403)) return
    toast.error(getApiErrorMessage(error))
  },
})
```
- Copre solo gli errori delle **query** (GET), non delle **mutation** (POST/PUT/DELETE/PATCH)
- Filtra silenziosamente 401, 403 e 404 senza mostrare nessun feedback
- Non è presente un `MutationCache` equivalente

### Mutation individuali
- Alcuni provider hanno `onError: (err) => toast.error(getApiErrorMessage(err))` sulla singola mutation
- Altri provider (es. `ShoppingListProvider`) non hanno alcun handler → errori ingoiati silenziosamente
- Rischio di toast duplicati quando il QueryCache e il mutation handler scattano per la stessa chiamata

### Axios interceptor (apiClient.ts)
- Gestisce solo il 401 per il token refresh
- Non mostra nessun toast per nessun altro errore

## File interessati

- `src/api/apiClient.ts` — interceptor da estendere
- `src/App.tsx` — QueryCache da aggiornare
- `src/lib/errors.ts` — potenzialmente aggiungere `getStatusErrorMessage(status)`
- `src/i18n/locales/it.json` / `en.json` — aggiungere chiavi `errors.forbidden`, `errors.notFound`, `errors.serverError`
- Provider con `onError` toast da rimuovere (duplicati):
  - `src/features/family/FamilyProvider.tsx`
  - `src/features/fridges/FridgeProvider.tsx`
  - altri provider con pattern identico

## Riproduzione

1. Chiamare qualsiasi endpoint che restituisce 403 → nessun toast
2. Chiamare qualsiasi endpoint che restituisce 404 → nessun toast
3. Eseguire una mutation (POST/PUT/DELETE) senza `onError` esplicito → nessun toast
4. Eseguire una mutation su provider che ha `onError` → toast singolo ✓ (ma solo per quel provider)

## Note

La soluzione preferita è centralizzare la logica nell'axios response interceptor, così da coprire tutte le chiamate indipendentemente da chi le fa. Vedi TASK-001.
