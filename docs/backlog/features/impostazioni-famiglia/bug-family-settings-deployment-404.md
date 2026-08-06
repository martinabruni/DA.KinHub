# BUG-FEAT-004-001 - Family Settings non raggiungibile nel deployment dev

- **Feature interessata**: FEAT-004 `impostazioni-famiglia`
- **Tipo**: regressione di deployment, API Family e PWA
- **Stato**: aperto
- **Breaking change prodotto**: no

## Segnalazione

Nell'ambiente dev la pagina delle impostazioni famiglia non riesce a caricare i
dati. La richiesta membri restituisce `404 Not Found`:

```text
GET https://kinhub-dev-lauj4ewc-func.azurewebsites.net/api/kinhub/families/members?familyId=d7deecd2-9007-40f9-8aa6-0df35bfca016&pageSize=50
```

La pagina emette inoltre richieste verso gli endpoint di dettaglio e inviti:

```text
GET https://kinhub-dev-lauj4ewc-func.azurewebsites.net/api/kinhub/families/details?familyId=d7deecd2-9007-40f9-8aa6-0df35bfca016
GET https://kinhub-dev-lauj4ewc-func.azurewebsites.net/api/kinhub/families/invitations?familyId=d7deecd2-9007-40f9-8aa6-0df35bfca016&pageSize=50
```

Durante l'installazione del service worker Workbox fallisce anche la
precaching di `staticwebapp.config.json` perché l'asset distribuito risponde
`404`:

```text
bad-precaching-response
https://red-mushroom-077de9003.7.azurestaticapps.net/staticwebapp.config.json
```

## Impatto

- La route Family Settings non può mostrare nome, membri o inviti.
- Un errore di precaching può impedire l'installazione corretta del service
  worker e lasciare la PWA in uno stato incoerente.
- Non è possibile distinguere dal solo sintomo se il `404` API deriva da
  Function non pubblicate, route/base path non allineati o versione backend
  non corrispondente al frontend; la causa va verificata nel deployment.

## Evidenza nel repository

- Le route autorevoli sono definite in
  `src/backend/applications/DA.KinHub.Functions/Http/ApiRoutes.cs`.
- Le Function `KinHubFamilyDetails`, `KinHubFamilyMembers` e
  `KinHubFamilyInvitations` sono definite in
  `src/backend/applications/DA.KinHub.Functions/Functions/KinHubFamilyFunctions.cs`.
- `src/frontend/public/staticwebapp.config.json` esiste e
  `src/frontend/vite.config.ts` lo dichiara tra gli asset inclusi della PWA.

## Risultato atteso

- Le tre route Family pubblicate nell'ambiente dev rispondono secondo il
  contratto FEAT-004 e non risultano `404` quando la richiesta è autenticata e
  autorizzata.
- Il deployment frontend pubblica `staticwebapp.config.json` all'URL atteso,
  oppure il service worker non lo inserisce nel precache se l'asset non deve
  essere pubblico. La scelta deve restare coerente con la configurazione
  Static Web Apps e con il packaging effettivo.
- L'installazione del service worker termina senza
  `bad-precaching-response`.

## Criteri di accettazione

### AC-BUG-001 - Route Family presenti nel runtime dev

- **Dato** un deployment backend dev completato dal workflow previsto
- **Quando** un membro autorizzato richiama dettaglio, membri e inviti con il
  `familyId` del contesto e `pageSize=50`
- **Allora** nessuna delle tre route restituisce `404`; gli esiti applicativi
  sono quelli documentati in OpenAPI, inclusi `200`, `401`, `403` e gli errori
  di dipendenza pertinenti
- **Fonte**: FEAT-004, `feature.plan.md` sezioni 7 e 14; `AGENTS.md` regole
  HTTP/deployment

### AC-BUG-002 - Asset Static Web Apps coerente

- **Dato** il pacchetto frontend prodotto dalla build usata per il deployment
- **Quando** viene richiesto `/staticwebapp.config.json` sull'origine pubblica
- **Allora** l'asset risponde `200` con `application/json`, oppure è escluso
  esplicitamente dal precache e la configurazione risultante resta valida per
  il fallback SPA
- **Fonte**: `src/frontend/public/staticwebapp.config.json`,
  `src/frontend/vite.config.ts`, `AGENTS.md` regole PWA/deployment

### AC-BUG-003 - Installazione PWA senza errore di precaching

- **Dato** un browser con service worker non installato o aggiornato
- **Quando** apre l'origine frontend dev e completa l'installazione del
  service worker
- **Allora** Workbox non registra `bad-precaching-response` e il service worker
  raggiunge lo stato installato
- **Fonte**: requisiti PWA di FEAT-004 e `AGENTS.md` regole PWA

## Verifica richiesta

- Ispezionare il contenuto dello ZIP/dist frontend e confermare la presenza o
  l'esclusione intenzionale di `staticwebapp.config.json`.
- Ispezionare il manifest delle risorse precache generato da Workbox e
  verificare che ogni URL risponda `200` sull'origine distribuita.
- Verificare le Function pubblicate nell'ambiente dev, il runtime effettivo e
  il mapping delle route con `health/live`, `api/version` e uno smoke test
  autenticato per i tre endpoint Family.
- Confermare che il backend e il frontend provengano dallo stesso commit o da
  una combinazione compatibile, senza stampare token, claim o dati familiari
  nei log.
- Ripetere il caricamento di `/settings/family`, aggiornamento, navigazione
  avanti/indietro e installazione PWA dopo un nuovo deployment.

## Touchpoint probabili

- `.github/workflows/release.yml` e packaging One Deploy backend.
- `.github/workflows/release.yml`, configurazione Static Web Apps e
  output `src/frontend/dist`.
- `src/backend/applications/DA.KinHub.Functions/Http/ApiRoutes.cs` e
  `Functions/KinHubFamilyFunctions.cs`, soltanto se la verifica dimostra un
  disallineamento reale del contratto.
- `src/frontend/vite.config.ts` e `src/frontend/public/staticwebapp.config.json`.

## Definition of Done del bug

- La causa dei due `404` è documentata separatamente per backend e frontend.
- Sono presenti test o smoke test di regressione per le route e per il
  packaging/precaching dell'asset.
- Sono verificati runtime live, `health/live`, `api/version`, route Family,
  fallback SPA e installazione service worker.
- Le verifiche non espongono secret, token, PII, `familyId` o cursori nei log.
- La correzione non introduce nuove risorse Azure, cache di API autenticate o
  deviazioni dal contratto FEAT-004.
