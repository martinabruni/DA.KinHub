import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { HomePage } from "./HomePage";

const bootstrapState: {
  state: { status: string; familyId?: string };
  client: { getFamilyServices: ReturnType<typeof vi.fn> };
  online: boolean;
  retry: ReturnType<typeof vi.fn>;
  createFamily: ReturnType<typeof vi.fn>;
} = vi.hoisted(() => ({
  state: { status: "visitor" },
  client: {
    getFamilyServices: vi.fn()
  },
  online: true,
  retry: vi.fn(),
  createFamily: vi.fn()
}));

vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { resolvedLanguage: "it" }
  })
}));

vi.mock("../components/KinHubFamilyBootstrap", () => ({
  useKinHubFamilyBootstrap: () => bootstrapState,
  KinHubOnboardingPanel: () => <div>onboarding-panel</div>
}));

describe("HomePage", () => {
  beforeEach(() => {
    bootstrapState.state = { status: "visitor" };
    bootstrapState.client.getFamilyServices.mockReset();
  });

  it("shows the visitor state without service cards", () => {
    render(<MemoryRouter><HomePage /></MemoryRouter>);

    expect(screen.getByText("home.visitorDescription")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /kinlist/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /release/i })).not.toBeInTheDocument();
  });

  it("renders only API-driven family services", async () => {
    bootstrapState.state = { status: "family", familyId: "family-a" };
    bootstrapState.client.getFamilyServices.mockResolvedValue({
      services: [{ key: "kinlist", route: "/kinlist", name: "KinList", description: "Shared list" }]
    });

    render(<MemoryRouter><HomePage /></MemoryRouter>);

    await waitFor(() => expect(screen.getByRole("link", { name: /KinList Shared list/i })).toBeInTheDocument());
    expect(screen.queryByRole("link", { name: /release/i })).not.toBeInTheDocument();
  });
});
