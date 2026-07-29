import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";
import { Badge, Card } from "../components/ui/core";
import { StatePanel } from "../components/ui/feedback";
import { formatDate } from "../i18n";

interface Entry { id: string; type: string; area: string; breaking: boolean; descriptions: Record<string, string>; }
interface ReleaseDocument { version: string; buildDate: string; entries: Entry[]; }

export function ReleaseNotesPage() {
  const { t, i18n } = useTranslation(["pages", "common"]);
  const [document, setDocument] = useState<ReleaseDocument | null>(null);
  const [error, setError] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    void fetch("/release-notes.json", { signal: controller.signal }).then(async (response) => {
      if (!response.ok) throw new Error(String(response.status));
      setDocument(await response.json() as ReleaseDocument);
    }).catch((reason: unknown) => { if (!(reason instanceof DOMException && reason.name === "AbortError")) setError(true); });
    return () => controller.abort();
  }, []);
  return (
    <PageScaffold routeId="releaseNotes">
      {error && <StatePanel title={t("releaseNotes.title", { ns: "pages" })} description={t("releaseNotes.loadError", { ns: "pages" })} tone="danger" role="alert" live="assertive" />}
      {!error && !document && <StatePanel title={t("states.loading", { ns: "common" })} description={t("releaseNotes.title", { ns: "pages" })} role="status" live="polite" busy />}
      {document && <>
        <div className="release-heading"><h2>{t("releaseNotes.current", { ns: "pages", version: document.version })}</h2><span>{t("releaseNotes.date", { ns: "pages", date: formatDate(document.buildDate, i18n.language) })}</span></div>
        {document.entries.length === 0 ? <StatePanel title={t("releaseNotes.title", { ns: "pages" })} description={t("releaseNotes.empty", { ns: "pages" })} tone="neutral" role="status" live="polite" /> : <ul className="release-list">{document.entries.map((entry) => <li key={entry.id}><Card><div className="release-card-meta"><Badge tone="info">{entry.type}</Badge>{entry.breaking ? <Badge tone="danger">{t("releaseNotes.breaking", { ns: "pages" })}</Badge> : null}</div><strong>{entry.area}</strong><p>{entry.descriptions[i18n.language] ?? entry.descriptions.en}</p></Card></li>)}</ul>}
      </>}
    </PageScaffold>
  );
}
