---
id: TASK-003
feature: FEAT-005
type: task
status: planned
priority: high
created_at: 2026-05-21
related:
  - CR-001
  - TASK-004
---

# TASK-003 - Comporre i deep link ricetta verso Core React

## Obiettivo

Includere nelle risposte KinAi un link diretto alla recipe detail page della SWA `Kin.KinHub.Core.React`.

## Attivita'

- riusare il route template esistente `/recipe-books/{bookId}/recipes/{recipeId}`;
- costruire URL assoluti partendo dalla variabile ambiente `VITE_CORE_URL` lato KinAi;
- aggiungere il link nelle risposte che elencano o identificano ricette salvate.

## Output atteso

- ogni ricetta restituita da KinAi puo' includere un URL apribile verso Core React;
- la composizione URL e' centralizzata e indipendente dall'ambiente locale/produzione.

