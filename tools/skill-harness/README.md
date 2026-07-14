# KinHub skill harness

CLI Node.js senza dipendenze che indicizza conoscenza Markdown senza eseguirne il contenuto.

- `list`: elenca id, nome e descrizione.
- `read <id|area>`: stampa la skill selezionata.
- `build`: valida e rigenera deterministicamente `skills/registry.json`.
- `validate`: valida skill, cataloghi, riferimenti, duplicati e registry versionato.
- `watch`: osserva le modifiche in sviluppo e rigenera il registry.

I comandi pubblici sono esposti dal `package.json` della root. Eseguire i test con `npm run skills:test`.
