# Microsoft Entra External ID

Creare due app registration nel tenant External ID.

## API

1. Registra `KinHub API`.
2. In **Expose an API**, imposta Application ID URI `api://<ENTRA_BACKEND_CLIENT_ID_OR_AUDIENCE>`.
3. Crea lo scope delegato `access_as_user` e annota il valore completo come `ENTRA_API_SCOPE`.
4. Non creare client secret: l'API valida token delegati e usa managed identity verso Azure.

## SPA

1. Registra `KinHub Web` come Single-page application.
2. Aggiungi `http://localhost:5173` e l'URL Static Web Apps come redirect URI SPA.
3. Aggiungi il permesso delegato allo scope KinHub API e concedi il consenso richiesto.
4. Configura `VITE_ENTRA_TENANT_ID`, `VITE_ENTRA_FRONTEND_CLIENT_ID`, `VITE_ENTRA_API_SCOPE` e `VITE_ENTRA_REDIRECT_URI` in fase build.

Il frontend usa popup con selezione account. Il backend convalida issuer, audience, firma, scadenza e scope tramite JWT bearer. I placeholder non sono validi quando `Entra:Enabled=true`.
