---
id: kinhub-implementation
name: KinHub repository implementation workflow
version: 0.3.0
area: implementation
description: Esecuzione autonoma end-to-end di modifiche repository, checkpoint riprendibili e consegna tramite pull request.
references: AGENTS.md, skills/implementation/templates/implementation-progress.md
---

# KinHub repository implementation

## Scopo

Portare una modifica al repository, inclusi fix, refactor, workflow, documentazione versionata e feature approvate, fino alla pull request senza arresti prematuri, conservando uno stato riprendibile quando e indispensabile interrompere il lavoro.

## Quando usare

Usa questa skill ogni volta che l'utente chiede di implementare, completare, correggere o aggiornare qualcosa nel repository.

## Quando non usare

Non usarla per brainstorming, ricerca, backlog, sola pianificazione, code review o richieste informative che non prevedono modifiche.

## Componenti e servizi disponibili

La fonte autorevole e `AGENTS.md`; se il lavoro appartiene a una feature approvata usa la relativa cartella di backlog, altrimenti lavora direttamente nei percorsi coinvolti. Solo durante un'interruzione ammessa mantieni `implementation-progress.md` nella cartella della feature oppure, se non esiste, nella cartella piu vicina che rappresenta il lavoro corrente o nella root del repository. Git e GitHub CLI gestiscono la consegna finale.

## API e interfacce

Prima di lavorare individua il contenitore autorevole del lavoro: cartella della feature con `feature.md` e piano applicabile se la richiesta nasce dal backlog, oppure i file/percorsi direttamente interessati se si tratta di fix o modifica puntuale. Leggi anche eventuali Change Request e un checkpoint esistente. `implementation-progress.md` segue `templates/implementation-progress.md` e deve permettere a una nuova sessione di ripartire senza ricostruire decisioni gia prese.

Il checkpoint contiene: richiesta o feature di riferimento, data UTC, branch, commit di partenza e motivo dell'interruzione; scope e decisioni; lavoro completato; modifiche in corso per file; comandi di verifica con esito; pull request, SHA e stato delle GitHub Actions; lavoro residuo ordinato; eventuale richiesta human in the loop; prima azione concreta di ripresa.

## Esempi

Se un test fallisce, correggi codice o test e rilancialo: non creare un checkpoint solo per il fallimento. Se una GitHub Action della PR diventa rossa, leggi il log, correggi la causa, verifica localmente, crea un nuovo commit e push, quindi attendi il run relativo al nuovo SHA. Se l'utilizzo del contesto raggiunge il 35%, aggiorna il checkpoint con il comando fallito, l'errore utile e la prossima correzione concreta, quindi interrompi.

## Dipendenze

Dipende dal contesto autorevole della richiesta, dalla Definition of Done del repository, dalle skill tecniche pertinenti, da Git, dal remote GitHub e da `gh` autenticato.

## Vincoli

Gli unici arresti ammessi sono utilizzo del contesto almeno al 35% e human in the loop realmente necessario. Non fermarti con documentazione incompleta, verifiche applicabili fallite o GitHub Actions della PR queued, in progress o non concluse con `success` sull'ultimo SHA. Non inserire secret o PII nel checkpoint, non includere modifiche estranee nel commit e non eseguire mai merge della pull request. Ogni pull request parte dal branch sorgente `dev` ed e destinata a `main`.

## Test richiesti

Esegui tutte le verifiche richieste dalla modifica e da `AGENTS.md`. Prima della consegna verifica almeno i validatori dei tool interessati e lo stato Git; build, test, lint, packaging e validazioni applicabili devono passare. Dopo il push monitora i check della PR fino a esito terminale e accetta solo `success` per tutte le GitHub Actions attivate sull'ultimo commit.

## Checklist di aggiornamento

Leggi gli artefatti e l'eventuale checkpoint; verifica di lavorare su `dev`; implementa la modifica richiesta; aggiorna codice, test, documentazione, traduzioni, guide, skill e fragment applicabili; ripeti le verifiche fino al successo; controlla diff e stato; crea commit e push su `dev`; apri una PR da `dev` verso `main`; monitora le Actions dell'ultimo SHA; per ogni esito non verde correggi, verifica, committa e pusha di nuovo; rimuovi il checkpoint solo quando tutti i check sono verdi; non eseguire il merge.

## Changelog

0.3.0: estendo la skill a qualsiasi modifica del repository, non solo a nuove feature, definisco la posizione del checkpoint fuori backlog e rendo obbligatori commit, push, PR e monitoraggio Actions per fix, workflow e aggiornamenti documentali versionati.

0.2.0: imposto `dev` come branch sorgente obbligatorio di ogni pull request verso `main` e richiedo l'esito verde di tutte le GitHub Actions della PR, con ciclo obbligatorio di diagnosi, correzione e push per ogni run non riuscito.

0.1.0: introdotti continuita obbligatoria, checkpoint al 35% o human in the loop e consegna tramite pull request senza merge.
