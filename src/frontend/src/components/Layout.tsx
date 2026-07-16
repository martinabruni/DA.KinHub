import { BookOpen, Home, Info, ListChecks, Settings } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AuthControls } from "./AuthControls";
import { LanguageSelector } from "./LanguageSelector";
import { Onboarding } from "./Onboarding";
import { ThemeSelector } from "./ThemeSelector";
import { VersionNotification } from "./VersionNotification";

export function Layout() {
  const { t } = useTranslation("common");
  const links = [
    { to: "/", key: "home", icon: Home, end: true },
    { to: "/projects", key: "projects", icon: ListChecks },
    { to: "/release-notes", key: "releaseNotes", icon: BookOpen },
    { to: "/about", key: "about", icon: Info },
    { to: "/settings", key: "settings", icon: Settings }
  ];
  return (
    <div className="app-shell">
      <header className="app-header">
        <NavLink className="brand" to="/" aria-label={t("appName")}>K</NavLink>
        <nav aria-label={t("appName")} data-tour="navigation">
          {links.map(({ to, key, icon: Icon, end }) => <NavLink key={to} to={to} end={end} className={({ isActive }) => isActive ? "active" : ""}><Icon size={18} aria-hidden="true" /><span>{t(`nav.${key}`)}</span></NavLink>)}
        </nav>
        <div className="header-controls"><LanguageSelector /><ThemeSelector /><AuthControls /></div>
      </header>
      <main id="main-content"><Outlet /></main>
      <footer>{t("footer", { version: __APP_VERSION__, environment: __BUILD_ENVIRONMENT__ })}</footer>
      <VersionNotification />
      <Onboarding />
    </div>
  );
}
