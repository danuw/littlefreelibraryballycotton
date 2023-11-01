param name string
param location string = resourceGroup().location
param tags object = {}

param sku object = {
  name: 'Free'
  tier: 'Free'
}

resource web 'Microsoft.Web/staticSites@2022-03-01' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'SwaCli'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource staticSites_stapp_web_hbbe3mhgpzhpm_name_www_riverviewer_co_uk 'Microsoft.Web/staticSites/customDomains@2022-09-01' = {
  parent: web
  name: 'www.littlefreelibraryballycotton.top'
  location: location
  properties: {
  }
}         

output name string = web.name
output uri string = 'https://${web.properties.defaultHostname}'
