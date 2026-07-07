targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string

@description('Key Vault name.')
param keyVaultName string

@description('PostgreSQL Flexible Server name.')
param postgresServerName string

@description('PostgreSQL Database name.')
@minLength(1)
@maxLength(63)
param postgresDatabaseName string

@description('PostgreSQL administrator login.')
param postgresAdministratorLogin string

@description('PostgreSQL administrator password.')
@secure()
param postgresAdministratorPassword string

@description('Storage account name dedicated to KinList audio.')
param kinListAudioStorageAccountName string

@description('Log Analytics workspace id for diagnostic settings.')
param logAnalyticsWorkspaceId string

@description('OpenAI endpoint stored as a Key Vault secret.')
param openAiEndpoint string

@description('OpenAI account key stored as a Key Vault secret.')
@secure()
param openAiKey string

@description('Speech endpoint stored as a Key Vault secret.')
param speechEndpoint string

@description('Speech account key stored as a Key Vault secret.')
@secure()
param speechKey string

@description('JWT secret key shared across KinHub services.')
@secure()
param jwtSecret string

@description('Container registry username used by Container Apps to pull private images.')
@minLength(1)
param ghcrUsername string

@description('Container registry password or PAT used by Container Apps to pull private images.')
@secure()
@minLength(1)
param ghcrPassword string

@description('Principal ids granted Key Vault Secrets User (identity, kinrecipe, kinlist, worker, migration).')
param identityPrincipalId string
param kinRecipePrincipalId string
param kinListApiPrincipalId string
param kinListWorkerPrincipalId string
param kinListMigrationPrincipalId string

@description('Resource ids of the same identities (used for stable role-assignment GUID names).')
param identityIdentityId string
param kinRecipeIdentityId string
param kinListApiIdentityId string
param kinListWorkerIdentityId string
param kinListMigrationIdentityId string

var postgresConnectionString = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=${postgresDatabaseName};Port=5432;User Id=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};Ssl Mode=Require;'
var sqlConnectionStringSecretName = 'database-connection-string'
var jwtSecretSecretName = 'jwt-secret'
var openAiEndpointSecretName = 'openai-endpoint'
var openAiKeySecretName = 'openai-key'
var speechEndpointSecretName = 'speech-endpoint'
var speechKeySecretName = 'speech-key'
var ghcrUsernameSecretName = 'ghcr-username'
var ghcrPasswordSecretName = 'ghcr-password'
var kinListAudioContainerName = 'kinlist-audio'
var kinListAudioProcessingQueueName = 'kinlist-audio-processing'
var kinListAudioPoisonQueueName = 'kinlist-audio-poison'

var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageQueueDataMessageProcessorRoleId = '8a0f0c08-91a1-4084-bc3d-661d67233fed'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enablePurgeProtection: true
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 90
    tenantId: tenant().tenantId
  }
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: postgresAdministratorLogin
    administratorLoginPassword: postgresAdministratorPassword
    version: '17'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// IAC-07 TODO: The 0.0.0.0 rule below opens the PostgreSQL server to all Azure services
// (this is the special "Allow Azure services" rule, not a public 0.0.0.0/0 CIDR). To restrict
// access to only the Container Apps, replace this with either:
//   - a VNet-integrated Container Apps environment + private endpoint / delegated subnet rule, or
//   - explicit firewall rules for the Container Apps' outbound static IPs.
// Those IPs are not derivable from this codebase, so the rule is left in place to avoid breaking
// connectivity. Do not remove without a validated replacement.
resource postgresFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name: postgresDatabaseName
}

// IAC-09: audit-only diagnostic settings routed to the existing Log Analytics workspace.
resource keyVaultDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyVault
  name: 'audit'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
  }
}

resource postgresDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: postgresServer
  name: 'audit'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
      }
    ]
  }
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: sqlConnectionStringSecretName
  properties: {
    value: postgresConnectionString
  }
}

resource openAiEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: openAiEndpointSecretName
  properties: {
    value: openAiEndpoint
  }
}

