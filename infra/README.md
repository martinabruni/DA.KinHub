# Infrastruttura KinHub

Il template opera nel resource group esistente `rg-kinhub-dev`. Static Web Apps usa sempre `westeurope`; le altre risorse usano `location` (`italynorth` in dev).

Il modulo Function deriva dal template HTTP C# Flex Consumption ufficiale Azure Functions: conserva `FC1`, `functionAppConfig`, container Blob privato, autenticazione managed identity e RBAC. KinHub usa system-assigned identity per ridurre risorse e gestione in un progetto personale.

```bash
az bicep build --file infra/app.bicep
az deployment group validate --resource-group rg-kinhub-dev --template-file infra/app.bicep --parameters infra/main.dev.bicepparam --parameters postgresAdminUsername='<VALUE>' postgresAdminPassword='<VALUE>' azureTenantId='<AZURE_TENANT_ID>' entraInstance='https://<TENANT_SUBDOMAIN>.ciamlogin.com/' entraTenantId='<ENTRA_TENANT_ID>' entraBackendAudience='<ENTRA_BACKEND_CLIENT_ID>'
```

Non eseguire il deploy senza confermare subscription, policy, provider, quote e costi. `main.dev.bicepparam` contiene solo placeholder. Memoria, scala e always-ready si modificano qui, non in GitHub Variables.

Per VNet integration imposta `enableVnetIntegration=true` e passa un subnet resource ID già delegato e compatibile; il template non crea una VNet per default.
