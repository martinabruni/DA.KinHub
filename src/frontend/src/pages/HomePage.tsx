import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { KinHubOnboardingPanel, useKinHubFamilyBootstrap } from "../components/KinHubFamilyBootstrap";
import { KinServiceCard, KinServiceGrid } from "../components/KinPatterns";
import { PageScaffold } from "../components/PageScaffold";
import { Button } from "../components/ui/core";
import { StatePanel } from "../components/ui/feedback";
import { ApiError, ApiNetworkError, ApiResponseError, type KinHubServiceCatalogItem } from "../lib/api";

type CatalogState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "empty" }
  | { status: "success"; services: KinHubServiceCatalogItem[] }
  | { status: "offline" }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "error" };

export function HomePage() {
  const { t, i18n } = useTranslation(["pages", "common"]);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const bootstrap = useKinHubFamilyBootstrap();
  const [catalogState, setCatalogState] = useState<CatalogState>({ status: "idle" });
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    if (bootstrap.state.status !== "family") {
      setCatalogState(bootstrap.state.status === "offline" ? { status: "offline" } : { status: "idle" });
      return;
    }

    const controller = new AbortController();
    setCatalogState({ status: "loading" });

    void bootstrap.client.getFamilyServices(bootstrap.state.familyId, i18n.resolvedLanguage?.startsWith("en") ? "en" : "it", controller.signal)
      .then((result) => {
        setCatalogState(result.services.length === 0 ? { status: "empty" } : { status: "success", services: result.services });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiResponseError) {
          if (error.problem.status === 401) {
            setCatalogState({ status: "sessionExpired" });
            return;
          }

          if (error.problem.status === 403) {
            setCatalogState({ status: "forbidden" });
            return;
          }
        }

        if (error instanceof ApiNetworkError) {
          setCatalogState({ status: "offline" });
          return;
        }

        if (error instanceof ApiError) {
          setCatalogState({ status: "error" });
          return;
        }

        setCatalogState({ status: "error" });
      });

    return () => controller.abort();
  }, [bootstrap.client, bootstrap.state, i18n.resolvedLanguage, reloadToken]);

  return (
    <PageScaffold routeId="home" titleRef={titleRef}>
      <p className="lead">{t("home.subtitle", { ns: "pages" })}</p>
      {bootstrap.state.status === "initializing" || bootstrap.state.status === "loading" ? <StatePanel title={t("home.title", { ns: "pages" })} description={t("home.loading", { ns: "pages" })} role="status" live="polite" busy /> : null}
      {bootstrap.state.status === "visitor" ? <StatePanel title={t("home.visitorTitle", { ns: "pages" })} description={t("home.visitorDescription", { ns: "pages" })} tone="info" role="status" live="polite" /> : null}
      {bootstrap.state.status === "offline" ? <StatePanel title={t("home.offlineTitle", { ns: "pages" })} description={t("home.offlineDescription", { ns: "pages" })} tone="warning" role="status" live="polite" /> : null}
      {bootstrap.state.status === "sessionExpired" ? <StatePanel title={t("home.sessionExpiredTitle", { ns: "pages" })} description={t("home.sessionExpiredDescription", { ns: "pages" })} tone="warning" role="alert" live="assertive" /> : null}
      {bootstrap.state.status === "forbidden" ? <StatePanel title={t("home.forbiddenTitle", { ns: "pages" })} description={t("home.forbiddenDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" /> : null}
      {bootstrap.state.status === "error" ? <StatePanel title={t("home.errorTitle", { ns: "pages" })} description={t("home.errorDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={bootstrap.retry}>{t("actions.retry", { ns: "common" })}</Button>} /> : null}
      {bootstrap.state.status === "onboarding" ? <KinHubOnboardingPanel onCreate={async (name) => {
        const result = await bootstrap.createFamily(name);
        if (result === "created") {
          requestAnimationFrame(() => titleRef.current?.focus());
        }

        return result;
      }} /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "loading" ? <StatePanel title={t("home.catalogLoadingTitle", { ns: "pages" })} description={t("home.catalogLoadingDescription", { ns: "pages" })} role="status" live="polite" busy /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "empty" ? <StatePanel title={t("home.emptyTitle", { ns: "pages" })} description={t("home.emptyDescription", { ns: "pages" })} tone="info" role="status" live="polite" /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "offline" ? <StatePanel title={t("home.offlineTitle", { ns: "pages" })} description={t("home.offlineDescription", { ns: "pages" })} tone="warning" role="status" live="polite" /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "sessionExpired" ? <StatePanel title={t("home.sessionExpiredTitle", { ns: "pages" })} description={t("home.sessionExpiredDescription", { ns: "pages" })} tone="warning" role="alert" live="assertive" /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "forbidden" ? <StatePanel title={t("home.forbiddenTitle", { ns: "pages" })} description={t("home.forbiddenDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "error" ? <StatePanel title={t("home.errorTitle", { ns: "pages" })} description={t("home.errorDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</Button>} /> : null}
      {bootstrap.state.status === "family" && catalogState.status === "success" ? <KinServiceGrid>{catalogState.services.map((service) => <KinServiceCard key={service.key} to={service.route} title={service.name} description={service.description} />)}</KinServiceGrid> : null}
    </PageScaffold>
  );
}
