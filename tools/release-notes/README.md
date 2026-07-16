# Release notes tool

Convalida i fragment in `changes/`, genera patch note bilingui e `src/frontend/public/release-notes.json`. Il comando `release` aggiorna anche `CHANGELOG.md` senza cancellare i fragment, così la rimozione può avvenire in una PR dedicata.

```bash
npm run release:validate
npm run release:generate
npm run release:prepare
```
