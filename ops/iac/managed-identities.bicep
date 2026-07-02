@description('Azure region where the managed identities are provisioned.')
param location string = resourceGroup().location

@description('Name of the KinList API Container App.')
param kinListContainerAppName string

@description('Name of the KinList migration Container Apps Job.')
param kinListMigrationJobName string

resource kinListIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinListContainerAppName}-identity'
  location: location
}

resource kinListMigrationIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinListMigrationJobName}-identity'
  location: location
}

output kinListIdentityId string = kinListIdentity.id
output kinListMigrationIdentityId string = kinListMigrationIdentity.id
