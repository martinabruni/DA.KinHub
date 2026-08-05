---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il workflow di deploy frontend aggiungendo `AZURE_STATIC_WEB_APP_URL` come fallback autorevole per risolvere l'hostname della Static Web App quando nome e query ARM non sono sufficienti nell'environment GitHub.

## en
Fixed the frontend deployment workflow by adding `AZURE_STATIC_WEB_APP_URL` as the authoritative fallback to resolve the Static Web App hostname when the name and ARM queries are not sufficient in the GitHub environment.
