import { GitBranch, ListChecks } from "lucide-react";
import { useTranslation } from "react-i18next";
import { FeatureCard, KinServiceGrid } from "../components/KinPatterns";
import { PageScaffold } from "../components/PageScaffold";

export function HomePage() {
  const { t } = useTranslation(["pages", "common"]);
  return (
    <PageScaffold routeId="home">
      <p className="lead">{t("home.subtitle", { ns: "pages" })}</p>
      <KinServiceGrid>
        <FeatureCard to="/kinlist" icon={ListChecks} title={t("nav.kinlist", { ns: "common" })} description={t("home.kinlistCard", { ns: "pages" })} />
        <div data-tour="lifecycle"><FeatureCard to="/release-notes" icon={GitBranch} title={t("nav.releaseNotes", { ns: "common" })} description={t("home.lifecycleCard", { ns: "pages" })} tone="info" /></div>
      </KinServiceGrid>
    </PageScaffold>
  );
}
