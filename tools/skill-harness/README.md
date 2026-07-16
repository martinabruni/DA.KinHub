# KinHub skill harness

Tool Node.js senza dipendenze esterne. Scansiona esclusivamente documenti e cataloghi JSON: non importa né esegue codice indicato dalle skill.

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:validate
npm run skills:build
npm run skills:watch
```

`build` rigenera il registry deterministico; `validate` fallisce se metadati, sezioni, cataloghi, riferimenti o registry non sono validi.
