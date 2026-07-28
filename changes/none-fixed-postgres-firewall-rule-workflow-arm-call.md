---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il provisioning temporaneo delle firewall rule PostgreSQL nei workflow di deploy sostituendo il comando `az postgres flexible-server firewall-rule` con chiamate ARM via `az rest`, e reso affidabile il code deploy ricavando dinamicamente Function App, server, host e database direttamente dalle risorse Azure invece di dipendere da app setting o nomi statici.

## en
Fixed temporary PostgreSQL firewall rule provisioning in the deployment workflows by replacing `az postgres flexible-server firewall-rule` with ARM calls through `az rest`, and made code deployments reliable by dynamically resolving the Function App, server, host, and database directly from Azure resources instead of depending on app settings or static names.
