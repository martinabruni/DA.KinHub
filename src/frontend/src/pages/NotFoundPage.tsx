import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";
import { ButtonLink } from "../components/ui/core";
import { StatePanel } from "../components/ui/feedback";

export function NotFoundPage() {
  const { t } = useTranslation("pages");
  return <PageScaffold routeId="notFound"><StatePanel title={t("notFound.title")} description={t("notFound.description")} tone="warning" action={<ButtonLink to="/">{t("notFound.home")}</ButtonLink>} /></PageScaffold>;
}
