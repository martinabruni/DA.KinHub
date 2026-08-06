using '../main.bicep'

param environmentName = 'dev'
param location = 'italynorth'
param staticWebAppLocation = 'westeurope'
param storageAccountName = 'kinhubdevlauj4ewc'
param keyVaultName = 'kinhub-dev-lauj4ewc'
param logAnalyticsName = 'kinhub-dev-lauj4ewc-log'
param applicationInsightsName = 'kinhub-dev-lauj4ewc-appi'
param postgresServerName = 'kinhub-dev-lauj4ewc-pg'
param functionAppName = 'kinhub-dev-lauj4ewc-func'
param functionPlanName = 'kinhub-dev-lauj4ewc-fc'
param staticWebAppName = 'kinhub-dev-lauj4ewc-web'
param runtimeName = 'dotnet-isolated'
param runtimeVersion = '10.0'
param instanceMemoryMB = 2048
param maximumInstanceCount = 20
param alwaysReadyInstanceCount = 0
param deploymentBlobContainerName = 'function-packages'
param azureTenantId = '<AZURE_TENANT_ID>'
param entraInstance = 'https://<ENTRA_TENANT_SUBDOMAIN>.ciamlogin.com/'
param entraTenantId = '<ENTRA_TENANT_ID>'
param entraBackendAudience = '<ENTRA_BACKEND_CLIENT_ID>'
param entraApiScopeName = 'access_as_user'
param postgresEntraAdministratorName = '<POSTGRES_ENTRA_ADMIN_NAME>'
param postgresEntraAdministratorObjectId = '<POSTGRES_ENTRA_ADMIN_OBJECT_ID>'
param postgresEntraAdministratorPrincipalType = 'ServicePrincipal'
param postgresAdminUsername = '<POSTGRES_ADMIN_USERNAME>'
param postgresAdminPassword = '<POSTGRES_ADMIN_PASSWORD>'
param allowedOrigins = [
  'http://localhost:5173'
]
param enableVnetIntegration = false
param virtualNetworkSubnetResourceId = ''
param enablePurgeProtection = true
param logRetentionDays = 30
param logDailyCapGb = 1
param tags = {
  workload: 'kinhub'
  environment: 'dev'
  owner: 'martinabruni'
  costClassification: 'personal-low-cost'
}
