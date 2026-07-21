# Skill di progetto KinHub

Le skill sono conoscenza operativa versionata. Prima di modificare un'area, esegui `npm run skills:read -- <area>`. Dopo aver promosso un elemento riutilizzabile aggiorna implementazione, test, esempio, catalogo, `SKILL.md`, change fragment e registry con `npm run skills:build`.

Il harness legge solo Markdown e JSON e non esegue codice referenziato dalle skill.

Il frontmatter opzionale `references` elenca, separati da virgole, documenti Markdown/JSON repository-relative che completano la skill. Il registry include path e checksum; reference mancanti, duplicate, non documentali o risolte fuori dal repository rendono non valida la skill.
