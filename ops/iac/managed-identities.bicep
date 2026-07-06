@description('Azure region where the managed identities are provisioned.')
param location string = resourceGroup().location

@description('Name of the Identity API Container App.')
param identityContainerAppName string

@description('Name of the KinRecipe API Container App.')
param kinRecipeContainerAppName string

@description('Name of the KinList API Container App.')
param kinListContainerAppName string

@description('Name of the KinList audio worker Container App.')
param kinListAudioWorkerContainerAppName string

@description('Name of the KinList migration Container Apps Job.')
param kinListMigrationJobName string

resource kinListIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinListContainerAppName}-identity'
  location: location
}

resource kinListAudioWorkerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinListAudioWorkerContainerAppName}-identity'
  location: location
}

resource identityIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${identityContainerAppName}-identity'
  location: location
}

resource kinRecipeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinRecipeContainerAppName}-identity'
  location: location
}

resource kinListMigrationIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${kinListMigrationJobName}-identity'
  location: location
}

output kinListIdentityId string = kinListIdentity.id
output kinListAudioWorkerIdentityId string = kinListAudioWorkerIdentity.id
output kinListMigrationIdentityId string = kinListMigrationIdentity.id
output identityIdentityId string = identityIdentity.id
output kinRecipeIdentityId string = kinRecipeIdentity.id
