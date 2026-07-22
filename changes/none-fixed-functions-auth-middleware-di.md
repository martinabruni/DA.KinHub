---
type: fixed
area: backend
breaking: false
issue: none
---

## it
Corretto il middleware di autorizzazione della Function App per risolvere i servizi scoped per-invocazione invece che dal costruttore singleton, con test di regressione che valida l'avvio del container DI con il middleware registrato.

## en
Fixed the Function App authorization middleware to resolve scoped services per invocation instead of from the singleton constructor, with a regression test that validates DI container startup when the middleware is registered.
