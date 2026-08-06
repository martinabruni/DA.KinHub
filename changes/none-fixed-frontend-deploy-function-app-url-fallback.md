---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il deploy frontend usando `AZURE_FUNCTIONAPP_URL` come fallback autorevole per l'hostname API quando il provisioning infrastrutturale non viene eseguito nello stesso workflow.

## en
Fixed frontend deployment by using `AZURE_FUNCTIONAPP_URL` as the authoritative API hostname fallback when infrastructure provisioning does not run in the same workflow.
