---
id: kinhub-implementation
name: KinHub feature implementation workflow
version: 0.2.0
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

Il checkpoint contiene: feature, data UTC, branch, commit di partenza e motivo dell'interruzione; scope e decisioni; lavoro completato; modifiche in corso per file; comandi di verifica con esito; pull request, SHA e stato delle GitHub Actions; lavoro residuo ordinato; eventuale richiesta human in the loop; prima azione concreta di ripresa.

## Esempi

Se un test fallisce, correggi codice o test e rilancialo: non creare un checkpoint solo per il fallimento. Se una GitHub Action della PR diventa rossa, leggi il log, correggi la causa, verifica localmente, crea un nuovo commit e push, quindi attendi il run relativo al nuovo SHA. Se l'utilizzo del contesto raggiunge il 35%, aggiorna il checkpoint con il comando fallito, l'errore utile e la prossima correzione concreta, quindi interrompi.

## Dipendenze

Dipende dalla feature approvata, dalla Definition of Done del repository, dalle skill tecniche pertinenti, da Git, dal remote GitHub e da `gh` autenticato.

## Vincoli

Gli unici arresti ammessi sono utilizzo del contesto almeno al 35% e human in the loop realmente necessario. Non fermarti con documentazione incompleta, verifiche applicabili fallite o GitHub Actions della PR queued, in progress o non concluse con `success` sull'ultimo SHA. Non inserire secret o PII nel checkpoint, non includere modifiche estranee nel commit e non eseguire mai merge della pull request.

## Test richiesti

Esegui tutte le verifiche richieste dalla feature e da `AGENTS.md`. Prima della consegna verifica almeno i validatori dei tool interessati e lo stato Git; build, test, lint, packaging e validazioni applicabili devono passare. Dopo il push monitora i check della PR fino a esito terminale e accetta solo `success` per tutte le GitHub Actions attivate sull'ultimo commit.

## Checklist di aggiornamento

Leggi gli artefatti e l'eventuale checkpoint; implementa la feature; aggiorna codice, test, documentazione, traduzioni, guide, skill e fragment applicabili; ripeti le verifiche fino al successo; controlla diff e stato; crea commit e push; apri una PR verso `main`; monitora le Actions dell'ultimo SHA; per ogni esito non verde correggi, verifica, committa e pusha di nuovo; rimuovi il checkpoint solo quando tutti i check sono verdi; non eseguire il merge.

## Changelog

0.2.0: richiesto l'esito verde di tutte le GitHub Actions della PR, con ciclo obbligatorio di diagnosi, correzione e push per ogni run non riuscito.

0.1.0: introdotti continuita obbligatoria, checkpoint al 35% o human in the loop e consegna tramite pull request senza merge.
