import { useEffect, useId, useRef, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { PageHelpAccordion, routeDefinition } from "./PageHelpAccordion";

export function PageScaffold({ routeId, children }: { routeId: string; children: ReactNode }) {
  const { t } = useTranslation("pages");
  const route = routeDefinition(routeId);
  const titleId = useId();
  const titleRef = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    titleRef.current?.focus();
  }, [routeId]);

  return (
    <section className="page" data-page={routeId} aria-labelledby={titleId}>
      <h1 id={titleId} ref={titleRef} tabIndex={-1}>{t(route.titleKey)}</h1>
      <PageHelpAccordion routeId={routeId} />
      <div className="page-content">{children}</div>
    </section>
  );
}
