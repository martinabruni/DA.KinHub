---
id: architecture
name: architecture
area: architecture
description: Confini dei layer, dipendenze e decisioni strutturali
version: 1.0.0
---

# Architettura KinHub

## Scopo

Fornire la mappa decisionale del repository: responsabilità, direzione delle dipendenze, fonti autorevoli e criteri per introdurre nuova struttura senza sovraingegnerizzare.

## Quando usare

Per nuove dipendenze, integrazioni esterne, spostamenti tra layer, nuovi tool, decisioni di persistenza/deploy o cambi che richiedono AGENTS.md/ADR.

## Quando non usare

Non usarla per giustificare astrazioni speculative o dettagli confinati a una feature. Le skill frontend/backend sono più adatte ai pattern locali.

## Componenti e servizi disponibili

Il [catalogo architetturale](catalog.json) registra fonti autorevoli e confini verificati. Stato reale: solution `.slnx`, quattro progetti/layer previsti ma Infrastructure privo di implementazione; frontend monolitico in `main.tsx`; tool Node senza workspace npm storico; Bicep e workflow presenti come bootstrap. Non confondere struttura presente con requisito completato.

## API e interfacce

Backend: Domain → nessuno; Business → Domain; Infrastructure → Domain/Business solo per implementare porte; Applications → Business/Infrastructure per composition root. Frontend: feature possiede pagina e traduzioni; shared ospita primitive dimostrate riusabili; route registry collega navigazione e documentazione. Tooling legge file dichiarativi e non esegue contenuto delle skill.

Fonti singole: `VERSION` per SemVer; docs Markdown per guida utente; file i18n per testo UI; route registry per route/help; `skills/registry.json` generato per inventario; environment/Key Vault/GitHub Secrets per segreti.

## Esempi

Una repository interface necessaria a un use case appartiene al layer interno consumatore; EF la implementa in Infrastructure. Un componente usato solo da Projects resta nella feature; dopo riuso reale può passare a shared seguendo la skill frontend. Un nuovo generatore documentale appartiene a `tools/`, produce output deterministico e viene validato in CI.

## Dipendenze

.NET 10, React/TypeScript, PostgreSQL/EF Core, Entra ID, Azure/Bicep, GitHub Actions e Node 22 per tool statici.

## Vincoli

Niente dipendenze framework in Domain; niente segreti; niente codice runtime derivato dalle skill; output generati deterministici e verificabili. Una nuova astrazione deve ridurre duplicazione o proteggere un confine reale. Le modifiche strutturali aggiornano AGENTS.md e, se la motivazione non è ovvia, una decisione in `docs/architecture`.

## Test richiesti

Build solution/frontend, test reference boundaries o almeno ispezione dei csproj, test DI, validazione tool/registry/docs e `az bicep build` quando pertinente. Verificare anche i path/comandi su Linux perché la CI gira su Ubuntu.

## Checklist di aggiornamento

1. Scrivere problema e forza che richiede il cambiamento.
2. Elencare layer coinvolti e direzione delle dipendenze.
3. Preferire la soluzione minima reversibile.
4. Aggiornare codice, test e composition root.
5. Aggiornare decisione/diagramma e AGENTS.md se cambia una regola.
6. Aggiornare catalogo, skill, fragment e registry.
7. Eseguire verifiche applicative, tooling e infrastruttura pertinenti.

## Changelog

Le decisioni cambiate sono collegate ai change fragment e alle patch note; la versione della skill cambia quando mutano confini o fonti autorevoli.

