import { useTranslation } from "react-i18next";
import { LanguageSelector } from "../components/LanguageSelector";
import { restartTutorial } from "../components/Onboarding";
import { PageScaffold } from "../components/PageScaffold";
import { ThemeSelector } from "../components/ThemeSelector";

export function SettingsPage() {
  const { t } = useTranslation(["pages", "common"]);
  return (
    <PageScaffold routeId="settings">
      <div className="settings-grid">
        <section className="settings-card"><h2>{t("settings.appearance", { ns: "pages" })}</h2><LanguageSelector /><ThemeSelector /></section>
        <section className="settings-card"><h2>{t("settings.tutorial", { ns: "pages" })}</h2><p>{t("settings.tutorialDescription", { ns: "pages" })}</p><button className="button secondary" type="button" onClick={restartTutorial}>{t("actions.restartTutorial", { ns: "common" })}</button></section>
        <section className="settings-card"><h2>{t("settings.pwa", { ns: "pages" })}</h2><p>{t("settings.pwaDescription", { ns: "pages" })}</p></section>
      </div>
    </PageScaffold>
  );
}
