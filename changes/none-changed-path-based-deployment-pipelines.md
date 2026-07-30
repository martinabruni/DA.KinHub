---
type: changed
area: infra
breaking: false
issue: none
---

## it
I merge su `main` distribuiscono automaticamente il solo scope modificato: il codice applicativo usa il deploy leggero, mentre infrastruttura e migration usano un percorso full-stack serializzato che applica il database prima del codice senza deploy duplicati.

## en
Merges to `main` now automatically deploy only the changed scope: application code uses the lightweight deployment, while infrastructure and migrations use a serialized full-stack path that updates the database before the code without duplicate deployments.
