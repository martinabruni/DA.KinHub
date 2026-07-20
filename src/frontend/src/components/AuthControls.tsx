import { useMsal } from "@azure/msal-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { authConfig, getActiveAccount, loginForApiAccess, logoutCurrentAccount } from "../lib/auth";
import { useConnectivity } from "./ConnectivityProvider";

export function AuthControls() {
  const { instance } = useMsal();
  const { t } = useTranslation("common");
  const { online } = useConnectivity();
  const [error, setError] = useState(false);
  if (!authConfig.configured) return <span className="auth-note">{t("auth.notConfigured")}</span>;
  const account = getActiveAccount(instance);

  const login = async () => {
    setError(false);
    try { await loginForApiAccess(instance); }
    catch { setError(true); }
  };
  const logout = async () => { await logoutCurrentAccount(instance); };
  return (
    <div className="auth-controls">
      {error && <span role="alert">{t("auth.error")}</span>}
      <button type="button" className="button secondary" disabled={!online && !account} onClick={() => { void (account ? logout() : login()); }}>
        {t(account ? "actions.logout" : "actions.login")}
      </button>
    </div>
  );
}
