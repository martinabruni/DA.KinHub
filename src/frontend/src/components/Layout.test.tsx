import { render, screen, fireEvent } from "@testing-library/react";
import { useEffect } from "react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { Layout } from "./Layout";
import { useShellBar } from "./ShellBarContext";

beforeAll(() => {
  class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
  }

  vi.stubGlobal("ResizeObserver", ResizeObserverMock);
});

vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string, values?: { current?: number; total?: number }) => {
      if (key === "navigation.pageLabel" && values) {
        return `Bar ${values.current} of ${values.total}`;
      }

      return key;
    },
    i18n: { language: "it", changeLanguage: () => Promise.resolve() }
  })
}));

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: { name: "msal" } })
}));

vi.mock("../lib/auth", () => ({
  authConfig: { configured: false },
  getActiveAccount: () => null,
  loginForApiAccess: () => Promise.resolve(),
  logoutCurrentAccount: () => Promise.resolve()
}));

vi.mock("./ThemeProvider", () => ({
  useTheme: () => ({ theme: "light", setTheme: () => undefined })
}));

vi.mock("./VersionNotification", () => ({
  VersionNotification: () => <div>Version notification</div>
}));

vi.mock("./Onboarding", () => ({
  Onboarding: () => <div>Onboarding</div>
}));

function ContextualPage() {
  const { setContextualBar } = useShellBar();

  useEffect(() => {
    setContextualBar(<div>Contextual actions</div>);
    return () => setContextualBar(null);
  }, [setContextualBar]);

  return <div>Page body</div>;
}

describe("Layout", () => {
  it("keeps the floating shell mounted and exposes contextual bars through the shared contract", () => {
    const router = createMemoryRouter([
      {
        path: "/",
        element: <Layout />,
        children: [{ index: true, element: <ContextualPage /> }]
      }
    ]);

    render(<RouterProvider router={router} />);

    expect(screen.getByText("Page body")).toBeInTheDocument();
    expect(document.querySelector(".app-floating-bars")).not.toBeNull();
    const carousel = document.querySelector<HTMLElement>(".kh-floating-carousel");
    expect(carousel).not.toBeNull();

    if (carousel) {
      fireEvent.keyDown(carousel, { key: "ArrowRight" });
    }

    expect(screen.getByText("Contextual actions")).toBeInTheDocument();
  });

  it("adds the user guide link to the information menu", () => {
    const router = createMemoryRouter([
      {
        path: "/",
        element: <Layout />,
        children: [{ index: true, element: <div>Page body</div> }]
      }
    ]);

    render(<RouterProvider router={router} />);

    fireEvent.click(screen.getByRole("button", { name: "nav.information" }));

    expect(screen.getByRole("link", { name: "nav.releaseNotes" })).toHaveAttribute("href", "/release-notes");
    expect(screen.getByRole("link", { name: "nav.about" })).toHaveAttribute("href", "/about");
    expect(screen.getByRole("link", { name: "nav.userGuide" })).toHaveAttribute("href", "/docs/getting-started");
  });
});
