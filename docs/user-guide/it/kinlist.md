---
slug: kinlist
locale: it
title: KinList
description: Lista condivisa paginata con ordine stabile, autore, categorie essenziali e navigazione avanti/indietro.
---

## Cosa mostra KinList

KinHub verifica sempre in modo autorevole se il tuo profilo ha una membership familiare attiva. Quando la verifica e positiva, la route `/kinlist` esegue anche un controllo server-side dedicato sulla disponibilita del servizio `kinlist` per la famiglia autorizzata e poi legge solo gli elementi `Active` visibili.

L'ordine e stabile: prima i gruppi piu recenti, poi la posizione originale dell'elemento nel gruppo. La pagina mostra nome, fino a tre categorie, eventuale `+N` e autore. Se il profilo non ha ancora un nome visibile, l'autore usa il fallback accessibile **Membro** con avatar `?`.

## Navigare e aggiornare

La pagina iniziale legge 50 elementi per volta. Puoi usare **Indietro** e **Avanti** per muoverti tra le pagine senza numero pagina o totale. **Aggiorna** riparte sempre dalla prima pagina.

Durante refresh o navigazione l'ultima pagina valida resta leggibile. Se il cursore della pagina non e piu valido, KinList non mostra dati nuovi parziali: conserva la vista corrente e ti propone di tornare all'inizio.

## Creare la prima famiglia

Scegli **Crea una famiglia** per aprire un form con il solo campo nome. Il nome accetta da 1 a 100 caratteri dopo trim e compressione degli spazi, conserva maiuscole e caratteri Unicode validi e non viene salvato nel browser. Quando l'invio va a buon fine, KinHub crea insieme famiglia e membership del creatore e ti lascia direttamente in KinList.

Se la richiesta viene ripetuta o arriva in concorrenza, KinHub restituisce comunque lo stesso contesto famiglia senza creare duplicati.

## Cosa non viene mostrato

Quando non esiste una membership attiva, KinHub non mostra membri, lista o altri dati condivisi. Anche l'accesso negato resta distinto da uno stato vuoto e non espone se il servizio sia sconosciuto, inattivo o semplicemente non disponibile per la famiglia.

Questa slice non include ancora creazione manuale di elementi, microfono, filtro categoria, drawer, selezione o completamento.

## Offline e privacy

Offline resta disponibile solo la shell pubblica della PWA. KinList non conserva elementi, pagine o cursori in cache, `localStorage`, `sessionStorage`, IndexedDB o service worker. Anche il nome famiglia inserito resta solo in memoria finche la pagina e attiva.
