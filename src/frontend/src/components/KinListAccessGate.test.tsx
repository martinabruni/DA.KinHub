import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { KinListAccessGate } from "./KinListAccessGate";
import { KinHubFamilyProvider, useKinHubFamily } from "./KinHubFamilyContext";
import { ShellBarProvider } from "./ShellBarContext";

let online = true;
let account: { homeAccountId: string } | null = null;
let bootstrapHandler: (signal?: AbortSignal) => Promise<{ state: "family"; familyId: string } | { state: "onboarding" }> = () => Promise.resolve({ state: "onboarding" });
let createFamilyHandler: (name: string, signal?: AbortSignal) => Promise<{ state: "family"; familyId: string }> = () => Promise.resolve({ state: "family", familyId: "family-created" });
let createFamilyCallCount = 0;
const msalInstance = { name: "msal" };
const apiMocks = vi.hoisted(() => ({
  ApiResponseError: class extends Error {
    constructor(public readonly problem: { status: number; detail: string; code?: string }, public readonly correlationId?: string) {
      super(problem.detail);
    }
  }
}));

vi.mock("react-i18next", () => ({
  useTranslation: () => ({ t: (key: string) => key })
}));

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: msalInstance })
}));

vi.mock("../lib/auth", () => ({
  acquireApiAccessToken: () => Promise.resolve("token-123"),
  getActiveAccount: () => account ? { ...account } : null
}));

vi.mock("./ConnectivityProvider", () => ({
  useConnectivity: () => ({ online })
}));

vi.mock("../lib/api", () => {
  class ApiError extends Error {}
  class ApiNetworkError extends ApiError {}

  return {
    ApiError,
    ApiNetworkError,
    ApiResponseError: apiMocks.ApiResponseError,
    KinHubApiClient: class {
      getKinHubBootstrap(signal?: AbortSignal) {
        return bootstrapHandler(signal);
      }

      createFamily(body: { name: string }, signal?: AbortSignal) {
        createFamilyCallCount += 1;
        return createFamilyHandler(body.name, signal);
      }
    }
  };
});

function Probe() {
  const { familyId } = useKinHubFamily();
  return <output data-testid="family-id">{familyId ?? ""}</output>;
}

function renderGate() {
  return render(
    <ShellBarProvider>
      <KinHubFamilyProvider>
        <KinListAccessGate />
        <Probe />
      </KinHubFamilyProvider>
    </ShellBarProvider>
  );
}

describe("KinListAccessGate", () => {
  beforeEach(() => {
    online = true;
    account = null;
    bootstrapHandler = () => Promise.resolve({ state: "onboarding" });
    createFamilyHandler = () => Promise.resolve({ state: "family", familyId: "family-created" });
    createFamilyCallCount = 0;
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
      <ShellBarProvider>
        <KinHubFamilyProvider>
          <KinListAccessGate />
          <Probe />
        </KinHubFamilyProvider>
      </ShellBarProvider>
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
      <ShellBarProvider>
        <KinHubFamilyProvider>
          <KinListAccessGate />
          <Probe />
        </KinHubFamilyProvider>
      </ShellBarProvider>
    );

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent(""));
    expect(screen.getByText("kinlist.offline")).toBeInTheDocument();
  });

  it("opens the create form and stores the created family context in memory", async () => {
    account = { homeAccountId: "account-a" };

    renderGate();

    await screen.findByText("kinlist.onboarding");
    fireEvent.click(screen.getByText("actions.createFamily"));
    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "Famiglia Bruni" } });
    fireEvent.submit(input.closest("form")!);

    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent("family-created"));
    expect(screen.getByText("kinlist.ready")).toBeInTheDocument();
  });

  it("preserves the entered name after a validation error", async () => {
    account = { homeAccountId: "account-a" };
    createFamilyHandler = () => Promise.reject(new apiMocks.ApiResponseError({ status: 400, detail: "Denied", code: "family.nameInvalid" }));

    renderGate();

    await screen.findByText("kinlist.onboarding");
    fireEvent.click(screen.getByText("actions.createFamily"));
    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "  " } });
    fireEvent.submit(input.closest("form")!);

    await screen.findByText("kinlist.create.validationError");
    expect(input).toHaveValue("  ");
  });

  it("prevents a double submit before the rerender", async () => {
    account = { homeAccountId: "account-a" };
    let resolveCreate: (value: { state: "family"; familyId: string }) => void = () => undefined;
    createFamilyHandler = () => new Promise((resolve) => {
      resolveCreate = resolve;
    });

    renderGate();

    await screen.findByText("kinlist.onboarding");
    fireEvent.click(screen.getByText("actions.createFamily"));
    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "Famiglia Bruni" } });
    const form = input.closest("form");
    fireEvent.submit(form!);
    fireEvent.submit(form!);

    expect(createFamilyCallCount).toBe(1);

    resolveCreate({ state: "family", familyId: "family-created" });
    await waitFor(() => expect(screen.getByTestId("family-id")).toHaveTextContent("family-created"));
  });
});
