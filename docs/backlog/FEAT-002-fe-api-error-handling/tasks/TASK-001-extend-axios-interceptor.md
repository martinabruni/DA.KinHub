---
id: TASK-001
feature: FEAT-002
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - BUG-001
---

# TASK-001 — Extend Axios Interceptor for Global Error Toast

## Obiettivo

Estendere il response interceptor in `src/api/apiClient.ts` per mostrare un toast di errore per tutti i codici di stato >= 400, con messaggi specifici per 401, 403, 404 e 5xx.

## Implementazione

### `src/api/apiClient.ts`

Il branch `error.response?.status !== 401 || original._retry` che oggi fa solo `Promise.reject(error)` deve essere esteso per mostrare il toast prima di rigettare.

Aggiungere import di `toast` da `sonner` e `getApiErrorMessage` da `@/lib/errors`.

Logica da aggiungere nel ramo non-401 dell'interceptor:

```ts
// Prima di return Promise.reject(error)
const status = error.response?.status
if (status !== undefined) {
  const message = getStatusAwareErrorMessage(error, status)
  toast.error(message)
}
return Promise.reject(error)
```

La funzione `getStatusAwareErrorMessage` (da aggiungere in `src/lib/errors.ts`) deve:
- Leggere prima il messaggio del backend (`getApiErrorMessage`)
- Se assente, restituire la chiave i18n per il codice di stato

Per il 401 (nel ramo del refresh fallito):
```ts
toast.error(t('errors.sessionExpired'))
clearTokens()
window.location.href = '/login'
```

## File da modificare

- `src/api/apiClient.ts`
- `src/lib/errors.ts` — aggiungere `getStatusAwareErrorMessage(err, status)`

## Note

- Usare `i18next.t()` direttamente (non il hook) perché l'interceptor è fuori dal contesto React
- Importare `i18next` da `i18next` (istanza globale già configurata in `src/i18n/index.ts`)
