import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { acquireApiAccessToken, getActiveAccount } from "../lib/auth";
import { ApiError, ApiNetworkError, ApiResponseError, KinHubApiClient } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";

type GateState =
  | { status: "loading" }
  | { status: "offline" }
  | { status: "onboarding" }
  | { status: "ready"; familyId: string }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "error" };

export function KinListAccessGate() {
  const { t } = useTranslation(["pages", "common"]);
  const { instance } = useMsal();
  const { online } = useConnectivity();
  const [reloadToken, setReloadToken] = useState(0);
  const [state, setState] = useState<GateState>({ status: online ? "loading" : "offline" });
  const client = useMemo(
    () => new KinHubApiClient(() => acquireApiAccessToken(instance, getActiveAccount(instance))),
    [instance]
  );

  useEffect(() => {
    const account = getActiveAccount(instance);
    if (!account) {
      setState({ status: "sessionExpired" });
      return;
    }

    if (!online) {
      setState({ status: "offline" });
      return;
    }

    const controller = new AbortController();
    setState({ status: "loading" });

    void client.getKinListBootstrap(controller.signal)
      .then((result) => {
        setState(result.state === "family"
          ? { status: "ready", familyId: result.familyId }
          : { status: "onboarding" });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiResponseError) {
          if (error.problem.status === 401) {
            setState({ status: "sessionExpired" });
            return;
          }

          if (error.problem.status === 403) {
            setState({ status: "forbidden" });
            return;
          }
        }

        if (error instanceof ApiNetworkError) {
          setState({ status: "offline" });
          return;
        }

        if (error instanceof ApiError) {
          setState({ status: "error" });
          return;
        }

        setState({ status: "error" });
      });

    return () => controller.abort();
  }, [client, instance, online, reloadToken]);

  if (state.status === "loading") {
    return <div className="state-card" role="status" aria-live="polite" aria-busy="true">{t("kinlist.loading", { ns: "pages" })}</div>;
  }

  if (state.status === "offline") {
    return <div className="state-card" role="status">{t("kinlist.offline", { ns: "pages" })}</div>;
  }

  if (state.status === "sessionExpired") {
    return <div className="state-card" role="alert">{t("kinlist.sessionExpired", { ns: "pages" })}</div>;
  }

  if (state.status === "forbidden") {
    return <div className="state-card" role="alert">{t("kinlist.forbidden", { ns: "pages" })}</div>;
  }

  if (state.status === "error") {
    return (
      <div className="state-card stack" role="alert">
        <p>{t("kinlist.error", { ns: "pages" })}</p>
        <button type="button" className="button secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</button>
      </div>
    );
  }

  if (state.status === "onboarding") {
    return (
      <div className="state-card stack" role="status">
        <p>{t("kinlist.onboarding", { ns: "pages" })}</p>
        <div className="actions">
          <button type="button" className="button" disabled>{t("actions.createFamily", { ns: "common" })}</button>
          <button type="button" className="button secondary" disabled>{t("actions.joinFamily", { ns: "common" })}</button>
        </div>
        <p className="muted-text">{t("kinlist.onboardingPending", { ns: "pages" })}</p>
      </div>
    );
  }

  return (
    <div className="state-card stack">
      <p>{t("kinlist.ready", { ns: "pages" })}</p>
      <p className="muted-text">{t("kinlist.readyDetail", { ns: "pages" })}</p>
    </div>
  );
}
