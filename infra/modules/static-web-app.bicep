param name string
param location string = 'westeurope'
param functionAppId string
param functionAppRegion string = 'italynorth'
param tags object = {}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: { name: 'Standard', tier: 'Standard' }
  properties: {
    allowConfigFileUpdates: true
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource linkedBackend 'Microsoft.Web/staticSites/linkedBackends@2022-09-01' = {
  parent: staticWebApp
  name: 'api'
  properties: {
    backendResourceId: functionAppId
    region: functionAppRegion
  }
}

output id string = staticWebApp.id
output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
