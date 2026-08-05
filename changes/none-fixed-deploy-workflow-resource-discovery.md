---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretti i workflow di deploy rendendo il build backend coerente con il percorso gia validato in `pr-quality` e sostituendo nel deploy frontend la discovery della Static Web App via `az staticwebapp` con query ARM generiche piu affidabili sul runner GitHub.

## en
Fixed the deployment workflows by aligning the backend build with the path already validated in `pr-quality` and replacing frontend Static Web App discovery via `az staticwebapp` with more reliable generic ARM queries on the GitHub runner.
