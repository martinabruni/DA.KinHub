targetScope = 'resourceGroup'

@allowed(['dev', 'test', 'prod'])
param environmentName string = 'dev'
param location string = 'italynorth'
param staticWebAppLocation string = 'westeurope'
param storageAccountName string
param keyVaultName string
param logAnalyticsName string
param applicationInsightsName string
param postgresServerName string
param functionAppName string
param functionPlanName string
param staticWebAppName string
@allowed(['dotnet-isolated'])
param runtimeName string = 'dotnet-isolated'
param runtimeVersion string = '10.0'
@allowed([512, 2048, 4096])
param instanceMemoryMB int = 2048
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int = 20
@minValue(0)
param alwaysReadyInstanceCount int = 0
param deploymentBlobContainerName string = 'function-packages'
param azureTenantId string
param entraInstance string
param entraTenantId string
param entraBackendAudience string
param entraApiScopeName string = 'access_as_user'
param postgresEntraAdministratorName string
param postgresEntraAdministratorObjectId string
@allowed(['User', 'Group', 'ServicePrincipal'])
param postgresEntraAdministratorPrincipalType string = 'ServicePrincipal'
param postgresAdminUsername string
@secure()
param postgresAdminPassword string
param allowedOrigins array = ['http://localhost:5173']
param enableVnetIntegration bool = false
param virtualNetworkSubnetResourceId string = ''
param enablePurgeProtection bool = true
@minValue(30)
@maxValue(730)
param logRetentionDays int = 30
param logDailyCapGb int = 1
param tags object = {
  workload: 'kinhub'
  environment: environmentName
  owner: 'martinabruni'
  costClassification: 'personal-low-cost'
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    applicationInsightsName: applicationInsightsName
    retentionDays: logRetentionDays
    dailyCapGb: logDailyCapGb
    tags: tags
  }
}

module dataSecurity './modules/data-security.bicep' = {
  name: 'data-security'
  params: {
    location: location
    storageAccountName: storageAccountName
    keyVaultName: keyVaultName
    deploymentContainerName: deploymentBlobContainerName
    postgresName: postgresServerName
    enablePurgeProtection: enablePurgeProtection
    azureTenantId: azureTenantId
    entraAdministratorPrincipalName: postgresEntraAdministratorName
    entraAdministratorObjectId: postgresEntraAdministratorObjectId
    entraAdministratorPrincipalType: postgresEntraAdministratorPrincipalType
    administratorLogin: postgresAdminUsername
    administratorPassword: postgresAdminPassword
    tags: tags
  }
}

module functions './modules/functions.bicep' = {
  name: 'functions'
  params: {
    name: functionAppName
    planName: functionPlanName
    location: location
    tags: tags
    runtimeName: runtimeName
    runtimeVersion: runtimeVersion
    instanceMemoryMB: instanceMemoryMB
    maximumInstanceCount: maximumInstanceCount
    alwaysReadyInstanceCount: alwaysReadyInstanceCount
    storageAccountName: dataSecurity.outputs.storageAccountName
    storageAccountId: dataSecurity.outputs.storageAccountId
    storageBlobEndpoint: dataSecurity.outputs.storageBlobEndpoint
    deploymentContainerName: dataSecurity.outputs.deploymentContainerName
    applicationContainerName: dataSecurity.outputs.applicationContainerName
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    entraInstance: entraInstance
    entraTenantId: entraTenantId
    entraBackendAudience: entraBackendAudience
    entraApiScopeName: entraApiScopeName
    environmentName: environmentName
    postgresHost: dataSecurity.outputs.postgresFqdn
    postgresDatabaseName: dataSecurity.outputs.postgresDatabaseName
    allowedOrigins: allowedOrigins
    enableVnetIntegration: enableVnetIntegration
    virtualNetworkSubnetResourceId: virtualNetworkSubnetResourceId
  }
}

module staticWebApp './modules/static-web-app.bicep' = {
  name: 'static-web-app'
  params: {
    name: staticWebAppName
    location: staticWebAppLocation
    functionAppId: functions.outputs.id
    functionAppRegion: location
    tags: tags
  }
}

output functionAppName string = functions.outputs.name
output functionAppId string = functions.outputs.id
output functionAppHostname string = functions.outputs.hostname
output functionAppPrincipalId string = functions.outputs.principalId
output functionPlanId string = functions.outputs.planId
output storageAccountName string = dataSecurity.outputs.storageAccountName
output storageAccountId string = dataSecurity.outputs.storageAccountId
output deploymentContainerName string = dataSecurity.outputs.deploymentContainerName
output deploymentContainerUri string = dataSecurity.outputs.deploymentContainerUri
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output postgresServerName string = dataSecurity.outputs.postgresName
output postgresServerFqdn string = dataSecurity.outputs.postgresFqdn
output postgresDatabaseName string = dataSecurity.outputs.postgresDatabaseName
output keyVaultName string = dataSecurity.outputs.keyVaultName
