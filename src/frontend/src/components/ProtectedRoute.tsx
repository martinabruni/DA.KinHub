import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { authConfig, getActiveAccount, loginForApiAccess } from "../lib/auth";
import { PageScaffold } from "./PageScaffold";

export function ProtectedRoute({ children, routeId }: { children: ReactNode; routeId: string }) {
  const { instance, inProgress } = useMsal();
  const { t } = useTranslation("common");
  if (!authConfig.configured) {
    return <PageScaffold routeId={routeId}><div className="state-card">{t("auth.notConfigured")}</div></PageScaffold>;
  }

  if (getActiveAccount(instance)) {
    return children;
  }

  if (inProgress !== InteractionStatus.None) {
    return <PageScaffold routeId={routeId}><div className="state-card" role="status" aria-live="polite">{t("states.loading")}</div></PageScaffold>;
  }

  return (
    <PageScaffold routeId={routeId}>
      <div className="state-card stack">
        <p>{t("auth.required")}</p>
        <button type="button" className="button" onClick={() => { void loginForApiAccess(instance); }}>
          {t("actions.login")}
        </button>
      </div>
    </PageScaffold>
  );
}
