---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corrette le risorse Bicep per i deploy dev rimuovendo la app setting `FUNCTIONS_WORKER_RUNTIME` non valida per Azure Functions Flex Consumption e allineando il parametro del Key Vault alla purge protection gia abilitata.

## en
Fixed the Bicep resources for dev deployments by removing the `FUNCTIONS_WORKER_RUNTIME` app setting, which is invalid for Azure Functions Flex Consumption, and aligning the Key Vault parameter with purge protection already enabled.
