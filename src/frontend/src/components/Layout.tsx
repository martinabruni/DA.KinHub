import { useMsal } from "@azure/msal-react";
import { Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { authConfig, getActiveAccount, loginForApiAccess, logoutCurrentAccount } from "../lib/auth";
import { FloatingBarCarousel, FloatingBarPage, GlobalNavigationBar } from "./FloatingBars";
import { Onboarding } from "./Onboarding";
import { routeDefinition } from "./PageHelpAccordion";
import { ShellBarProvider, useShellBar } from "./ShellBarContext";
import { useTheme } from "./ThemeProvider";
import { VersionNotification } from "./VersionNotification";
import { AccountProfileProvider, useAccountProfileName } from "./AccountProfileContext";

function LayoutContent() {
  const { t } = useTranslation("common");
  const { instance } = useMsal();
  const { i18n } = useTranslation();
  const { theme, setTheme } = useTheme();
  const location = useLocation();
  const { contextualBar } = useShellBar();
  const account = getActiveAccount(instance);
  const accountName = useAccountProfileName();
  const isDark = theme === "dark" || (theme === "system" && document.documentElement.classList.contains("dark"));
  const globalPaths = {
    home: routeDefinition("home").path,
    releaseNotes: routeDefinition("releaseNotes").path,
    about: routeDefinition("about").path,
    settings: routeDefinition("settings").path,
    userGuide: routeDefinition("docs").path.replace(":slug", "getting-started")
  };

  const handleThemeToggle = () => setTheme(isDark ? "light" : "dark");
  const handleLogin = async () => {
    if (!authConfig.configured) {
      return;
    }

    await loginForApiAccess(instance);
  };

  const handleLogout = async () => {
    if (!authConfig.configured) {
      return;
    }

    await logoutCurrentAccount(instance);
  };

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">{t("actions.skipToContent")}</a>
      <main id="main-content" tabIndex={-1}><Outlet /></main>
      <div className="app-floating-bars">
        <FloatingBarCarousel defaultIndex={0} routeKey={location.pathname} label={t("appName")} pageLabel={(current, total) => t("navigation.pageLabel", { current, total })}>
          <FloatingBarPage label={t("navigation.globalBar")}>
            <GlobalNavigationBar
              labels={{
                navigation: t("appName"),
                home: t("nav.home"),
                information: t("nav.information"),
                releaseNotes: t("nav.releaseNotes"),
                version: t("nav.about"),
                userGuide: t("nav.userGuide"),
                language: t("language.label"),
                languageOptions: [
                  { value: "it", label: t("language.it") },
                  { value: "en", label: t("language.en") }
                ],
                theme: t("theme.label"),
                settings: t("nav.settings"),
                login: t("actions.login"),
                logout: t("actions.logout"),
                account: t("auth.account")
              }}
              paths={globalPaths}
              theme={isDark ? "dark" : "light"}
              authenticated={Boolean(account)}
              accountName={accountName}
              currentLanguage={i18n.language === "it" ? "it" : "en"}
              onLanguageChange={(language) => { void i18n.changeLanguage(language); }}
              onThemeToggle={() => handleThemeToggle()}
              onLogin={() => { void handleLogin(); }}
              onLogout={() => { void handleLogout(); }}
            />
          </FloatingBarPage>
          {contextualBar ? <FloatingBarPage label={t("navigation.contextualBar")}>{contextualBar}</FloatingBarPage> : null}
        </FloatingBarCarousel>
      </div>
      <footer>{t("footer", { version: __APP_VERSION__, environment: __BUILD_ENVIRONMENT__ })}</footer>
      <VersionNotification />
      <Onboarding />
    </div>
  );
}

export function Layout() {
  const { instance } = useMsal();
  const account = getActiveAccount(instance);
  return <AccountProfileProvider accountName={account?.name}><ShellBarProvider><LayoutContent /></ShellBarProvider></AccountProfileProvider>;
}
