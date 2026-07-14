param name string
param location string = resourceGroup().location
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
  tags: {
    app: 'KinHub'
  }
}
output loginServer string = acr.properties.loginServer
