import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useKinHubFamilyBootstrap } from "../components/KinHubFamilyBootstrap";
import { InviteRow, MemberRow } from "../components/KinPatterns";
import { PageScaffold } from "../components/PageScaffold";
import { Button, ButtonLink, Card, Pagination } from "../components/ui/core";
import { Alert, StatePanel } from "../components/ui/feedback";
import { ApiError, ApiNetworkError, ApiResponseError, type FamilyDetails, type FamilyInvitationsPage, type FamilyMembersPage } from "../lib/api";
import { useAccountProfileName } from "../components/AccountProfileContext";

type CollectionState<TPage> =
  | { status: "loading" }
  | { status: "ready"; page: TPage; busy: boolean }
  | { status: "empty"; page: TPage }
  | { status: "cursorInvalid" }
  | { status: "error" };

const PAGE_SIZE = 50;

export function FamilySettingsPage() {
  const { t, i18n } = useTranslation(["pages", "common"]);
  const bootstrap = useKinHubFamilyBootstrap();
  const accountName = useAccountProfileName();
  const generationRef = useRef(0);
  const familyCardRef = useRef<HTMLHeadingElement>(null);
  const membersHeadingRef = useRef<HTMLHeadingElement>(null);
  const invitationsHeadingRef = useRef<HTMLHeadingElement>(null);
  const [details, setDetails] = useState<FamilyDetails | null>(null);
  const [globalState, setGlobalState] = useState<"loading" | "ready" | "sessionExpired" | "forbidden" | "offline" | "error" | "inconsistent">("loading");
  const [membersState, setMembersState] = useState<CollectionState<FamilyMembersPage>>({ status: "loading" });
  const [invitationsState, setInvitationsState] = useState<CollectionState<FamilyInvitationsPage>>({ status: "loading" });
  const dateFormatter = useMemo(() => new Intl.DateTimeFormat(i18n.language === "it" ? "it" : "en", { dateStyle: "medium", timeStyle: "short" }), [i18n.language]);

  useEffect(() => {
    if (bootstrap.state.status !== "family") {
      setDetails(null);
      setMembersState({ status: "loading" });
      setInvitationsState({ status: "loading" });
      setGlobalState(bootstrap.state.status === "offline" ? "offline" : bootstrap.state.status === "sessionExpired" || bootstrap.state.status === "visitor" ? "sessionExpired" : bootstrap.state.status === "forbidden" ? "forbidden" : bootstrap.state.status === "error" ? "error" : "loading");
      generationRef.current += 1;
      return;
    }

    void loadInitial(bootstrap.state.familyId);
  }, [bootstrap.state]);

  async function loadInitial(familyId: string) {
    const generation = ++generationRef.current;
    const detailsController = new AbortController();
    const membersController = new AbortController();
    const invitationsController = new AbortController();
    setGlobalState("loading");
    setDetails(null);
    setMembersState({ status: "loading" });
    setInvitationsState({ status: "loading" });

    try {
      const [nextDetails, nextMembers, nextInvitations] = await Promise.all([
        bootstrap.client.getFamilyDetails(familyId, detailsController.signal),
        bootstrap.client.getFamilyMembers(familyId, PAGE_SIZE, null, membersController.signal),
        bootstrap.client.getFamilyInvitations(familyId, PAGE_SIZE, null, invitationsController.signal)
      ]);

      if (generation !== generationRef.current) {
        return;
      }

      setDetails(nextDetails);
      setMembersState({ status: "ready", page: nextMembers, busy: false });
      setInvitationsState(nextInvitations.items.length === 0 ? { status: "empty", page: nextInvitations } : { status: "ready", page: nextInvitations, busy: false });
      setGlobalState("ready");
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      if (generation !== generationRef.current) {
        return;
      }

      setDetails(null);
      setMembersState({ status: "loading" });
      setInvitationsState({ status: "loading" });
      setGlobalState(resolveGlobalError(error));
    } finally {
      detailsController.abort();
      membersController.abort();
      invitationsController.abort();
    }
  }

  async function loadMembers(cursor: string | null, mode: "refresh" | "next" | "previous") {
    if (bootstrap.state.status !== "family") {
      return;
    }

    const currentPage = membersState.status === "ready" ? membersState.page : null;
    const generation = generationRef.current;
    const controller = new AbortController();
    setMembersState(currentPage ? { status: "ready", page: currentPage, busy: true } : { status: "loading" });

    try {
      const page = await bootstrap.client.getFamilyMembers(bootstrap.state.familyId, PAGE_SIZE, cursor, controller.signal);
      if (generation !== generationRef.current) {
        return;
      }

      setMembersState({ status: "ready", page, busy: false });
      if (mode !== "refresh") {
        requestAnimationFrame(() => membersHeadingRef.current?.focus());
      }
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      if (generation !== generationRef.current) {
        return;
      }

      if (error instanceof ApiResponseError && error.problem.status === 409 && error.problem.code === "family.stateInconsistent") {
        setDetails(null);
        setMembersState({ status: "loading" });
        setInvitationsState({ status: "loading" });
        setGlobalState("inconsistent");
        return;
      }

      if (error instanceof ApiResponseError && error.problem.status === 400 && error.problem.code === "pagination.cursorInvalid") {
        setMembersState({ status: "cursorInvalid" });
        requestAnimationFrame(() => membersHeadingRef.current?.focus());
        return;
      }

      const nextGlobal = resolveGlobalError(error);
      if (nextGlobal !== "error") {
        setDetails(null);
        setMembersState({ status: "loading" });
        setInvitationsState({ status: "loading" });
        setGlobalState(nextGlobal);
        return;
      }

      setMembersState({ status: "error" });
      requestAnimationFrame(() => membersHeadingRef.current?.focus());
    } finally {
      controller.abort();
    }
  }

  async function loadInvitations(cursor: string | null, mode: "refresh" | "next" | "previous") {
    if (bootstrap.state.status !== "family") {
      return;
    }

    const currentPage = invitationsState.status === "ready" || invitationsState.status === "empty" ? invitationsState.page : null;
    const generation = generationRef.current;
    const controller = new AbortController();
    setInvitationsState(currentPage && currentPage.items.length === 0 ? { status: "empty", page: currentPage } : currentPage ? { status: "ready", page: currentPage, busy: true } : { status: "loading" });

    try {
      const page = await bootstrap.client.getFamilyInvitations(bootstrap.state.familyId, PAGE_SIZE, cursor, controller.signal);
      if (generation !== generationRef.current) {
        return;
      }

      setInvitationsState(page.items.length === 0 ? { status: "empty", page } : { status: "ready", page, busy: false });
      if (mode !== "refresh") {
        requestAnimationFrame(() => invitationsHeadingRef.current?.focus());
      }
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      if (generation !== generationRef.current) {
        return;
      }

      if (error instanceof ApiResponseError && error.problem.status === 400 && error.problem.code === "pagination.cursorInvalid") {
        setInvitationsState({ status: "cursorInvalid" });
        requestAnimationFrame(() => invitationsHeadingRef.current?.focus());
        return;
      }

      const nextGlobal = resolveGlobalError(error);
      if (nextGlobal !== "error") {
        setDetails(null);
        setMembersState({ status: "loading" });
        setInvitationsState({ status: "loading" });
        setGlobalState(nextGlobal);
        return;
      }

      setInvitationsState({ status: "error" });
      requestAnimationFrame(() => invitationsHeadingRef.current?.focus());
    } finally {
      controller.abort();
    }
  }

  if (bootstrap.state.status === "onboarding") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.onboardingTitle", { ns: "pages" })} description={t("familySettings.onboardingDescription", { ns: "pages" })} tone="info" action={<ButtonLink to="/kinlist">{t("familySettings.onboardingAction", { ns: "pages" })}</ButtonLink>} /></PageScaffold>;
  }

  if (bootstrap.state.status === "initializing" || bootstrap.state.status === "loading" || globalState === "loading") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.loadingTitle", { ns: "pages" })} description={t("familySettings.loadingDescription", { ns: "pages" })} role="status" live="polite" busy /></PageScaffold>;
  }

  if (bootstrap.state.status === "offline" || globalState === "offline") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.offlineTitle", { ns: "pages" })} description={t("familySettings.offlineDescription", { ns: "pages" })} tone="warning" role="status" live="polite" /></PageScaffold>;
  }

  if (bootstrap.state.status === "visitor" || bootstrap.state.status === "sessionExpired" || globalState === "sessionExpired") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.sessionExpiredTitle", { ns: "pages" })} description={t("familySettings.sessionExpiredDescription", { ns: "pages" })} tone="warning" role="alert" live="assertive" /></PageScaffold>;
  }

  if (bootstrap.state.status === "forbidden" || globalState === "forbidden") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.forbiddenTitle", { ns: "pages" })} description={t("familySettings.forbiddenDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" /></PageScaffold>;
  }

  if (globalState === "inconsistent") {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.inconsistentTitle", { ns: "pages" })} description={t("familySettings.inconsistentDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={() => bootstrap.state.status === "family" ? void loadInitial(bootstrap.state.familyId) : undefined}>{t("actions.retry", { ns: "common" })}</Button>} /></PageScaffold>;
  }

  if (globalState === "error" || details === null) {
    return <PageScaffold routeId="familySettings"><StatePanel title={t("familySettings.errorTitle", { ns: "pages" })} description={t("familySettings.errorDescription", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={() => bootstrap.state.status === "family" ? void loadInitial(bootstrap.state.familyId) : undefined}>{t("actions.retry", { ns: "common" })}</Button>} /></PageScaffold>;
  }

  const membersPage = membersState.status === "ready" ? membersState.page : null;
  const invitationsPage = invitationsState.status === "ready" || invitationsState.status === "empty" ? invitationsState.page : null;
  const memberFallback = t("familySettings.memberFallback", { ns: "pages" });
  const invitationCreatorFallback = t("familySettings.memberFallback", { ns: "pages" });

  return (
    <PageScaffold routeId="familySettings">
      <div className="kh-service-grid">
        <Card className="kh-settings-card">
          <h2 ref={familyCardRef} tabIndex={-1}>{t("familySettings.familyCardTitle", { ns: "pages" })}</h2>
          <p>{t("familySettings.familyCardDescription", { ns: "pages" })}</p>
          <strong>{details.name}</strong>
        </Card>

        <Card className="kh-settings-card">
          <h2 ref={membersHeadingRef} tabIndex={-1}>{t("familySettings.membersTitle", { ns: "pages" })}</h2>
          <p>{t("familySettings.membersDescription", { ns: "pages" })}</p>
          {membersState.status === "loading" ? <StatePanel title={t("familySettings.membersLoadingTitle", { ns: "pages" })} description={t("familySettings.membersLoadingDescription", { ns: "pages" })} role="status" live="polite" busy headingLevel={3} /> : null}
          {membersState.status === "cursorInvalid" ? <Alert tone="warning" title={t("familySettings.cursorInvalidTitle", { ns: "pages" })}>{t("familySettings.cursorInvalidDescription", { ns: "pages" })} <Button variant="ghost" onClick={() => void loadMembers(null, "refresh")}>{t("familySettings.restartSection", { ns: "pages" })}</Button></Alert> : null}
          {membersState.status === "error" ? <Alert tone="danger" title={t("familySettings.membersErrorTitle", { ns: "pages" })}>{t("familySettings.membersErrorDescription", { ns: "pages" })}</Alert> : null}
          {membersPage ? <ul>{membersPage.items.map((member, index) => {
            const displayName = member.isCurrentUser ? (accountName?.trim() || member.displayName || memberFallback) : (member.displayName ?? memberFallback);
            const initials = member.initials ?? "?";
            return <MemberRow key={`${displayName}-${index}`} label={displayName} displayName={displayName} initials={initials} status={t("familySettings.memberStatus", { ns: "pages" })} />;
          })}</ul> : null}
          {membersPage ? <Pagination hasPrevious={Boolean(membersPage.previousCursor)} hasNext={Boolean(membersPage.nextCursor)} busy={membersState.status === "ready" && membersState.busy} onPrevious={() => void loadMembers(membersPage.previousCursor, "previous")} onNext={() => void loadMembers(membersPage.nextCursor, "next")} label={t("familySettings.membersPaginationLabel", { ns: "pages" })} previousLabel={t("actions.back", { ns: "common" })} nextLabel={t("actions.next", { ns: "common" })} statusLabel={t("familySettings.paginationStatus", { ns: "pages", count: membersPage.items.length, pageSize: membersPage.effectivePageSize })} /> : null}
        </Card>

        <Card className="kh-settings-card">
          <h2 ref={invitationsHeadingRef} tabIndex={-1}>{t("familySettings.invitationsTitle", { ns: "pages" })}</h2>
          <p>{t("familySettings.invitationsDescription", { ns: "pages" })}</p>
          {invitationsState.status === "loading" ? <StatePanel title={t("familySettings.invitationsLoadingTitle", { ns: "pages" })} description={t("familySettings.invitationsLoadingDescription", { ns: "pages" })} role="status" live="polite" busy headingLevel={3} /> : null}
          {invitationsState.status === "empty" ? <StatePanel title={t("familySettings.invitationsEmptyTitle", { ns: "pages" })} description={t("familySettings.invitationsEmptyDescription", { ns: "pages" })} tone="info" role="status" live="polite" headingLevel={3} /> : null}
          {invitationsState.status === "cursorInvalid" ? <Alert tone="warning" title={t("familySettings.cursorInvalidTitle", { ns: "pages" })}>{t("familySettings.cursorInvalidDescription", { ns: "pages" })} <Button variant="ghost" onClick={() => void loadInvitations(null, "refresh")}>{t("familySettings.restartSection", { ns: "pages" })}</Button></Alert> : null}
          {invitationsState.status === "error" ? <Alert tone="danger" title={t("familySettings.invitationsErrorTitle", { ns: "pages" })}>{t("familySettings.invitationsErrorDescription", { ns: "pages" })}</Alert> : null}
          {invitationsState.status === "ready" && invitationsPage ? <ul>{invitationsPage.items.map((invitation) => {
            const displayName = invitation.creator.displayName ?? invitationCreatorFallback;
            return <InviteRow key={invitation.id} creatorLabel={displayName} creatorDisplayName={displayName} creatorInitials={invitation.creator.initials ?? "?"} createdAtLabel={t("familySettings.invitationCreatedAt", { ns: "pages", value: dateFormatter.format(new Date(invitation.createdAt)) })} expiresAtLabel={t("familySettings.invitationExpiresAt", { ns: "pages", value: dateFormatter.format(new Date(invitation.expiresAt)) })} status={t("familySettings.invitationStatusActive", { ns: "pages" })} />;
          })}</ul> : null}
          {invitationsPage ? <Pagination hasPrevious={Boolean(invitationsPage.previousCursor)} hasNext={Boolean(invitationsPage.nextCursor)} busy={invitationsState.status === "ready" && invitationsState.busy} onPrevious={() => void loadInvitations(invitationsPage.previousCursor, "previous")} onNext={() => void loadInvitations(invitationsPage.nextCursor, "next")} label={t("familySettings.invitationsPaginationLabel", { ns: "pages" })} previousLabel={t("actions.back", { ns: "common" })} nextLabel={t("actions.next", { ns: "common" })} statusLabel={t("familySettings.paginationStatus", { ns: "pages", count: invitationsPage.items.length, pageSize: invitationsPage.effectivePageSize })} /> : null}
        </Card>
      </div>
    </PageScaffold>
  );
}

function resolveGlobalError(error: unknown): "sessionExpired" | "forbidden" | "offline" | "error" | "inconsistent" {
  if (error instanceof ApiResponseError) {
    if (error.problem.status === 401) {
      return "sessionExpired";
    }

    if (error.problem.status === 403) {
      return "forbidden";
    }

    if (error.problem.status === 409 && error.problem.code === "family.stateInconsistent") {
      return "inconsistent";
    }
  }

  if (error instanceof ApiNetworkError) {
    return "offline";
  }

  if (error instanceof ApiError) {
    return "error";
  }

  return "error";
}
