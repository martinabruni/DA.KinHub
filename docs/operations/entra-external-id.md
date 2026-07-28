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

Per Postman usa Authorization Code con PKCE, callback `https://oauth.pstmn.io/v1/callback`, authorization URL `https://<tenant-subdomain>.ciamlogin.com/<ENTRA_TENANT_ID>/oauth2/v2.0/authorize` e token URL equivalente con suffisso `/token`. Registra la callback come redirect URI SPA sulla app registration frontend.

Il frontend usa popup con selezione account e mantiene i token soltanto in memoria. Il backend convalida issuer, audience, firma, scadenza e scope tramite JWT bearer con `MapInboundClaims=false`.

Per KinHub il bootstrap post-login richiede sempre i claim canonici `iss` e `oid`. Se uno dei due manca o `oid` non e un GUID valido, l'accesso fallisce chiuso con `401` e nessun profilo viene creato come fallback da nome o email.
