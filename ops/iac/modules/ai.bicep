targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string

@description('Azure OpenAI account name.')
param openAiAccountName string

@description('Azure OpenAI SKU name.')
param openAiSkuName string

@description('Azure AI Speech account name used by the KinList audio pipeline.')
param speechAccountName string

@description('Azure AI Speech SKU name.')
param speechSkuName string

@description('Principal id of the KinList API identity granted Speech/OpenAI user roles.')
param kinListApiPrincipalId string

@description('Resource id of the KinList API identity (used for stable role-assignment GUID names).')
param kinListApiIdentityId string

@description('Principal id of the KinList audio worker identity granted Speech/OpenAI user roles.')
param kinListWorkerPrincipalId string

@description('Resource id of the KinList audio worker identity (used for stable role-assignment GUID names).')
param kinListWorkerIdentityId string

// Built-in Azure RBAC role definition IDs.
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'
var cognitiveServicesOpenAiUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiAccountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: openAiSkuName
  }
  properties: {
    customSubDomainName: openAiAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource speechAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: speechAccountName
  location: location
  kind: 'SpeechServices'
  sku: {
    name: speechSkuName
  }
  properties: {
    customSubDomainName: speechAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
  name: 'gpt-4o-mini'
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
    }
  }
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
  name: 'text-embedding-3-small'
  dependsOn: [
    gpt4oDeployment
  ]
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-small'
    }
  }
}

resource kinListSpeechUserForApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: speechAccount
  name: guid(speechAccount.id, kinListApiIdentityId, cognitiveServicesUserRoleId)
  properties: {
    principalId: kinListApiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
  }
}

resource kinListOpenAiUserForApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: openAiAccount
  name: guid(openAiAccount.id, kinListApiIdentityId, cognitiveServicesOpenAiUserRoleId)
  properties: {
    principalId: kinListApiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAiUserRoleId)
  }
}

resource kinListSpeechUserForWorker 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: speechAccount
  name: guid(speechAccount.id, kinListWorkerIdentityId, cognitiveServicesUserRoleId)
  properties: {
    principalId: kinListWorkerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
  }
}

resource kinListOpenAiUserForWorker 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: openAiAccount
  name: guid(openAiAccount.id, kinListWorkerIdentityId, cognitiveServicesOpenAiUserRoleId)
  properties: {
    principalId: kinListWorkerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAiUserRoleId)
  }
}

output openAiEndpoint string = openAiAccount.properties.endpoint
@secure()
output openAiKey string = openAiAccount.listKeys().key1
output speechEndpoint string = speechAccount.properties.endpoint
@secure()
output speechKey string = speechAccount.listKeys().key1
output gpt4oDeploymentName string = gpt4oDeployment.name
output embeddingDeploymentName string = embeddingDeployment.name
