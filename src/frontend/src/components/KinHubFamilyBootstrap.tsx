import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { acquireApiAccessToken, getActiveAccount } from "../lib/auth";
import { ApiError, ApiNetworkError, ApiResponseError, KinHubApiClient } from "../lib/api";
import { useConnectivity } from "./ConnectivityProvider";
import { useKinHubFamily } from "./KinHubFamilyContext";
import { Button, TextField } from "./ui/core";
import { StatePanel } from "./ui/feedback";

export type KinHubBootstrapState =
  | { status: "initializing" }
  | { status: "visitor" }
  | { status: "loading" }
  | { status: "offline" }
  | { status: "onboarding" }
  | { status: "family"; familyId: string }
  | { status: "sessionExpired" }
  | { status: "forbidden" }
  | { status: "error" };

export function useKinHubFamilyBootstrap() {
  const { instance, inProgress } = useMsal();
  const { online } = useConnectivity();
  const { setFamilyId } = useKinHubFamily();
  const [reloadToken, setReloadToken] = useState(0);
  const [state, setState] = useState<KinHubBootstrapState>({ status: "initializing" });
  const account = getActiveAccount(instance);
  const accountKey = account?.homeAccountId ?? "";
  const hasAccount = accountKey.length > 0;
  const createControllerRef = useRef<AbortController | null>(null);
  const client = useMemo(
    () => new KinHubApiClient(() => {
      const resolvedAccount = getActiveAccount(instance);
      return acquireApiAccessToken(instance, resolvedAccount?.homeAccountId === accountKey ? resolvedAccount : null);
    }),
    [accountKey, instance]
  );

  useEffect(() => () => {
    createControllerRef.current?.abort();
  }, []);

  useEffect(() => {
    createControllerRef.current?.abort();
    createControllerRef.current = null;

    if (inProgress !== InteractionStatus.None) {
      setFamilyId(null);
      setState({ status: "initializing" });
      return;
    }

    if (!hasAccount) {
      setFamilyId(null);
      setState({ status: "visitor" });
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

    void client.getKinHubBootstrap(controller.signal)
      .then((result) => {
        setFamilyId(result.state === "family" ? result.familyId : null);
        setState(result.state === "family" ? { status: "family", familyId: result.familyId } : { status: "onboarding" });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setFamilyId(null);
        setState(resolveBootstrapError(error));
      });

    return () => controller.abort();
  }, [accountKey, client, hasAccount, inProgress, online, reloadToken, setFamilyId]);

  async function createFamily(name: string): Promise<"created" | "validation_error" | "retryable_error"> {
    if (!hasAccount) {
      setFamilyId(null);
      setState({ status: "sessionExpired" });
      return "retryable_error";
    }

    if (!online) {
      setFamilyId(null);
      setState({ status: "offline" });
      return "retryable_error";
    }

    const controller = new AbortController();
    createControllerRef.current?.abort();
    createControllerRef.current = controller;

    try {
      const result = await client.createFamily({ name }, controller.signal);
      setFamilyId(result.familyId);
      setState({ status: "family", familyId: result.familyId });
      return "created";
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === "AbortError") {
        throw error;
      }

      if (error instanceof ApiResponseError) {
        if (error.problem.status === 401) {
          setFamilyId(null);
          setState({ status: "sessionExpired" });
          return "retryable_error";
        }

        if (error.problem.status === 403) {
          setFamilyId(null);
          setState({ status: "forbidden" });
          return "retryable_error";
        }

        if (error.problem.status === 400 && error.problem.code === "family.nameInvalid") {
          return "validation_error";
        }
      }

      if (error instanceof ApiNetworkError) {
        setFamilyId(null);
        setState({ status: "offline" });
        return "retryable_error";
      }

      if (error instanceof ApiError) {
        return "retryable_error";
      }

      return "retryable_error";
    } finally {
      createControllerRef.current = null;
    }
  }

  return {
    client,
    state,
    hasAccount,
    online,
    retry: () => setReloadToken((current) => current + 1),
    createFamily
  };
}

export function KinHubOnboardingPanel({ onCreate }: { onCreate: (name: string) => Promise<"created" | "validation_error" | "retryable_error"> }) {
  const { t } = useTranslation(["pages", "common"]);
  const [createMode, setCreateMode] = useState(false);
  const [familyName, setFamilyName] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const createButtonRef = useRef<HTMLButtonElement>(null);
  const submitLockRef = useRef(false);

  useEffect(() => {
    if (createMode) {
      inputRef.current?.focus();
    }
  }, [createMode]);

  function openCreateMode() {
    setCreateMode(true);
    setFieldError(null);
    setRequestError(null);
  }

  function closeCreateMode() {
    submitLockRef.current = false;
    setSubmitting(false);
    setCreateMode(false);
    setFieldError(null);
    setRequestError(null);
    requestAnimationFrame(() => createButtonRef.current?.focus());
  }

  async function handleCreateSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitLockRef.current) {
      return;
    }

    submitLockRef.current = true;
    setSubmitting(true);
    setFieldError(null);
    setRequestError(null);

    try {
      const result = await onCreate(familyName);
      if (result === "validation_error") {
        setFieldError(t("kinlist.create.validationError", { ns: "pages" }));
        requestAnimationFrame(() => inputRef.current?.focus());
        return;
      }

      if (result === "retryable_error") {
        setRequestError(t("kinlist.create.retryableError", { ns: "pages" }));
      }
    } finally {
      submitLockRef.current = false;
      setSubmitting(false);
    }
  }

  return (
    <div className="kh-onboarding-panel" data-kinhub-state="onboarding">
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

function resolveBootstrapError(error: unknown): KinHubBootstrapState {
  if (error instanceof ApiResponseError) {
    if (error.problem.status === 401) {
      return { status: "sessionExpired" };
    }

    if (error.problem.status === 403) {
      return { status: "forbidden" };
    }
  }

  if (error instanceof ApiNetworkError) {
    return { status: "offline" };
  }

  if (error instanceof ApiError) {
    return { status: "error" };
  }

  return { status: "error" };
}
