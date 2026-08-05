# KinHub skill harness

Tool Node.js senza dipendenze esterne. Scansiona esclusivamente documenti e cataloghi JSON: non importa né esegue codice indicato dalle skill.

Le skill di progetto sono in `.agents/skills/`; il harness considera le skill con frontmatter dello schema repository (`id`, `name`, `version`, `area`, `description`) e lascia disponibili nello stesso albero le skill agente con schema differente.

```bash
npm run skills:list
npm run skills:read -- frontend
npm run skills:read -- implementation
npm run skills:validate
npm run skills:build
npm run skills:watch
```

`build` rigenera il registry deterministico; `validate` fallisce se metadati, sezioni, cataloghi, riferimenti o registry non sono validi. Verifica anche che ogni route di una HTTP Function sia documentata in `openapi.yaml`.

La skill `implementation` e obbligatoria per le richieste di implementazione feature. Definisce gli unici arresti ammessi, il checkpoint `implementation-progress.md` nella cartella della feature e la consegna tramite commit e push su `dev`, quindi pull request con branch sorgente `dev` e destinazione `main`. Quando una feature passa a `In review`, il lavoro non e concluso finche la PR non esiste e tutte le GitHub Actions attivate sull'ultimo commit non sono verdi; i run rossi richiedono correzione e nuovo push. Il merge resta vietato.

La stessa skill impone guardrail anti-regressione derivati dai fix reali del repository: verifica preventiva di versioni/runtime Azure supportati, grep repository-wide per rename configurativi, controllo dei contratti effettivi dei workflow, uso corretto di `functionAppConfig` su Flex Consumption, connessioni storage identity-based non ambigue, rigenerazione degli artefatti derivati e verifiche live post-deploy quando si toccano deploy o observability.

Una skill puo dichiarare documenti passivi repository-relative nel frontmatter:

```yaml
references: docs/architecture/http-functions.md, docs/operations/observability.md
```

L'harness accetta solo documenti Markdown/JSON, rifiuta reference mancanti, duplicate o esterne al repository e ne registra il checksum. Le reference vengono lette come testo e non sono mai importate o eseguite.
