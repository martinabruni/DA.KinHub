import { useEffect, useState, type RefObject } from "react";
import { useTranslation } from "react-i18next";
import { ApiError, ApiNetworkError, ApiResponseError } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";
import { useShellBar } from "./ShellBarContext";
import { KinHubOnboardingPanel, useKinHubFamilyBootstrap } from "./KinHubFamilyBootstrap";
import { Button } from "./ui/core";
import { StatePanel } from "./ui/feedback";

type AccessState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; familyId: string }
  | { status: "offline" }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "error" };

export function KinListAccessGate({ titleRef }: { titleRef?: RefObject<HTMLHeadingElement | null> }) {
  const { t } = useTranslation(["pages", "common"]);
  const { online } = useConnectivity();
  const { setContextualBar } = useShellBar();
  const bootstrap = useKinHubFamilyBootstrap();
  const [reloadToken, setReloadToken] = useState(0);
  const [accessState, setAccessState] = useState<AccessState>({ status: online ? "loading" : "offline" });

  useEffect(() => {
    if (bootstrap.state.status === "error" || accessState.status === "error") {
      setContextualBar(
        <div className="kh-floating-bar kh-service-bar" aria-label={t("navigation.contextualBar", { ns: "common" })}>
          <Button variant="secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</Button>
        </div>
      );
      return () => setContextualBar(null);
    }

    setContextualBar(null);
    return undefined;
  }, [accessState.status, bootstrap.state.status, setContextualBar, t]);

  useEffect(() => {
    if (bootstrap.state.status !== "family") {
      setAccessState(bootstrap.state.status === "offline" ? { status: "offline" } : { status: "idle" });
      return;
    }

    const controller = new AbortController();
    const familyId = bootstrap.state.familyId;
    setAccessState({ status: "loading" });

    void bootstrap.client.checkServiceAccess("kinlist", familyId, controller.signal)
      .then(() => {
        setAccessState({ status: "ready", familyId });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiResponseError) {
          if (error.problem.status === 401) {
            setAccessState({ status: "sessionExpired" });
            return;
          }

          if (error.problem.status === 403) {
            setAccessState({ status: "forbidden" });
            return;
          }
        }

        if (error instanceof ApiNetworkError) {
          setAccessState({ status: "offline" });
          return;
        }

        if (error instanceof ApiError) {
          setAccessState({ status: "error" });
          return;
        }

        setAccessState({ status: "error" });
      });

    return () => controller.abort();
  }, [bootstrap.client, bootstrap.state, online, reloadToken]);

  if (bootstrap.state.status === "initializing" || bootstrap.state.status === "loading" || accessState.status === "loading") {
    return <StatePanel data-kinlist-state="loading" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.loading", { ns: "pages" })} role="status" live="polite" busy />;
  }

  if (bootstrap.state.status === "offline" || accessState.status === "offline") {
    return <StatePanel data-kinlist-state="offline" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.offline", { ns: "pages" })} tone="warning" role="status" live="polite" />;
  }

  if (bootstrap.state.status === "sessionExpired" || bootstrap.state.status === "visitor" || accessState.status === "sessionExpired") {
    return <StatePanel data-kinlist-state="sessionExpired" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.sessionExpired", { ns: "pages" })} tone="warning" role="alert" live="assertive" />;
  }

  if (bootstrap.state.status === "forbidden" || accessState.status === "forbidden") {
    return <StatePanel data-kinlist-state="forbidden" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.forbidden", { ns: "pages" })} tone="danger" role="alert" live="assertive" />;
  }

  if (bootstrap.state.status === "error" || accessState.status === "error") {
    return (
      <StatePanel data-kinlist-state="error" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.error", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</Button>} />
    );
  }

  if (bootstrap.state.status === "onboarding") {
    return <KinHubOnboardingPanel onCreate={async (name) => {
      const result = await bootstrap.createFamily(name);
      if (result === "created") {
        requestAnimationFrame(() => titleRef?.current?.focus());
      }

      return result;
    }} />;
  }

  return (
    <StatePanel data-kinlist-state="ready" data-family-id={accessState.status === "ready" ? accessState.familyId : undefined} title={t("kinlist.ready", { ns: "pages" })} description={t("kinlist.readyDetail", { ns: "pages" })} tone="success" />
  );
}
