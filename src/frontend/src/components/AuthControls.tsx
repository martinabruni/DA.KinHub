import { useMsal } from "@azure/msal-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { authConfig } from "../lib/auth";

export function AuthControls() {
  const { instance, accounts } = useMsal();
  const { t } = useTranslation("common");
  const [error, setError] = useState(false);
  if (!authConfig.configured) return <span className="auth-note">{t("auth.notConfigured")}</span>;

  const login = async () => {
    setError(false);
    try { await instance.loginPopup({ scopes: [authConfig.apiScope], prompt: "select_account" }); }
    catch { setError(true); }
  };
  const logout = async () => { await instance.logoutPopup({ account: accounts[0] }); };
  return (
    <div className="auth-controls">
      {error && <span role="alert">{t("auth.error")}</span>}
      <button type="button" className="button secondary" onClick={() => { void (accounts.length ? logout() : login()); }}>
        {t(accounts.length ? "actions.logout" : "actions.login")}
      </button>
    </div>
  );
}
