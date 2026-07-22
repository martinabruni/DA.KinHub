import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { KinListAccessGate } from "./KinListAccessGate";
import { KinListFamilyProvider, useKinListFamily } from "./KinListFamilyContext";

let online = true;
let account: { homeAccountId: string } | null = null;
let bootstrapHandler: (signal?: AbortSignal) => Promise<{ state: "family"; familyId: string } | { state: "onboarding" }> = () => Promise.resolve({ state: "onboarding" });
const msalInstance = { name: "msal" };

vi.mock("react-i18next", () => ({
  useTranslation: () => ({ t: (key: string) => key })
}));

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: msalInstance })
}));

vi.mock("../lib/auth", () => ({
  acquireApiAccessToken: () => Promise.resolve("token-123"),
  getActiveAccount: () => account
}));

vi.mock("./ConnectivityProvider", () => ({
  useConnectivity: () => ({ online })
}));

vi.mock("../lib/api", () => {
  class ApiError extends Error {}
  class ApiNetworkError extends ApiError {}
  class ApiResponseError extends ApiError {
    constructor(public readonly problem: { status: number; detail: string }, public readonly correlationId?: string) {
      super(problem.detail);
    }
  }

  return {
    ApiError,
    ApiNetworkError,
    ApiResponseError,
    KinHubApiClient: class {
      getKinListBootstrap(signal?: AbortSignal) {
        return bootstrapHandler(signal);
      }
    }
  };
});

function Probe() {
  const { familyId } = useKinListFamily();
  return <output data-testid="family-id">{familyId ?? ""}</output>;
}

function renderGate() {
  return render(
    <KinListFamilyProvider>
      <KinListAccessGate />
      <Probe />
    </KinListFamilyProvider>
  );
}

describe("KinListAccessGate", () => {
  beforeEach(() => {
    online = true;
    account = null;
    bootstrapHandler = () => Promise.resolve({ state: "onboarding" });
  });

  it("stores the family context only in memory after an authorized bootstrap", async () => {
    account = { homeAccountId: "account-a" };
    bootstrapHandler = () => Promise.resolve({ state: "family", familyId: "family-a" });

    renderGate();

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent("family-a"));
    expect(screen.getByText("kinlist.ready")).toBeInTheDocument();
  });

  it("clears the family context when the account changes and bootstrap resolves to onboarding", async () => {
    account = { homeAccountId: "account-a" };
    bootstrapHandler = () => Promise.resolve({ state: "family", familyId: "family-a" });

    const view = renderGate();

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent("family-a"));

    account = { homeAccountId: "account-b" };
    bootstrapHandler = () => Promise.resolve({ state: "onboarding" });
    view.rerender(
      <KinListFamilyProvider>
        <KinListAccessGate />
        <Probe />
      </KinListFamilyProvider>
    );

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent(""));
    expect(screen.getByText("kinlist.onboarding")).toBeInTheDocument();
  });

  it("clears the family context when the browser goes offline", async () => {
    account = { homeAccountId: "account-a" };
    bootstrapHandler = () => Promise.resolve({ state: "family", familyId: "family-a" });

    const view = renderGate();

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent("family-a"));

    online = false;
    view.rerender(
      <KinListFamilyProvider>
        <KinListAccessGate />
        <Probe />
      </KinListFamilyProvider>
    );

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent(""));
    expect(screen.getByText("kinlist.offline")).toBeInTheDocument();
  });
});
