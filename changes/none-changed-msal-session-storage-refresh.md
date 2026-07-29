---
type: changed
area: frontend
breaking: false
issue: none
---

## it
La cache MSAL usa `sessionStorage` per mantenere la sessione della scheda durante il refresh. Il bootstrap famiglia resta obbligatorio e i dati applicativi continuano a vivere soltanto in memoria.

## en
The MSAL cache uses `sessionStorage` to preserve the tab session across refreshes. The family bootstrap remains mandatory, and application data continues to live only in memory.
