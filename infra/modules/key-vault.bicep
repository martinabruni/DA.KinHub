param name string
param location string
param tags object = {}
param enablePurgeProtection bool = false
param secretName string
@secure()
param secretValue string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
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

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: secretName
  properties: {
    value: secretValue
    contentType: 'PostgreSQL connection string'
  }
}

output id string = vault.id
output name string = vault.name
output uri string = vault.properties.vaultUri
output createdSecretName string = secretName
