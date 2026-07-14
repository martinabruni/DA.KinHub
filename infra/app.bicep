param namePrefix string = 'kinhub'
param location string = resourceGroup().location
param environmentName string = 'dev'
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-${environmentName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: '${namePrefix}-${environmentName}-api'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|<ACR_LOGIN_SERVER>/kinhub:<VERSION>-<SHA>'
      alwaysOn: false
      healthCheckPath: '/health/live'
    }
  }
}
output apiName string = app.name
