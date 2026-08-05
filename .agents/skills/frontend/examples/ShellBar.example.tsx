import { FloatingBarCarousel, FloatingBarPage, GlobalNavigationBar } from "../../../src/frontend/src/components/FloatingBars";

export function ShellBarExample() {
  return (
    <FloatingBarCarousel routeKey="/" label="KinHub" pageLabel={(current, total) => `Bar ${current} of ${total}`}>
      <FloatingBarPage label="Global navigation">
        <GlobalNavigationBar
          labels={{
            navigation: "KinHub",
            home: "Home",
            information: "Information",
            releaseNotes: "Release notes",
            version: "Version",
            language: "Language",
            languageOptions: [{ value: "it", label: "Italiano" }, { value: "en", label: "English" }],
            theme: "Theme",
            settings: "Settings",
            login: "Sign in",
            logout: "Sign out",
            account: "Account"
          }}
          paths={{ home: "/", releaseNotes: "/release-notes", about: "/about", settings: "/settings" }}
          theme="light"
          authenticated={false}
          currentLanguage="it"
          onLanguageChange={() => undefined}
          onThemeToggle={() => undefined}
          onLogin={() => undefined}
          onLogout={() => undefined}
        />
      </FloatingBarPage>
    </FloatingBarCarousel>
  );
}
