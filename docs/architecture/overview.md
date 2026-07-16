# Architettura KinHub

KinHub usa un monolite modulare pragmatico: una SPA React/PWA comunica via HTTPS con una Function App .NET 10 Isolated. Il backend separa dominio, business, infrastruttura e applicazione. PostgreSQL è l'unico datastore iniziale.

Il dominio non dipende da framework. Il business orchestra use case e contratti. Infrastructure contiene EF Core e integrazioni tecniche. Applications espone trigger HTTP e composition root.

Azure usa una Function App per piano Flex Consumption, Static Web Apps per il frontend, PostgreSQL Flexible Server, Storage identity-based, Key Vault, Application Insights e Log Analytics. Il deploy del codice è separato dal deploy infrastrutturale.

## Decisioni

- Niente CQRS o mediator finché non esiste un bisogno misurato.
- Niente migration lunghe al cold start; in produzione si usa un migration bundle in pipeline.
- Le skill contengono conoscenza, non codice caricato dinamicamente.
- System-assigned managed identity riduce oggetti e credenziali in un progetto personale.
- Rete pubblica controllata per dev; VNet resta opzionale e disabilitata per default.
