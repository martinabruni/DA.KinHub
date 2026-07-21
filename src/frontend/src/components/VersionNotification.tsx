import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useRegisterSW } from "virtual:pwa-register/react";

interface ReleaseMetadata { version: string; }

export function VersionNotification() {
  const { t } = useTranslation("common");
  const [availableVersion, setAvailableVersion] = useState<string | null>(null);
  const [dismissed, setDismissed] = useState(false);
  const { needRefresh: [needRefresh], updateServiceWorker } = useRegisterSW();

  useEffect(() => {
    const check = async () => {
      if (!navigator.onLine) return;
      try {
        const response = await fetch(`/release-notes.json?now=${Date.now()}`, { cache: "no-store" });
        if (!response.ok) return;
        const metadata = await response.json() as ReleaseMetadata;
        if (metadata.version !== __APP_VERSION__) setAvailableVersion(metadata.version);
      } catch { /* Offline mode keeps the current version. */ }
    };
    void check();
    const interval = window.setInterval(() => { void check(); }, 15 * 60 * 1000);
    const onFocus = () => { void check(); };
    window.addEventListener("focus", onFocus);
    return () => { window.clearInterval(interval); window.removeEventListener("focus", onFocus); };
  }, []);

  if (dismissed || (!availableVersion && !needRefresh)) return null;
  const refresh = async () => {
    if (sessionStorage.getItem("kinhub.refreshing") === "1") return;
    sessionStorage.setItem("kinhub.refreshing", "1");
    if (needRefresh) await updateServiceWorker(true);
    else window.location.reload();
  };
  return (
    <div className="update-notice" role="status">
      <span>{availableVersion ? t("versionUpdate.available", { version: availableVersion }) : t("versionUpdate.pwaAvailable")}</span>
      <button type="button" className="button" onClick={() => { void refresh(); }}>{t("actions.refresh")}</button>
      <button type="button" className="button ghost" onClick={() => setDismissed(true)}>{t("versionUpdate.dismiss")}</button>
    </div>
  );
}
