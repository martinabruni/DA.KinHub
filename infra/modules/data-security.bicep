param location string
param storageAccountName string
param keyVaultName string
param tags object = {}
param deploymentContainerName string
param applicationContainerName string = 'documents'
param enablePurgeProtection bool = true
param azureTenantId string
param entraAdministratorPrincipalName string
param entraAdministratorObjectId string
@allowed(['User', 'Group', 'ServicePrincipal'])
param entraAdministratorPrincipalType string = 'ServicePrincipal'
param administratorLogin string
@secure()
param administratorPassword string
param postgresName string
param databaseName string = 'kinhub'
param postgresSkuName string = 'Standard_B1ms'
param postgresStorageSizeGB int = 32
param postgresVersion string = '16'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
    networkAcls: { bypass: 'AzureServices', defaultAction: 'Allow' }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: { enabled: true, days: 7 }
    containerDeleteRetentionPolicy: { enabled: true, days: 7 }
  }
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: deploymentContainerName
  properties: { publicAccess: 'None' }
}

resource applicationContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: applicationContainerName
  properties: { publicAccess: 'None' }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: 'Enabled'
    networkAcls: { bypass: 'AzureServices', defaultAction: 'Allow' }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresName
  location: location
  tags: tags
  sku: { name: postgresSkuName, tier: 'Burstable' }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: postgresVersion
    storage: { storageSizeGB: postgresStorageSizeGB, autoGrow: 'Enabled' }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
    network: { publicNetworkAccess: 'Enabled' }
    authConfig: { activeDirectoryAuth: 'Enabled', passwordAuth: 'Disabled', tenantId: azureTenantId }
  }
}

resource entraAdministrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = if (!empty(entraAdministratorPrincipalName) && !empty(entraAdministratorObjectId)) {
  parent: postgres
  name: entraAdministratorObjectId
  properties: {
    principalName: entraAdministratorPrincipalName
    principalType: entraAdministratorPrincipalType
    tenantId: azureTenantId
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: databaseName
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}

resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

output storageAccountId string = storage.id
output storageAccountName string = storage.name
output storageBlobEndpoint string = storage.properties.primaryEndpoints.blob
output deploymentContainerName string = deploymentContainer.name
output deploymentContainerUri string = '${storage.properties.primaryEndpoints.blob}${deploymentContainer.name}'
output applicationContainerName string = applicationContainer.name
output keyVaultName string = vault.name
output postgresName string = postgres.name
output postgresFqdn string = postgres.properties.fullyQualifiedDomainName
output postgresDatabaseName string = database.name
