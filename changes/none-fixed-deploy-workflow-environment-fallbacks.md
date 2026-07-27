---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretti i workflow di deploy per usare fallback compatibili con i secret e le variable gia presenti nell'environment `dev` e per derivare l'object id del principal OIDC dal token ARM invece che da lookup Graph fragile.

## en
Fixed the deployment workflows to use fallbacks compatible with the secrets and variables already present in the `dev` environment, and to derive the OIDC principal object id from the ARM token instead of a fragile Graph lookup.
