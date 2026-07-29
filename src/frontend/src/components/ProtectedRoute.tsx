import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { authConfig, getActiveAccount, loginForApiAccess } from "../lib/auth";
import { PageScaffold } from "./PageScaffold";
import { StatePanel } from "./ui/feedback";
import { Button } from "./ui/core";

export function ProtectedRoute({ children, routeId }: { children: ReactNode; routeId: string }) {
  const { instance, inProgress } = useMsal();
  const { t } = useTranslation("common");
  if (!authConfig.configured) {
    return <PageScaffold routeId={routeId}><StatePanel title={t("states.error")} description={t("auth.notConfigured")} tone="warning" role="status" live="polite" /></PageScaffold>;
  }

  if (getActiveAccount(instance)) {
    return children;
  }

  if (inProgress !== InteractionStatus.None) {
    return <PageScaffold routeId={routeId}><StatePanel title={t("states.loading")} description={t("auth.required")} role="status" live="polite" busy /></PageScaffold>;
  }

  return (
    <PageScaffold routeId={routeId}>
      <StatePanel title={t("auth.required")} description={t("auth.signInDescription")} tone="info" action={<Button onClick={() => { void loginForApiAccess(instance); }}>{t("actions.login")}</Button>} />
    </PageScaffold>
  );
}
