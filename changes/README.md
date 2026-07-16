# Change fragments

Ogni modifica significativa aggiunge un file `<issue>-<type>-<slug>.md` con frontmatter:

```yaml
---
type: added
area: frontend
breaking: false
issue: 1234
---

## it
Descrizione italiana.

## en
English description.
```

Tipi ammessi: `added`, `changed`, `deprecated`, `removed`, `fixed`, `security`. Usa `none` quando non esiste una issue/PR. Esegui `npm run release:validate` prima della PR.
