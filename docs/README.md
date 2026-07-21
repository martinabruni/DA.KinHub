# Documentazione KinHub

- `architecture/`: decisioni e confini tecnici.
- `development/`: ambiente locale, test e convenzioni.
- `operations/`: migrazioni, packaging, deployment e troubleshooting.
- `user-guide/{it,en}/`: unica fonte Markdown per la guida in-app.
- `patch-notes/{it,en}/`: output localizzato del release tool.
- `FP/`: Feature Proposal.
- `CR/`: Change Request.

Esegui `npm run docs:sync` dopo ogni modifica alle guide utente.

Le CR collegate a una feature vivono nella cartella della feature: `feature.md` e `feature.plan.md` conservano la consegna originaria, mentre `cr.md` e `cr.plan.md` descrivono delta e piano correttivo correnti.
