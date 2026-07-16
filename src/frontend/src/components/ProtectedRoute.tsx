import { useMsal } from "@azure/msal-react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { authConfig } from "../lib/auth";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { accounts, instance } = useMsal();
  const { t } = useTranslation("common");
  if (!authConfig.configured || accounts.length > 0) return children;
  return (
    <div className="state-card">
      <p>{t("auth.required")}</p>
      <button type="button" className="button" onClick={() => { void instance.loginPopup({ scopes: [authConfig.apiScope], prompt: "select_account" }); }}>
        {t("actions.login")}
      </button>
    </div>
  );
}
