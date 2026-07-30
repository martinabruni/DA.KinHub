---
type: changed
area: infra
breaking: false
issue: none
---

## it
Separati i deploy Azure per responsabilita: `infra/**` esegue solo il provisioning Bicep, `src/backend/**` applica migration e distribuisce la Function App, `src/frontend/**` distribuisce soltanto la SPA, mantenendo l'ordine corretto nei commit misti.

## en
Separated Azure deployments by responsibility: `infra/**` only provisions Bicep resources, `src/backend/**` applies migrations and deploys the Function App, and `src/frontend/**` only deploys the SPA, while preserving the correct order for mixed commits.
