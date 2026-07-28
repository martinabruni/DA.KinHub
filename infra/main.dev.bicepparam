using './app.bicep'

param environmentName = 'dev'
param location = 'italynorth'
param namingPrefix = 'kinhub'
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
