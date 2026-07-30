# Evidenze criteri FEAT-014

## AC-078 - Pagine correnti migrate

- Shell condivisa: `src/frontend/src/components/Layout.tsx`
- Route migrate: `src/frontend/src/pages/HomePage.tsx`, `SettingsPage.tsx`, `AboutPage.tsx`, `ReleaseNotesPage.tsx`, `DocsPage.tsx`, `NotFoundPage.tsx`, `src/frontend/src/components/ProtectedRoute.tsx`, `ErrorBoundary.tsx`, `Onboarding.tsx`, `VersionNotification.tsx`, `KinListAccessGate.tsx`
- Evidenza automatica: `npm run test`, `npm run lint`, `npm run typecheck`, `npm run build`

## AC-079 - Navigazione flottante condivisa

- Contratto shell: `src/frontend/src/components/FloatingBars.tsx`
- Registrazione contestuale: `src/frontend/src/components/ShellBarContext.tsx`
- Integrazione shell: `src/frontend/src/components/Layout.tsx`
- Evidenza test: `src/frontend/src/components/FloatingBars.test.tsx`, `src/frontend/src/components/Layout.test.tsx`

## AC-080 - Nessun legacy o duplicazione

- Route demo rimossa: `src/frontend/src/App.tsx`, `src/frontend/src/routes/route-registry.json`
- Demo rimossa: `src/frontend/src/pages/DesignSystemPage.tsx` eliminata
- Guide demo rimosse: `docs/user-guide/{it,en}/design-system.md`, `projects.md` eliminate
- Validator anti-regressione: `src/frontend/scripts/validate-design-system.mjs`
- Evidenza automatica: `npm run design-system:validate`

## AC-081 - Primitive e wrapper coerenti

- Primitive ufficiali: `src/frontend/src/components/ui/core.tsx`, `controls.tsx`, `feedback.tsx`, `accordion.tsx`
- Wrapper sottili: `src/frontend/src/components/KinPatterns.tsx`
- Evidenza test: `src/frontend/src/components/ui/core.test.tsx`, `src/frontend/src/components/KinPatterns.test.tsx`

## AC-082 - Documentazione e harness vincolanti

- Regole repository: `AGENTS.md`
- Skill frontend: `skills/frontend/SKILL.md`, `skills/frontend/catalog.json`, `skills/frontend/examples/ShellBar.example.tsx`
- Guida tecnica: `docs/architecture/frontend-design-system.md`
- Workflow: `.github/workflows/pr-quality.yml`, `deploy-frontend.yml`
- Registro skill rigenerato: `skills/registry.json`

## AC-083 - Stati e temi preservati

- Tema e bootstrap visivo: `src/frontend/index.html`, `src/frontend/src/components/ThemeProvider.tsx`, `src/frontend/vite.config.ts`, `src/frontend/public/icon.svg`, `src/frontend/src/styles.css`
- Stati asincroni e focus: `src/frontend/src/components/KinListAccessGate.tsx`, `ProtectedRoute.tsx`, `ErrorBoundary.tsx`, `Onboarding.tsx`, `VersionNotification.tsx`
- Evidenza test: `src/frontend/src/components/KinListAccessGate.test.tsx`, `Layout.test.tsx`
- Evidenza automatica: `npm run i18n:validate`, `npm run routes:validate`, `npm run build`

## Residuo non automatico

- Le verifiche manuali richieste dal piano sono tracciate in `manual-verification.md`.
