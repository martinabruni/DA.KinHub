import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PageScaffold } from "./PageScaffold";

vi.mock("react-i18next", () => ({
  useTranslation: () => ({ t: (key: string) => key })
}));

vi.mock("./PageHelpAccordion", () => ({
  routeDefinition: (id: string) => ({ id, titleKey: `${id}.title` })
}));

describe("PageScaffold", () => {
  it("renders the localized title and page content without the inline help accordion", () => {
    render(<PageScaffold routeId="home"><div>Page body</div></PageScaffold>);

    expect(screen.getByRole("heading", { level: 1, name: "home.title" })).toBeInTheDocument();
    expect(screen.getByText("Page body")).toBeInTheDocument();
    expect(screen.queryByText(/Apri la guida completa|Open the full guide/)).toBeNull();
  });
});
