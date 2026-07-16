import { Route, Routes } from "react-router-dom";
import { AppErrorBoundary } from "./components/ErrorBoundary";
import { Layout } from "./components/Layout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { AboutPage } from "./pages/AboutPage";
import { DocsPage } from "./pages/DocsPage";
import { HomePage } from "./pages/HomePage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { ReleaseNotesPage } from "./pages/ReleaseNotesPage";
import { SettingsPage } from "./pages/SettingsPage";

export function App() {
  return (
    <AppErrorBoundary>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<HomePage />} />
          <Route path="projects" element={<ProtectedRoute><ProjectsPage /></ProtectedRoute>} />
          <Route path="about" element={<AboutPage />} />
          <Route path="release-notes" element={<ReleaseNotesPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="docs/:slug" element={<DocsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </AppErrorBoundary>
  );
}
