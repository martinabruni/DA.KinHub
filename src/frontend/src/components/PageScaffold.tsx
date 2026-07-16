import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { PageHelpAccordion, routeDefinition } from "./PageHelpAccordion";

export function PageScaffold({ routeId, children }: { routeId: string; children: ReactNode }) {
  const { t } = useTranslation("pages");
  const route = routeDefinition(routeId);
  return (
    <section className="page" data-page={routeId}>
      <h1>{t(route.titleKey)}</h1>
      <PageHelpAccordion routeId={routeId} />
      <div className="page-content">{children}</div>
    </section>
  );
}
