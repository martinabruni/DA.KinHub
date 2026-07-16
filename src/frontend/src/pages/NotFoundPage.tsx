import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";

export function NotFoundPage() {
  const { t } = useTranslation("pages");
  return <PageScaffold routeId="notFound"><p>{t("notFound.description")}</p><Link className="button" to="/">{t("notFound.home")}</Link></PageScaffold>;
}
