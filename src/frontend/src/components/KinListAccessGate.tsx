import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useRef, useState, type FormEvent, type RefObject } from "react";
import { useTranslation } from "react-i18next";
import { acquireApiAccessToken, getActiveAccount } from "../lib/auth";
import { ApiError, ApiNetworkError, ApiResponseError, KinHubApiClient } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";
import { useKinHubFamily } from "./KinHubFamilyContext";
import { useShellBar } from "./ShellBarContext";
import { Button } from "./ui/core";
import { StatePanel } from "./ui/feedback";
import { TextField } from "./ui/core";

type GateState =
  | { status: "loading" }
  | { status: "offline" }
  | { status: "onboarding" }
  | { status: "ready"; familyId: string }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "error" };

export function KinListAccessGate({ titleRef }: { titleRef?: RefObject<HTMLHeadingElement | null> }) {
  const { t } = useTranslation(["pages", "common"]);
  const { instance } = useMsal();
  const { online } = useConnectivity();
  const { setFamilyId } = useKinHubFamily();
  const { setContextualBar } = useShellBar();
  const [reloadToken, setReloadToken] = useState(0);
  const [state, setState] = useState<GateState>({ status: online ? "loading" : "offline" });
  const [createMode, setCreateMode] = useState(false);
  const [familyName, setFamilyName] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const account = getActiveAccount(instance);
  const accountKey = account?.homeAccountId ?? "";
  const hasAccount = accountKey.length > 0;
  const createButtonRef = useRef<HTMLButtonElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const submitLockRef = useRef(false);
  const createControllerRef = useRef<AbortController | null>(null);
  const client = useMemo(
    () => new KinHubApiClient(() => {
      const resolvedAccount = getActiveAccount(instance);
      return acquireApiAccessToken(instance, resolvedAccount?.homeAccountId === accountKey ? resolvedAccount : null);
    }),
    [accountKey, instance]
  );

  useEffect(() => {
    if (createMode) {
      inputRef.current?.focus();
    }
  }, [createMode]);

  useEffect(() => {
    if (state.status === "onboarding" && !createMode) {
      setContextualBar(
        <div className="kh-floating-bar kh-service-bar" aria-label={t("navigation.contextualBar", { ns: "common" })}>
          <Button onClick={openCreateMode}>{t("actions.createFamily", { ns: "common" })}</Button>
        </div>
      );
      return () => setContextualBar(null);
    }

    if (state.status === "error") {
      setContextualBar(
        <div className="kh-floating-bar kh-service-bar" aria-label={t("navigation.contextualBar", { ns: "common" })}>
          <Button variant="secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</Button>
        </div>
      );
      return () => setContextualBar(null);
    }

    setContextualBar(null);
    return undefined;
  }, [createMode, setContextualBar, state.status, t]);

  useEffect(() => {
    return () => {
      createControllerRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    createControllerRef.current?.abort();
    createControllerRef.current = null;
    submitLockRef.current = false;
    setSubmitting(false);

    if (!hasAccount) {
      setFamilyId(null);
      setCreateMode(false);
      setFamilyName("");
      setFieldError(null);
      setRequestError(null);
      setState({ status: "sessionExpired" });
      return;
    }

    if (!online) {
      setFamilyId(null);
      setCreateMode(false);
      setFamilyName("");
      setFieldError(null);
      setRequestError(null);
      setState({ status: "offline" });
      return;
    }

    const controller = new AbortController();
    setFamilyId(null);
    setState({ status: "loading" });

    void client.getKinHubBootstrap(controller.signal)
      .then((result) => {
        setFamilyId(result.state === "family" ? result.familyId : null);
        setState(result.state === "family"
          ? { status: "ready", familyId: result.familyId }
          : { status: "onboarding" });
        if (result.state === "onboarding") {
          setCreateMode(false);
          setFieldError(null);
          setRequestError(null);
        }
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
  }, [accountKey, client, hasAccount, online, reloadToken, setFamilyId]);

  function openCreateMode() {
    setCreateMode(true);
    setFieldError(null);
    setRequestError(null);
  }

  function closeCreateMode() {
    createControllerRef.current?.abort();
    createControllerRef.current = null;
    submitLockRef.current = false;
    setSubmitting(false);
    setCreateMode(false);
    setFieldError(null);
    setRequestError(null);
    requestAnimationFrame(() => createButtonRef.current?.focus());
  }

  async function handleCreateSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitLockRef.current || !hasAccount || !online) {
      return;
    }

    submitLockRef.current = true;
    setSubmitting(true);
    setFieldError(null);
    setRequestError(null);

    const controller = new AbortController();
    createControllerRef.current?.abort();
    createControllerRef.current = controller;

    try {
      const result = await client.createFamily({ name: familyName }, controller.signal);
      setFamilyId(result.familyId);
      setCreateMode(false);
      setState({ status: "ready", familyId: result.familyId });
      requestAnimationFrame(() => titleRef?.current?.focus());
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }

      if (error instanceof ApiResponseError) {
        if (error.problem.status === 401) {
          setFamilyId(null);
          setCreateMode(false);
          setFamilyName("");
          setState({ status: "sessionExpired" });
          return;
        }

        if (error.problem.status === 403) {
          setFamilyId(null);
          setCreateMode(false);
          setFamilyName("");
          setState({ status: "forbidden" });
          return;
        }

        if (error.problem.status === 400 && error.problem.code === "family.nameInvalid") {
          setFieldError(t("kinlist.create.validationError", { ns: "pages" }));
          requestAnimationFrame(() => inputRef.current?.focus());
          return;
        }
      }

      if (error instanceof ApiNetworkError) {
        setRequestError(t("kinlist.create.retryableError", { ns: "pages" }));
        return;
      }

      if (error instanceof ApiError) {
        setRequestError(t("kinlist.create.retryableError", { ns: "pages" }));
        return;
      }

      setRequestError(t("kinlist.create.retryableError", { ns: "pages" }));
    } finally {
      createControllerRef.current = null;
      submitLockRef.current = false;
      setSubmitting(false);
    }
  }

  if (state.status === "loading") {
    return <StatePanel data-kinlist-state="loading" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.loading", { ns: "pages" })} role="status" live="polite" busy />;
  }

  if (state.status === "offline") {
    return <StatePanel data-kinlist-state="offline" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.offline", { ns: "pages" })} tone="warning" role="status" live="polite" />;
  }

  if (state.status === "sessionExpired") {
    return <StatePanel data-kinlist-state="sessionExpired" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.sessionExpired", { ns: "pages" })} tone="warning" role="alert" live="assertive" />;
  }

  if (state.status === "forbidden") {
    return <StatePanel data-kinlist-state="forbidden" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.forbidden", { ns: "pages" })} tone="danger" role="alert" live="assertive" />;
  }

  if (state.status === "error") {
    return (
      <StatePanel data-kinlist-state="error" title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.error", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<Button variant="secondary" onClick={() => setReloadToken((current) => current + 1)}>{t("actions.retry", { ns: "common" })}</Button>} />
    );
  }

  if (state.status === "onboarding") {
    return (
      <div className="kh-onboarding-panel" data-kinlist-state="onboarding">
        <StatePanel title={t("kinlist.title", { ns: "pages" })} description={t("kinlist.onboarding", { ns: "pages" })} role="status" live="polite" />
        {!createMode ? (
          <>
            <div className="page-actions">
              <Button ref={createButtonRef} onClick={openCreateMode}>{t("actions.createFamily", { ns: "common" })}</Button>
              <Button variant="secondary" disabled>{t("actions.joinFamily", { ns: "common" })}</Button>
            </div>
            <p className="lead lead--compact">{t("kinlist.onboardingPending", { ns: "pages" })}</p>
          </>
        ) : (
          <form className="kh-onboarding-form" onSubmit={(event) => { void handleCreateSubmit(event); }} aria-busy={submitting}>
            <TextField
              ref={inputRef}
              id="family-name-input"
              name="name"
              label={t("kinlist.create.label", { ns: "pages" })}
              helper={t("kinlist.create.helper", { ns: "pages" })}
              error={fieldError ?? requestError ?? undefined}
              value={familyName}
              onChange={(event) => {
                setFamilyName(event.target.value);
                setFieldError(null);
                setRequestError(null);
              }}
              disabled={submitting}
            />
            <div className="page-actions">
              <Button type="submit" disabled={submitting} loading={submitting}>{submitting ? t("kinlist.create.pending", { ns: "pages" }) : t("actions.create", { ns: "common" })}</Button>
              <Button variant="secondary" onClick={closeCreateMode} disabled={submitting}>{t("actions.back", { ns: "common" })}</Button>
            </div>
          </form>
        )}
      </div>
    );
  }

  return (
    <StatePanel data-kinlist-state="ready" data-family-id={state.familyId} title={t("kinlist.ready", { ns: "pages" })} description={t("kinlist.readyDetail", { ns: "pages" })} tone="success" />
  );
}
