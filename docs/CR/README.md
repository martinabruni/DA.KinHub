# Change Request

Usa una CR per modifiche a una funzionalità esistente. Registra motivazione, comportamento attuale/desiderato, compatibilità, migration, rischio, rollback e criteri verificabili.

Quando la CR appartiene a una feature sotto `docs/**/backlog/features/<codice>/`, conserva gli artefatti insieme:

- `feature.md`: requisiti e criteri della feature originaria;
- `feature.plan.md`: piano originario, non riscritto retroattivamente;
- `cr.md`: motivazione, delta, contratti invariati, rischi e accettazione della modifica;
- `cr.plan.md`: piano esecutivo corrente della CR.

Se esistono piu CR per la stessa feature, usa un suffisso identificativo coerente invece di sovrascrivere la storia. Una CR tecnica non amplia implicitamente lo scope prodotto.
