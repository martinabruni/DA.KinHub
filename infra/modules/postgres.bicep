param name string
param databaseName string = 'kinhub'
param location string
param tags object = {}
param entraTenantId string
param entraAdministratorPrincipalName string
param entraAdministratorObjectId string
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param entraAdministratorPrincipalType string = 'ServicePrincipal'
param administratorLogin string
@secure()
param administratorPassword string
param skuName string = 'Standard_B1ms'
@minValue(32)
param storageSizeGB int = 32
param postgresVersion string = '16'
param allowAzureServices bool = true

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: name
  location: location
  tags: tags
  sku: { name: skuName, tier: 'Burstable' }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: postgresVersion
    storage: {
      storageSizeGB: storageSizeGB
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: { mode: 'Disabled' }
    network: { publicNetworkAccess: 'Enabled' }
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
      tenantId: entraTenantId
    }
  }
}

resource entraAdministrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  parent: server
  name: entraAdministratorObjectId
  properties: {
    principalName: entraAdministratorPrincipalName
    principalType: entraAdministratorPrincipalType
    tenantId: entraTenantId
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}

resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (allowAzureServices) {
  parent: server
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

output id string = server.id
output name string = server.name
output fqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
output entraAdministratorName string = entraAdministrator.properties.principalName
