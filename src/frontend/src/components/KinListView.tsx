import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { KinListItem } from "./KinPatterns";
import { useConnectivity } from "./ConnectivityProvider";
import { useShellBar } from "./ShellBarContext";
import { Button, Pagination } from "./ui/core";
import { Alert, StatePanel } from "./ui/feedback";
import { ApiError, ApiNetworkError, ApiResponseError, type KinListItemsPage, type KinHubApiClient } from "../lib/api";

type ViewState =
  | { status: "initialLoading" }
  | { status: "empty"; page: KinListItemsPage }
  | { status: "ready"; page: KinListItemsPage }
  | { status: "refreshing"; page: KinListItemsPage }
  | { status: "navigating"; page: KinListItemsPage }
  | { status: "cursorInvalid"; page: KinListItemsPage }
  | { status: "error"; page: KinListItemsPage }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "offline" };

const PAGE_SIZE = 50;

export function KinListView({ familyId, client }: { familyId: string; client: KinHubApiClient }) {
  const { t } = useTranslation(["pages", "common"]);
  const { online } = useConnectivity();
  const { setContextualBar } = useShellBar();
  const headingRef = useRef<HTMLHeadingElement>(null);
  const requestId = useRef(0);
  const [state, setState] = useState<ViewState>(online ? { status: "initialLoading" } : { status: "offline" });

  useEffect(() => {
    setContextualBar(
      <div className="kh-floating-bar kh-service-bar" aria-label={t("navigation.contextualBar", { ns: "common" })}>
        <Button variant="secondary" onClick={() => void loadPage(null, "refresh")}>{t("actions.refresh", { ns: "common" })}</Button>
      </div>
    );

    return () => setContextualBar(null);
  }, [setContextualBar, t]);

  useEffect(() => {
    if (!online) {
      setState({ status: "offline" });
      return;
    }

    void loadPage(null, "initial");
  }, [familyId, online]);

  async function loadPage(cursor: string | null, mode: "initial" | "refresh" | "next" | "previous") {
    const currentRequestId = ++requestId.current;
    const controller = new AbortController();

    setState((current) => {
      if (mode === "initial") {
        return { status: "initialLoading" };
      }

      if (current.status === "ready" || current.status === "empty" || current.status === "cursorInvalid" || current.status === "error") {
        return mode === "refresh" ? { status: "refreshing", page: current.page } : { status: "navigating", page: current.page };
      }

      return current;
    });

    try {
      const page = await client.getKinListItems(familyId, PAGE_SIZE, cursor, controller.signal);
      if (currentRequestId !== requestId.current) {
        return;
      }

      setState(page.items.length === 0 ? { status: "empty", page } : { status: "ready", page });
      if (mode === "next" || mode === "previous") {
        requestAnimationFrame(() => headingRef.current?.focus());
      }
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      if (currentRequestId !== requestId.current) {
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

        if (error.problem.status === 400 && error.problem.code === "pagination.cursorInvalid") {
          setState((current) => current.status === "ready" || current.status === "refreshing" || current.status === "navigating" || current.status === "error" || current.status === "cursorInvalid" || current.status === "empty"
            ? { status: "cursorInvalid", page: current.page }
            : { status: "initialLoading" });
          return;
        }
      }

      if (error instanceof ApiNetworkError) {
        setState({ status: "offline" });
        return;
      }

      if (error instanceof ApiError) {
        setState((current) => current.status === "ready" || current.status === "refreshing" || current.status === "navigating" || current.status === "error" || current.status === "cursorInvalid" || current.status === "empty"
          ? { status: "error", page: current.page }
          : { status: "offline" });
        return;
      }

      setState((current) => current.status === "ready" || current.status === "refreshing" || current.status === "navigating" || current.status === "error" || current.status === "cursorInvalid" || current.status === "empty"
        ? { status: "error", page: current.page }
        : { status: "offline" });
    } finally {
      controller.abort();
    }
  }

  if (state.status === "initialLoading") {
    return <StatePanel title={t("kinlist.list.loadingTitle", { ns: "pages" })} description={t("kinlist.list.loadingDescription", { ns: "pages" })} role="status" live="polite" busy headingLevel={2} />;
  }

  if (state.status === "sessionExpired") {
    return <StatePanel title={t("kinlist.list.sessionExpiredTitle", { ns: "pages" })} description={t("kinlist.list.sessionExpiredDescription", { ns: "pages" })} tone="warning" role="alert" live="assertive" headingLevel={2} />;
  }

  if (state.status === "forbidden") {
    return <StatePanel title={t("kinlist.list.forbiddenTitle", { ns: "pages" })} description={t("kinlist.list.forbiddenDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" headingLevel={2} />;
  }

  if (state.status === "offline") {
    return <StatePanel title={t("kinlist.list.offlineTitle", { ns: "pages" })} description={t("kinlist.list.offlineDescription", { ns: "pages" })} tone="warning" role="status" live="polite" headingLevel={2} />;
  }

  const page = state.page;
  const busy = state.status === "refreshing" || state.status === "navigating";
  const authorFallback = t("kinlist.authorFallback", { ns: "pages" });

  return <section className="kh-kinlist" aria-busy={busy || undefined}><h2 ref={headingRef} tabIndex={-1}>{t("kinlist.list.heading", { ns: "pages" })}</h2>{state.status === "cursorInvalid" ? <Alert tone="warning" title={t("kinlist.list.cursorInvalidTitle", { ns: "pages" })}>{t("kinlist.list.cursorInvalidDescription", { ns: "pages" })} <Button variant="ghost" onClick={() => void loadPage(null, "refresh")}>{t("kinlist.list.restart", { ns: "pages" })}</Button></Alert> : null}{state.status === "error" ? <Alert tone="danger" title={t("kinlist.list.errorTitle", { ns: "pages" })}>{t("kinlist.list.errorDescription", { ns: "pages" })}</Alert> : null}{state.status === "empty" ? <StatePanel title={t("kinlist.list.emptyTitle", { ns: "pages" })} description={t("kinlist.list.emptyDescription", { ns: "pages" })} tone="info" role="status" live="polite" headingLevel={2} /> : <ul className="kh-kinlist__list">{page.items.map((item) => <KinListItem key={item.id} name={item.name} categories={item.categories} remainingCategoryCount={item.remainingCategoryCount} authorName={authorFallback} authorDisplayName={item.author.displayName} />)}</ul>}<Pagination hasPrevious={Boolean(page.previousCursor)} hasNext={Boolean(page.nextCursor)} busy={busy} onPrevious={() => void loadPage(page.previousCursor, "previous")} onNext={() => void loadPage(page.nextCursor, "next")} label={t("kinlist.list.paginationLabel", { ns: "pages" })} previousLabel={t("actions.back", { ns: "common" })} nextLabel={t("actions.next", { ns: "common" })} statusLabel={t("kinlist.list.paginationStatus", { ns: "pages", count: page.items.length, pageSize: page.effectivePageSize })} /></section>;
}
