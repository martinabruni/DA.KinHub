import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";
import { ButtonLink, Card } from "../components/ui/core";
import { formatDate } from "../i18n";

export function AboutPage() {
  const { t, i18n } = useTranslation(["pages", "common"]);
  return (
    <PageScaffold routeId="about">
      <Card data-tour="version">
        <dl className="metadata">
          <div><dt>{t("about.current", { ns: "pages" })}</dt><dd>{__APP_VERSION__}</dd></div>
          <div><dt>{t("about.commit", { ns: "pages" })}</dt><dd><code>{__COMMIT_SHA__}</code></dd></div>
          <div><dt>{t("about.buildDate", { ns: "pages" })}</dt><dd>{formatDate(__BUILD_DATE__, i18n.language)}</dd></div>
          <div><dt>{t("about.environment", { ns: "pages" })}</dt><dd>{__BUILD_ENVIRONMENT__}</dd></div>
        </dl>
      </Card>
      <div className="page-actions"><ButtonLink variant="secondary" to="/release-notes">{t("about.releaseLink", { ns: "pages" })}</ButtonLink></div>
    </PageScaffold>
  );
}
