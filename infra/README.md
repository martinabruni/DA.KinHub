# Infrastruttura KinHub

Il template opera nel resource group esistente `rg-kinhub-dev`. I nomi delle risorse dev confermati sono in `infra/environments/dev.bicepparam`. Static Web Apps usa `westeurope`; le altre risorse usano `italynorth`.

I moduli raggruppano monitoring, data/security, Functions e Static Web Apps. KinHub usa `FC1`, `functionAppConfig`, container Blob privato, autenticazione managed identity, RBAC e system-assigned identity.

```bash
az bicep build --file infra/main.bicep
mkdir -p artifacts/infra
az bicep build-params --file infra/environments/dev.bicepparam --outfile artifacts/infra/dev.parameters.json
az deployment group validate --resource-group rg-kinhub-dev --template-file infra/main.bicep --parameters @artifacts/infra/dev.parameters.json postgresAdminUsername='<VALUE>' postgresAdminPassword='<VALUE>' azureTenantId='<AZURE_TENANT_ID>' entraInstance='https://<TENANT_SUBDOMAIN>.ciamlogin.com/' entraTenantId='<ENTRA_TENANT_ID>' entraBackendAudience='<ENTRA_BACKEND_CLIENT_ID>' postgresEntraAdministratorName='<POSTGRES_ENTRA_ADMIN_NAME>' postgresEntraAdministratorObjectId='<POSTGRES_ENTRA_ADMIN_OBJECT_ID>'
```

Non eseguire il deploy senza confermare subscription, policy, provider, quote e costi. `dev.bicepparam` contiene solo placeholder per i valori sensibili. Memoria, scala e always-ready si modificano qui, non in GitHub Variables.

Per VNet integration imposta `enableVnetIntegration=true` e passa un subnet resource ID già delegato e compatibile; il template non crea una VNet per default.
