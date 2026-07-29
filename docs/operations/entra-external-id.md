# Microsoft Entra External ID

Creare due app registration nel tenant External ID.

## API

1. Registra `KinHub API`.
2. In **Expose an API**, imposta Application ID URI `api://<ENTRA_BACKEND_CLIENT_ID>`.
3. Crea lo scope delegato `access_as_user` e annota il valore completo come `ENTRA_API_SCOPE`.
4. Non creare client secret: l'API valida token delegati e usa managed identity verso Azure.
5. Configura `ENTRA_BACKEND_AUDIENCE` con il solo Application (client) ID GUID dell'API. Il token v2 usa quel GUID nel claim `aud`, non l'Application ID URI `api://...`.

## SPA

1. Registra `KinHub Web` come Single-page application.
2. Aggiungi `http://localhost:5173` e l'URL Static Web Apps come redirect URI SPA.
3. Aggiungi il permesso delegato allo scope KinHub API e concedi il consenso richiesto.
4. Configura `VITE_ENTRA_TENANT_ID`, `VITE_ENTRA_FRONTEND_CLIENT_ID`, `VITE_ENTRA_API_SCOPE`, `VITE_ENTRA_AUTHORITY` e `VITE_ENTRA_REDIRECT_URI` in fase build.

Il tenant clienti External ID e distinto dal tenant Azure usato da OIDC e PostgreSQL. Configura `ENTRA_TENANT_ID` con il Directory (tenant) ID del tenant External ID e `ENTRA_INSTANCE`/`VITE_ENTRA_AUTHORITY` con `https://<tenant-subdomain>.ciamlogin.com/`. Il backend confronta il claim `scp` con il solo nome `access_as_user`; lo scope completo `api://<API_CLIENT_ID>/access_as_user` resta il valore richiesto dal frontend e da Postman.

Per Postman crea una app registration separata `KinHub Postman`: non riusare `KinHub Web` e non aggiungere secret alla SPA. Associa il nuovo client allo stesso user flow, registra `https://oauth.pstmn.io/v1/callback` come redirect URI di tipo **Web**, aggiungi il permesso delegato `access_as_user`, concedi admin consent e crea un client secret limitato all'ambiente di sviluppo. Conserva il secret nel Postman Vault o in una variabile sensibile e non inserirlo nel repository o nei log.

In Postman usa Authorization Code con PKCE, authorization URL `https://<tenant-subdomain>.ciamlogin.com/<ENTRA_TENANT_ID>/oauth2/v2.0/authorize`, token URL equivalente con suffisso `/token`, Client ID di `KinHub Postman`, il relativo client secret e lo scope completo `api://<API_CLIENT_ID>/access_as_user`. Imposta **Client Authentication** su `Send client credentials in body`. Registrare la callback come SPA provoca `AADSTS9002327`, perche Postman riscatta il codice dal proprio backend e non tramite una richiesta CORS dal browser.

Il frontend usa popup con selezione account e configura la cache MSAL in `sessionStorage`, limitata all'origine e alla sessione della scheda, per ripristinare account e token dopo il refresh. `sessionStorage` non contiene `familyId`, membership, risposte API o altri dati personali applicativi: il bootstrap autorevole viene sempre eseguito nuovamente. Il backend convalida issuer, audience, firma, scadenza e scope tramite JWT bearer con `MapInboundClaims=false`.

Per KinHub il bootstrap post-login richiede sempre i claim canonici `iss` e `oid`. Se uno dei due manca o `oid` non e un GUID valido, l'accesso fallisce chiuso con `401` e nessun profilo viene creato come fallback da nome o email.

## Verifica refresh della scheda

Eseguire il percorso con un account Entra di test su Chrome desktop, Chrome Android con PWA installata ed Edge:

1. Accedere e attendere il completamento del bootstrap su `/kinlist`.
2. Verificare che la UI non scriva `familyId`, membership o risposte API in `sessionStorage`, `localStorage`, Cache API o IndexedDB.
3. Ricaricare la stessa scheda e verificare che non venga mostrato il login; MSAL deve rendere disponibile l'account e la PWA deve mostrare loading prima di un nuovo bootstrap.
4. Verificare nella rete una nuova richiesta di bootstrap con acquisizione token silenziosa e nessun dato familiare precedente usato come fallback.
5. Eseguire logout e verificare che la cache MSAL della sessione venga rimossa e che la UI non mostri più il contesto famiglia.
6. Ripetere con sessione Entra scaduta o rete offline: il risultato deve essere rispettivamente nuovo accesso o stato offline, senza dati familiari residui.
