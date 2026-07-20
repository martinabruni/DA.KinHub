---
slug: kinlist
locale: it
title: KinList
description: Verifica del percorso corretto dopo il login, onboarding obbligatorio e shell offline sicura.
---

## Cosa succede dopo il login

KinList verifica sempre in modo autorevole se il tuo profilo ha una membership familiare attiva. Se la verifica trova una famiglia attiva, la PWA ti porta nell'area KinList. Se la famiglia manca oppure la membership non e attiva, vedi solo l'onboarding con le azioni **Crea una famiglia** e **Unisciti con un codice**.

## Cosa non viene mostrato

Quando non esiste una membership attiva, KinHub non mostra nome famiglia, membri, lista o altri dati condivisi. Anche l'accesso negato resta distinto da uno stato vuoto.

## Offline e privacy

Offline resta disponibile solo la shell pubblica della PWA. KinList non conserva dati personali in cache, non esegue richieste API autenticate e non accoda operazioni remote.

## Stato attuale della feature

Questa slice introduce accesso, bootstrap e autorizzazione `Family`. Creazione famiglia, join e lista condivisa arriveranno nelle feature successive del backlog KinList.
