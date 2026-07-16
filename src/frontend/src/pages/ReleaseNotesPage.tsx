import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";
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
      {error && <div className="state-card error-message" role="alert">{t("releaseNotes.loadError", { ns: "pages" })}</div>}
      {!error && !document && <div className="state-card">{t("states.loading", { ns: "common" })}</div>}
      {document && <>
        <div className="release-heading"><h2>{t("releaseNotes.current", { ns: "pages", version: document.version })}</h2><span>{t("releaseNotes.date", { ns: "pages", date: formatDate(document.buildDate, i18n.language) })}</span></div>
        {document.entries.length === 0 ? <div className="state-card">{t("releaseNotes.empty", { ns: "pages" })}</div> : <ul className="release-list">{document.entries.map((entry) => <li key={entry.id}><span className="badge">{entry.type}</span><strong>{entry.area}</strong>{entry.breaking && <span className="breaking">{t("releaseNotes.breaking", { ns: "pages" })}</span>}<p>{entry.descriptions[i18n.language] ?? entry.descriptions.en}</p></li>)}</ul>}
      </>}
    </PageScaffold>
  );
}
