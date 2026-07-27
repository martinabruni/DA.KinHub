---
type: fixed
area: infra
breaking: false
issue: none
---

## it
Corretto il comando `dotnet ef migrations bundle` nei workflow di deploy rimuovendo l'argomento invalido `--self-contained false`, che interrompeva la generazione del bundle EF Core.

## en
Fixed the `dotnet ef migrations bundle` command in the deployment workflows by removing the invalid `--self-contained false` argument, which was breaking EF Core bundle generation.
