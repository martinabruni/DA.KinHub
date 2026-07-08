@description('Azure region where the managed identities are provisioned.')
param location string = resourceGroup().location

@description('Name of the Identity API Container App.')
param identityContainerAppName string

@description('Name of the Function App hosting all non-identity routes (lists, audio operations, fridges, recipe books, recipe assistant) and the audio-processing queue trigger.')
param functionAppName string

resource identityIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${identityContainerAppName}-identity'
  location: location
}

// Single identity consolidating the former kinRecipe, kinList API, kinList audio-worker and
// kinList migration identities, now that App.Functions hosts all of that functionality.
resource functionAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${functionAppName}-identity'
  location: location
}

output identityIdentityId string = identityIdentity.id
output functionAppIdentityId string = functionAppIdentity.id
