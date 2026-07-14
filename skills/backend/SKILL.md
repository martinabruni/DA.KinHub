---
id: backend
name: backend
area: backend
description: Servizi business riutilizzabili e contratti DDD pragmatici
version: 1.0.0
---

# Backend KinHub

## Scopo

Guidare l'evoluzione .NET 10 rispettando dipendenze DDD pragmatiche e separando chiaramente regole, orchestrazione, adattatori e HTTP.

## Quando usare

Per entity/value object, use case, servizi riutilizzabili, repository, EF Core, configurazione DI, endpoint, autenticazione, health e metadata applicativi. Leggere anche la skill architecture quando cambiano confini.

## Quando non usare

Non usarla per UI o pipeline release. Non promuovere automaticamente a servizio condiviso una classe usata da un solo use case. Non introdurre mediator/CQRS, domain event o repository generici senza un problema concreto.

## Componenti e servizi disponibili

Il [catalogo backend](catalog.json) descrive solo simboli verificati. Attualmente `Project` applica nome obbligatorio e trimming; `IProjectService`/`ProjectService` creano un progetto; `Program.cs` registra il servizio singleton ed espone health, version, status e create project. Infrastructure è ancora vuota: PostgreSQL, EF, repository, migration e readiness DB non vanno dichiarati disponibili.

## API e interfacce

Direzione: Domain non referencia altri layer; Business referencia Domain; Infrastructure implementa contratti definiti nel layer più interno che li usa; Applications compone e traduce HTTP. Le entity proteggono invarianti nel costruttore/metodi e non espongono setter pubblici. I use case restituiscono DTO o risultati intenzionali quando esporre entity legherebbe HTTP al dominio.

Gli endpoint convertono errori noti in Problem Details e non lasciano che eccezioni di validazione diventino 500. `/api/version` legge `VERSION` tramite metadata iniettato, non duplica SemVer. La lifetime DI deve seguire lo stato: singleton soltanto per servizi realmente stateless e senza dipendenze scoped.

## Esempi

Per una nuova regola: implementarla in Domain e testarla senza host. Per un use case: definire il contratto in Business, validare input, orchestrare dipendenze astratte e testare con fake. Per persistenza: configurazione EF in Infrastructure, repository concreto, test di integrazione. Per HTTP: DTO request/response, mapping, authorization, status code e test WebApplicationFactory.

Promozione servizio: richiede due use case o una responsabilità infrastrutturale stabile; interfaccia piccola; ownership di layer esplicita; lifetime DI motivata; test unitario e di registrazione; esempio; catalogo con contract/implementation/lifetime/status.

## Dipendenze

.NET 10 e xUnit. Solo Infrastructure/Applications possono dipendere da EF Core, PostgreSQL, Entra, OpenTelemetry/Application Insights e framework web secondo i rispettivi ruoli.

## Vincoli

Nullable abilitato, warning puliti, cancellazione propagata per I/O asincrono, configurazione critica validata all'avvio, CORS a allowlist fuori dallo sviluppo. Migration controllabili e protette dalla concorrenza. Log strutturati senza token, connection string, contenuto familiare o PII. Placeholder esterni, mai credenziali versionate.

## Test richiesti

xUnit per ogni invariante e branch di validazione. Test Business senza database. Test integrazione per DI, middleware/Problem Details, auth policy, health/version/status e repository. Ogni bug fix deve avere un test che fallisce prima della correzione quando praticabile.

## Checklist di aggiornamento

1. Identificare invariante, use case, adapter o trasporto.
2. Controllare la direzione delle reference di progetto.
3. Definire errore, validazione, cancellation e osservabilità.
4. Implementare e testare al livello più basso possibile.
5. Verificare DI/lifetime e contratto HTTP se coinvolti.
6. Aggiornare catalogo solo per elementi realmente riusabili.
7. Aggiornare skill/docs/fragment, registry e build/test solution.

## Changelog

Fare riferimento ai change fragment e alle patch note della versione; incrementare la versione della skill quando cambiano contratti o procedure qui descritte.

