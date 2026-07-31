---
slug: family
locale: it
title: Famiglia
description: Consultare il nome della famiglia, i membri attivi e gli inviti attivi senza esporre codici segreti.
---

## Nome della famiglia

La pagina **Famiglia** mostra il nome corrente della famiglia autorizzata. La route e ricostruibile (`/settings/family`) e ricarica i dati dal server ogni volta che account, famiglia o sessione cambiano.

## Membri attivi

La sezione Membri legge pagine limitate di membership attive. Ogni riga mostra solo nome e iniziali minime. Se un profilo non ha un nome approvato, KinHub usa il fallback **Membro** e l'avatar mostra `?`.

## Inviti attivi

La sezione Inviti attivi mostra solo creatore, data di creazione, scadenza e stato attivo. Il codice segreto e la relativa impronta non vengono mai mostrati in questa pagina.

## Paginazione e recupero

Membri e inviti usano cursori opachi indipendenti e non salvati nel browser. Se un cursore non e piu valido, puoi tornare all'inizio della sola sezione interessata. Se la famiglia non ha inviti attivi, compare uno stato vuoto dedicato.

## Requisito online

La pagina Famiglia richiede connessione attiva e sessione valida. KinHub non salva nome, membri, inviti o cursori in storage locale, quindi offline i dati non sono disponibili.
