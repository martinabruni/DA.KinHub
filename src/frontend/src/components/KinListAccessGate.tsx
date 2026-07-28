import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useRef, useState, type FormEvent, type RefObject } from "react";
import { useTranslation } from "react-i18next";
import { acquireApiAccessToken, getActiveAccount } from "../lib/auth";
import { ApiError, ApiNetworkError, ApiResponseError, KinHubApiClient } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";
import { useKinHubFamily } from "./KinHubFamilyContext";

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
  const [reloadToken, setReloadToken] = useState(0);
  const [state, setState] = useState<GateState>({ status: online ? "loading" : "offline" });
  const [createMode, setCreateMode] = useState(false);
  const [familyName, setFamilyName] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const account = getActiveAccount(instance);
  const accountKey = account?.homeAccountId ?? "";
  const createButtonRef = useRef<HTMLButtonElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const submitLockRef = useRef(false);
  const createControllerRef = useRef<AbortController | null>(null);
  const client = useMemo(
    () => new KinHubApiClient(() => acquireApiAccessToken(instance, account)),
    [account, instance]
  );

  useEffect(() => {
    if (createMode) {
      inputRef.current?.focus();
    }
  }, [createMode]);

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

    if (!account) {
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
  }, [account, accountKey, client, online, reloadToken, setFamilyId]);

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
    if (submitLockRef.current || !account || !online) {
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
        {!createMode ? (
          <>
            <div className="actions">
              <button type="button" ref={createButtonRef} className="button" onClick={openCreateMode}>{t("actions.createFamily", { ns: "common" })}</button>
              <button type="button" className="button secondary" disabled>{t("actions.joinFamily", { ns: "common" })}</button>
            </div>
            <p className="muted-text">{t("kinlist.onboardingPending", { ns: "pages" })}</p>
          </>
        ) : (
          <form className="stack" onSubmit={(event) => { void handleCreateSubmit(event); }} aria-busy={submitting}>
            <label className="kinlist-form-field" htmlFor="family-name-input">
              <span>{t("kinlist.create.label", { ns: "pages" })}</span>
              <input
                ref={inputRef}
                id="family-name-input"
                name="name"
                value={familyName}
                onChange={(event) => {
                  setFamilyName(event.target.value);
                  setFieldError(null);
                  setRequestError(null);
                }}
                aria-describedby="family-name-helper family-name-error"
                aria-invalid={fieldError ? true : undefined}
                disabled={submitting}
              />
            </label>
            <p id="family-name-helper" className="muted-text">{t("kinlist.create.helper", { ns: "pages" })}</p>
            <p id="family-name-error" className="error-message" role={fieldError || requestError ? "alert" : undefined}>
              {fieldError ?? requestError ?? ""}
            </p>
            <div className="actions">
              <button type="submit" className="button" disabled={submitting}>{submitting ? t("kinlist.create.pending", { ns: "pages" }) : t("actions.create", { ns: "common" })}</button>
              <button type="button" className="button secondary" onClick={closeCreateMode} disabled={submitting}>{t("actions.back", { ns: "common" })}</button>
            </div>
          </form>
        )}
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
