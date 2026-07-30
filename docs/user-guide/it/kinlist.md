---
slug: kinlist
locale: it
title: KinList
description: Verifica del percorso corretto dopo il login, onboarding obbligatorio e shell offline sicura.
---

## Cosa succede dopo il login

KinHub verifica sempre in modo autorevole se il tuo profilo ha una membership familiare attiva. Se la verifica trova una famiglia attiva, la route `/kinlist` esegue anche un controllo server-side dedicato sulla disponibilita del servizio `kinlist` per la famiglia autorizzata. Se la famiglia manca oppure la membership non e attiva, resti nella route `/kinlist` e vedi l'onboarding KinHub.

## Creare la prima famiglia

Scegli **Crea una famiglia** per aprire un form con il solo campo nome. Il nome accetta da 1 a 100 caratteri dopo trim e compressione degli spazi, conserva maiuscole e caratteri Unicode validi e non viene salvato nel browser. Quando l'invio va a buon fine, KinHub crea insieme famiglia e membership del creatore e ti lascia direttamente in KinList.

Se la richiesta viene ripetuta o arriva in concorrenza, KinHub restituisce comunque lo stesso contesto famiglia senza creare duplicati.

## Cosa non viene mostrato

Quando non esiste una membership attiva, KinHub non mostra membri, lista o altri dati condivisi. Anche l'accesso negato resta distinto da uno stato vuoto e non espone se il servizio sia sconosciuto, inattivo o semplicemente non disponibile per la famiglia.

## Offline e privacy

Offline resta disponibile solo la shell pubblica della PWA. KinList non conserva dati personali in cache, non esegue richieste API autenticate e non accoda operazioni remote. Anche il nome famiglia inserito resta solo in memoria finché la pagina è attiva.

## Stato attuale della feature

Questa slice collega KinList al bootstrap condiviso di KinHub, abilita la creazione atomica della prima famiglia e usa il nuovo catalogo persistito dei servizi familiari. Join con codice e lista condivisa arriveranno nelle feature successive.
