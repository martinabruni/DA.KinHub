---
type: fixed
area: backend
breaking: false
issue: none
---

## it
KinList ora usa un upsert atomico per il profilo applicativo `(iss, oid)`, tratta come indisponibilita del repository solo i guasti reali di persistenza e richiede una configurazione database esplicita tra `ConnectionString` locale e `ManagedIdentity`.

## en
KinList now uses an atomic upsert for the `(iss, oid)` application profile, treats only real persistence failures as repository unavailability, and requires an explicit database configuration between local `ConnectionString` and `ManagedIdentity`.
