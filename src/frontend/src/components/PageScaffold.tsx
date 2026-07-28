import { useEffect, useId, useRef, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { PageHelpAccordion, routeDefinition } from "./PageHelpAccordion";

export function PageScaffold({ routeId, children, titleRef }: { routeId: string; children: ReactNode; titleRef?: React.RefObject<HTMLHeadingElement | null> }) {
  const { t } = useTranslation("pages");
  const route = routeDefinition(routeId);
  const titleId = useId();
  const fallbackTitleRef = useRef<HTMLHeadingElement>(null);
  const resolvedTitleRef = titleRef ?? fallbackTitleRef;

  useEffect(() => {
    resolvedTitleRef.current?.focus();
  }, [resolvedTitleRef, routeId]);

  return (
    <section className="page" data-page={routeId} aria-labelledby={titleId}>
      <h1 id={titleId} ref={resolvedTitleRef} tabIndex={-1}>{t(route.titleKey)}</h1>
      <PageHelpAccordion routeId={routeId} />
      <div className="page-content">{children}</div>
    </section>
  );
}
