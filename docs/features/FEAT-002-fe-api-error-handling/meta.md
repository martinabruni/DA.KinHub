---
id: FEAT-002
type: feature
status: implemented
priority: high
created_at: 2026-05-08
related:
  - BUG-001
---

# FEAT-002 — Frontend API Error Handling

## Descrizione

Standardizzazione della gestione degli errori HTTP nel frontend React. Ogni chiamata API che restituisce uno status code >= 400 deve mostrare un toast di errore con il messaggio fornito dal backend oppure un messaggio generico localizzato. Alcuni codici di stato hanno un comportamento specifico:

- **401** — il token refresh avviene automaticamente; in caso di fallimento viene mostrato un toast "Sessione scaduta" prima del redirect a `/login`
- **403** — toast con messaggio generico "permessi insufficienti"
- **404** — toast con messaggio generico "risorsa non trovata"
- **5xx** — toast con messaggio generico "errore del server"
- **altri >= 400** — toast con messaggio del backend o fallback generico

## Stato corrente (diagnosi)

| Problema | Descrizione |
|---|---|
| QueryCache copre solo le query | `MutationCache` non è configurato: le mutation senza `onError` esplicito non mostrano nessun toast |
| 403 e 404 ignorati | Il `QueryCache.onError` in `App.tsx` li filtra senza mostrare nessun feedback |
| Toast duplicati | Alcune mutation hanno `onError` esplicito E il QueryCache può farne scattare un secondo |
| Nessun messaggio specifico per 5xx | Il fallback generico non distingue errori server da errori client |
| ShoppingListProvider senza onError | Le mutation della shopping list non mostrano errori all'utente |

## Decisioni architetturali

| Decisione | Scelta |
|---|---|
| Punto di centralizzazione | Axios response interceptor in `apiClient.ts` |
| Dove mostrare il toast | Nell'interceptor, prima di `Promise.reject` |
| Gestione 401 | Invariata (refresh → retry → redirect), toast "Sessione scaduta" aggiunto prima del redirect |
| Gestione 5xx | Messaggio i18n specifico (`errors.serverError`) |
| Gestione 403 | Messaggio i18n specifico (`errors.forbidden`) |
| Gestione 404 | Messaggio i18n specifico (`errors.notFound`) |
| Rimozione handler duplicati | Rimuovere `toast.error` dagli `onError` delle singole mutation (lasciare solo la logica non-toast) |
| QueryCache / MutationCache | Rimuovere il toast da `QueryCache.onError` (diventa ridondante); mantenere per errori non-axios |

## Impatto

- `src/api/apiClient.ts`
- `src/lib/errors.ts`
- `src/App.tsx`
- `src/i18n/locales/it.json`
- `src/i18n/locales/en.json`
- Tutti i provider con `onError` che chiamano `toast.error`

## Acceptance Criteria

- [ ] Ogni risposta >= 400 mostra un toast di errore
- [ ] 401 → refresh automatico; se fallisce: toast "Sessione scaduta" + redirect a `/login`
- [ ] 403 → toast con messaggio backend o `errors.forbidden`
- [ ] 404 → toast con messaggio backend o `errors.notFound`
- [ ] 5xx → toast con messaggio backend o `errors.serverError`
- [ ] Nessun toast duplicato per la stessa chiamata
- [ ] Messaggi localizzati in italiano e inglese
- [ ] I provider non mostrano toast di errore duplicati
