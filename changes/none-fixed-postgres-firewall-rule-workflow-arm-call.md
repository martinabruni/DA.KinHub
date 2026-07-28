---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il provisioning temporaneo delle firewall rule PostgreSQL nei workflow di deploy sostituendo il comando `az postgres flexible-server firewall-rule` con chiamate ARM via `az rest`, e reso affidabile il code deploy ricavando server, host e database direttamente dalle risorse PostgreSQL invece di dipendere dagli app setting della Function App.

## en
Fixed temporary PostgreSQL firewall rule provisioning in the deployment workflows by replacing `az postgres flexible-server firewall-rule` with ARM calls through `az rest`, and made code deployments reliable by resolving server, host, and database directly from PostgreSQL resources instead of depending on Function App app settings.