// IAC-06 TODO: This stores the OpenAI account API key (listKeys) in Key Vault. Prefer keyless
// auth via managed identity (DefaultAzureCredential) with the "Cognitive Services OpenAI User"
// role (5e0bd9bd-7b93-4f28-af87-19fc36ad61bd) assigned to the consuming Container App identities,
// then drop this secret. Left in place until the apps are confirmed to use DefaultAzureCredential
// for the OpenAI endpoint to avoid breaking runtime access.
resource openAiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: openAiKeySecretName
  properties: {
    value: openAiKey
  }
}

resource speechEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: speechEndpointSecretName
  properties: {
    value: speechEndpoint
  }
}

// IAC-06 TODO: This stores the Speech account API key (listKeys) in Key Vault. Prefer keyless
// auth via managed identity (DefaultAzureCredential) with the "Cognitive Services User" role
// (a97b65f3-24c7-4388-baec-2e87135dc908) assigned to the consuming Container App identities,
// then drop this secret. Left in place until the apps are confirmed to use DefaultAzureCredential
// for the Speech endpoint to avoid breaking runtime access.
resource speechKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: speechKeySecretName
  properties: {
    value: speechKey
  }
}

resource jwtSecretKvSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: jwtSecretSecretName
  properties: {
    value: jwtSecret
  }
}

resource ghcrUsernameSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: ghcrUsernameSecretName
  properties: {
    value: ghcrUsername
  }
}

resource ghcrPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: ghcrPasswordSecretName
  properties: {
    value: ghcrPassword
  }
}

resource kinListAudioStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: kinListAudioStorageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource kinListAudioBlobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: kinListAudioStorageAccount
  name: 'default'
}

resource kinListAudioBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: kinListAudioBlobService
  name: kinListAudioContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource kinListAudioManagementPolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: kinListAudioStorageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'DeleteExpiredKinListAudioBlobs'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                '${kinListAudioContainerName}/'
              ]
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 1
                }
              }
            }
          }
        }
      ]
    }
  }
}

resource kinListAudioQueueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: kinListAudioStorageAccount
  name: 'default'
}

resource kinListAudioProcessingQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: kinListAudioQueueService
  name: kinListAudioProcessingQueueName
}

resource kinListAudioPoisonQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: kinListAudioQueueService
  name: kinListAudioPoisonQueueName
}

resource identityKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, identityIdentityId, keyVaultSecretsUserRoleId)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinRecipeKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinRecipeIdentityId, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinRecipePrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinListApiIdentityId, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinListApiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListMigrationKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinListMigrationIdentityId, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinListMigrationPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListAudioWorkerKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinListWorkerIdentityId, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinListWorkerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListStorageBlobContributorForApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kinListAudioStorageAccount
  name: guid(kinListAudioStorageAccount.id, kinListApiIdentityId, storageBlobDataContributorRoleId)
  properties: {
    principalId: kinListApiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
  }
}

resource kinListStorageQueueContributorForApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kinListAudioStorageAccount
  name: guid(kinListAudioStorageAccount.id, kinListApiIdentityId, storageQueueDataContributorRoleId)
  properties: {
    principalId: kinListApiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
  }
}

resource kinListStorageBlobContributorForWorker 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kinListAudioStorageAccount
  name: guid(kinListAudioStorageAccount.id, kinListWorkerIdentityId, storageBlobDataContributorRoleId)
  properties: {
    principalId: kinListWorkerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
  }
}

resource kinListStorageQueueProcessorForWorker 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kinListAudioStorageAccount
  name: guid(kinListAudioStorageAccount.id, kinListWorkerIdentityId, storageQueueDataMessageProcessorRoleId)
  properties: {
    principalId: kinListWorkerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataMessageProcessorRoleId)
  }
}

output sqlConnectionStringSecretUri string = sqlConnectionStringSecret.properties.secretUri
output jwtSecretUri string = jwtSecretKvSecret.properties.secretUri
output openAiEndpointSecretUri string = openAiEndpointSecret.properties.secretUri
output openAiKeySecretUri string = openAiKeySecret.properties.secretUri
output speechEndpointSecretUri string = speechEndpointSecret.properties.secretUri
output ghcrPasswordSecretUri string = ghcrPasswordSecret.properties.secretUri
output storageAccountName string = kinListAudioStorageAccount.name
output kinListAudioContainerName string = kinListAudioContainerName
output kinListAudioProcessingQueueName string = kinListAudioProcessingQueueName
output kinListAudioPoisonQueueName string = kinListAudioPoisonQueueName
