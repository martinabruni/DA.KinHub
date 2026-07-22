# KinHub skill harness

Tool Node.js senza dipendenze esterne. Scansiona esclusivamente documenti e cataloghi JSON: non importa né esegue codice indicato dalle skill.

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:read -- implementation
npm run skills:validate
npm run skills:build
npm run skills:watch
```

`build` rigenera il registry deterministico; `validate` fallisce se metadati, sezioni, cataloghi, riferimenti o registry non sono validi.

La skill `implementation` e obbligatoria per le richieste di implementazione feature. Definisce gli unici arresti ammessi, il checkpoint `implementation-progress.md` nella cartella della feature e la consegna tramite commit, push e pull request verso `main`. Tutte le GitHub Actions attivate dalla PR devono essere verdi sull'ultimo commit; i run rossi richiedono correzione e nuovo push. Il merge resta vietato.

Una skill puo dichiarare documenti passivi repository-relative nel frontmatter:

```yaml
references: docs/architecture/http-functions.md, docs/operations/observability.md
```

L'harness accetta solo documenti Markdown/JSON, rifiuta reference mancanti, duplicate o esterne al repository e ne registra il checksum. Le reference vengono lette come testo e non sono mai importate o eseguite.
