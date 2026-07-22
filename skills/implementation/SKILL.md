---
id: kinhub-implementation
name: KinHub feature implementation workflow
version: 0.1.0
area: implementation
description: Esecuzione autonoma end-to-end delle feature, checkpoint riprendibili e consegna tramite pull request.
references: AGENTS.md, skills/implementation/templates/implementation-progress.md
---

# KinHub feature implementation

## Scopo

Portare una feature approvata dall'implementazione alla pull request senza arresti prematuri, conservando uno stato riprendibile quando e indispensabile interrompere il lavoro.

## Quando usare

Usa questa skill ogni volta che l'utente chiede di implementare, completare o correggere una feature.

## Quando non usare

Non usarla per brainstorming, ricerca, backlog, sola pianificazione, code review o richieste informative che non prevedono modifiche.

## Componenti e servizi disponibili

La fonte autorevole e `AGENTS.md`; la cartella della feature contiene gli artefatti approvati e, solo durante un'interruzione ammessa, `implementation-progress.md`. Git e GitHub CLI gestiscono la consegna finale.

## API e interfacce

Prima di lavorare individua la cartella della feature e leggi `feature.md`, il piano applicabile, le eventuali Change Request e un checkpoint esistente. `implementation-progress.md` segue `templates/implementation-progress.md` e deve permettere a una nuova sessione di ripartire senza ricostruire decisioni gia prese.

Il checkpoint contiene: feature, data UTC, branch, commit di partenza e motivo dell'interruzione; scope e decisioni; lavoro completato; modifiche in corso per file; comandi di verifica con esito; lavoro residuo ordinato; eventuale richiesta human in the loop; prima azione concreta di ripresa.

## Esempi

Se un test fallisce, correggi codice o test e rilancialo: non creare un checkpoint solo per il fallimento. Se l'utilizzo del contesto raggiunge il 35%, aggiorna il checkpoint con il comando fallito, l'errore utile e la prossima correzione concreta, quindi interrompi.

## Dipendenze

Dipende dalla feature approvata, dalla Definition of Done del repository, dalle skill tecniche pertinenti, da Git, dal remote GitHub e da `gh` autenticato.

## Vincoli

Gli unici arresti ammessi sono utilizzo del contesto almeno al 35% e human in the loop realmente necessario. Non fermarti con documentazione incompleta o verifiche applicabili fallite. Non inserire secret o PII nel checkpoint, non includere modifiche estranee nel commit e non eseguire mai merge della pull request.

## Test richiesti

Esegui tutte le verifiche richieste dalla feature e da `AGENTS.md`. Prima della consegna verifica almeno i validatori dei tool interessati e lo stato Git; build, test, lint, packaging e validazioni applicabili devono passare.

## Checklist di aggiornamento

Leggi gli artefatti e l'eventuale checkpoint; implementa la feature; aggiorna codice, test, documentazione, traduzioni, guide, skill e fragment applicabili; ripeti le verifiche fino al successo; rimuovi il checkpoint; controlla diff e stato; crea commit e push; apri una PR verso `main`; non eseguire il merge.

## Changelog

0.1.0: introdotti continuita obbligatoria, checkpoint al 35% o human in the loop e consegna tramite pull request senza merge.
