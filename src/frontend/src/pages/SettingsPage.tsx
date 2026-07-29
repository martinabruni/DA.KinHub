import { useTranslation } from "react-i18next";
import { LanguageSelector } from "../components/LanguageSelector";
import { restartTutorial } from "../components/Onboarding";
import { PageScaffold } from "../components/PageScaffold";
import { ThemeSelector } from "../components/ThemeSelector";
import { Button, Card } from "../components/ui/core";

export function SettingsPage() {
  const { t } = useTranslation(["pages", "common"]);
  return (
    <PageScaffold routeId="settings">
      <div className="kh-service-grid">
        <Card className="kh-settings-card"><h2>{t("settings.appearance", { ns: "pages" })}</h2><LanguageSelector /><ThemeSelector /></Card>
        <Card className="kh-settings-card"><h2>{t("settings.tutorial", { ns: "pages" })}</h2><p>{t("settings.tutorialDescription", { ns: "pages" })}</p><Button variant="secondary" type="button" onClick={restartTutorial}>{t("actions.restartTutorial", { ns: "common" })}</Button></Card>
        <Card className="kh-settings-card"><h2>{t("settings.pwa", { ns: "pages" })}</h2><p>{t("settings.pwaDescription", { ns: "pages" })}</p></Card>
      </div>
    </PageScaffold>
  );
}
