import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { acquireApiAccessToken, getActiveAccount } from "../lib/auth";
import { ApiError, ApiNetworkError, ApiResponseError, KinHubApiClient } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";
import { useKinListFamily } from "./KinListFamilyContext";

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
  const { setFamilyId } = useKinListFamily();
  const [reloadToken, setReloadToken] = useState(0);
  const [state, setState] = useState<GateState>({ status: online ? "loading" : "offline" });
  const account = getActiveAccount(instance);
  const accountKey = account?.homeAccountId ?? "";
  const client = useMemo(
    () => new KinHubApiClient(() => acquireApiAccessToken(instance, account)),
    [account, instance]
  );

  useEffect(() => {
    if (!account) {
      setFamilyId(null);
      setState({ status: "sessionExpired" });
      return;
    }

    if (!online) {
      setFamilyId(null);
      setState({ status: "offline" });
      return;
    }

    const controller = new AbortController();
    setFamilyId(null);
    setState({ status: "loading" });

    void client.getKinListBootstrap(controller.signal)
      .then((result) => {
        setFamilyId(result.state === "family" ? result.familyId : null);
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
            setFamilyId(null);
            setState({ status: "sessionExpired" });
            return;
          }

          if (error.problem.status === 403) {
            setFamilyId(null);
            setState({ status: "forbidden" });
            return;
          }
        }

        if (error instanceof ApiNetworkError) {
          setFamilyId(null);
          setState({ status: "offline" });
          return;
        }

        if (error instanceof ApiError) {
          setFamilyId(null);
          setState({ status: "error" });
          return;
        }

        setFamilyId(null);
        setState({ status: "error" });
      });

    return () => controller.abort();
  }, [account, accountKey, client, online, reloadToken, setFamilyId]);

  if (state.status === "loading") {
    return <div className="state-card" data-kinlist-state="loading" role="status" aria-live="polite" aria-busy="true">{t("kinlist.loading", { ns: "pages" })}</div>;
  }

  if (state.status === "offline") {
    return <div className="state-card" data-kinlist-state="offline" role="status">{t("kinlist.offline", { ns: "pages" })}</div>;
  }

  if (state.status === "sessionExpired") {
    return <div className="state-card" data-kinlist-state="sessionExpired" role="alert">{t("kinlist.sessionExpired", { ns: "pages" })}</div>;
  }

  if (state.status === "forbidden") {
    return <div className="state-card" data-kinlist-state="forbidden" role="alert">{t("kinlist.forbidden", { ns: "pages" })}</div>;
  }

  if (state.status === "error") {
    return (
      <div className="state-card stack" data-kinlist-state="error" role="alert">
        <p>{t("kinlist.error", { ns: "pages" })}</p>
        <button type="button" className="button secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</button>
      </div>
    );
  }

  if (state.status === "onboarding") {
    return (
      <div className="state-card stack" data-kinlist-state="onboarding" role="status">
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
    <div className="state-card stack" data-kinlist-state="ready" data-family-id={state.familyId}>
      <p>{t("kinlist.ready", { ns: "pages" })}</p>
      <p className="muted-text">{t("kinlist.readyDetail", { ns: "pages" })}</p>
    </div>
  );
}
