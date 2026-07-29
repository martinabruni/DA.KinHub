import { useTranslation } from "react-i18next";
import ReactMarkdown from "react-markdown";
import { useParams } from "react-router-dom";
import { PageScaffold } from "../components/PageScaffold";
import { StatePanel } from "../components/ui/feedback";
import docs from "../generated/docs/index.json";

interface GuidePage { slug: string; locale: string; title: string; description: string; content: string; source: string; }

export function DocsPage() {
  const { slug } = useParams();
  const { t, i18n } = useTranslation("pages");
  const locale = i18n.language === "it" ? "it" : "en";
  const page = (docs.pages[locale] as GuidePage[]).find((entry) => entry.slug === slug);
  return (
    <PageScaffold routeId="docs">
      {page ? <article className="markdown"><h2>{page.title}</h2><p className="lead">{page.description}</p><ReactMarkdown>{page.content}</ReactMarkdown></article> : <StatePanel title={t("docs.title")} description={t("docs.missing")} tone="warning" role="status" live="polite" />}
    </PageScaffold>
  );
}
