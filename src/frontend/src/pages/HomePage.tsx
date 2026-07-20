import { ArrowRight, GitBranch, ListChecks } from "lucide-react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";

export function HomePage() {
  const { t } = useTranslation(["pages", "common"]);
  return (
    <PageScaffold routeId="home">
      <p className="lead">{t("home.subtitle", { ns: "pages" })}</p>
      <div className="card-grid">
        <Link className="feature-card" to="/kinlist"><ListChecks aria-hidden="true" /><p>{t("home.kinlistCard", { ns: "pages" })}</p><ArrowRight aria-hidden="true" /></Link>
        <Link className="feature-card" to="/release-notes" data-tour="lifecycle"><GitBranch aria-hidden="true" /><p>{t("home.lifecycleCard", { ns: "pages" })}</p><ArrowRight aria-hidden="true" /></Link>
      </div>
    </PageScaffold>
  );
}
