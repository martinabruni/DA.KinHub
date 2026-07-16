import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import registry from "../routes/route-registry.json";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "./ui/accordion";

export interface RouteDefinition { id: string; path: string; titleKey: string; helpKey: string; guideSlug: string; }

export function routeDefinition(id: string) {
  const route = (registry as RouteDefinition[]).find((entry) => entry.id === id);
  if (!route) throw new Error(`Unknown route id: ${id}`);
  return route;
}

export function PageHelpAccordion({ routeId }: { routeId: string }) {
  const route = routeDefinition(routeId);
  const { t } = useTranslation(["common", "help"]);
  const fields = ["purpose", "actions", "prerequisites", "fields", "limits"] as const;
  return (
    <Accordion type="single" collapsible className="page-help" data-tour="help">
      <AccordionItem value="help">
        <AccordionTrigger>{t(`${route.helpKey}.summary`, { ns: "help" })}</AccordionTrigger>
        <AccordionContent>
          <dl className="help-grid">
            {fields.map((field) => <div key={field}><dt>{t(`help.${field}`, { ns: "common" })}</dt><dd>{t(`${route.helpKey}.${field}`, { ns: "help" })}</dd></div>)}
          </dl>
          <Link className="text-link" to={`/docs/${route.guideSlug}`}>{t("actions.openGuide", { ns: "common" })}</Link>
        </AccordionContent>
      </AccordionItem>
    </Accordion>
  );
}
