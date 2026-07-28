---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il provisioning temporaneo delle firewall rule PostgreSQL nei workflow di deploy sostituendo il comando `az postgres flexible-server firewall-rule` con chiamate ARM via `az rest`, evitando il crash della Azure CLI durante le migration.

## en
Fixed temporary PostgreSQL firewall rule provisioning in the deployment workflows by replacing `az postgres flexible-server firewall-rule` with ARM calls through `az rest`, avoiding the Azure CLI crash during migrations.
