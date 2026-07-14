# KinHub skills

Le skill sono conoscenza operativa versionata. Il loro contenuto non viene mai eseguito dinamicamente.

Ogni `SKILL.md` usa metadati dichiarativi, sezioni obbligatorie e un eventuale `catalog.json`. Dalla root:

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:validate
npm run skills:build
npm run skills:watch
npm run skills:test
```

`validate` non modifica file; `build` rigenera deterministicamente il registry versionato. Dettagli in [skill harness](../tools/skill-harness/README.md).
