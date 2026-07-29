import { InteractionStatus } from "@azure/msal-browser";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProtectedRoute } from "./ProtectedRoute";

let account: { homeAccountId: string } | null = null;
let inProgress: string = InteractionStatus.None;

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: { name: "msal" }, inProgress })
}));

vi.mock("../lib/auth", () => ({
  authConfig: { configured: true },
  getActiveAccount: () => account,
  loginForApiAccess: () => Promise.resolve()
}));

vi.mock("react-i18next", () => ({
  useTranslation: () => ({ t: (key: string) => key })
}));

vi.mock("./PageScaffold", () => ({
  PageScaffold: ({ children }: { children: ReactNode }) => <div>{children}</div>
}));

vi.mock("./ui/feedback", () => ({
  StatePanel: ({ title, description }: { title: string; description: string }) => <div><strong>{title}</strong><span>{description}</span></div>
}));

vi.mock("./ui/core", () => ({
  Button: ({ children, onClick }: { children: ReactNode; onClick?: () => void }) => <button onClick={onClick}>{children}</button>
}));

describe("ProtectedRoute", () => {
  beforeEach(() => {
    account = null;
    inProgress = InteractionStatus.None;
  });

  it("renders the protected content when MSAL restores an account", () => {
    account = { homeAccountId: "restored-account" };

    render(<ProtectedRoute routeId="kinlist"><div data-testid="protected-content">family shell</div></ProtectedRoute>);

    expect(screen.getByTestId("protected-content")).toBeInTheDocument();
  });

  it("keeps the route in loading while MSAL initializes", () => {
    inProgress = InteractionStatus.Startup;

    render(<ProtectedRoute routeId="kinlist"><div data-testid="protected-content">family shell</div></ProtectedRoute>);

    expect(screen.queryByTestId("protected-content")).not.toBeInTheDocument();
    expect(screen.getByText("states.loading")).toBeInTheDocument();
  });
});
