import { render, screen } from "@testing-library/react";
import { ListChecks } from "lucide-react";
import { describe, expect, it } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { ComingSoonServiceCard, FeatureCard, KinListItem } from "./KinPatterns";

describe("KinPatterns", () => {
  it("renders service navigation patterns as links with accessible names", () => {
    render(
      <MemoryRouter>
        <FeatureCard to="/kinlist" icon={ListChecks} title="KinList" description="Shared list" />
      </MemoryRouter>
    );

    expect(screen.getByRole("link", { name: /KinList Shared list/i })).toHaveAttribute("href", "/kinlist");
  });

  it("renders coming soon cards as non-links", () => {
    render(<ComingSoonServiceCard title="KinRecipe" description="Future family service" badgeLabel="Soon" />);

    expect(screen.queryByRole("link", { name: /KinRecipe/i })).not.toBeInTheDocument();
    expect(screen.getByText("Soon")).toBeInTheDocument();
  });

  it("builds KinList rows on shared interactive controls", () => {
    render(<KinListItem title="Bread" detail="Added today" completed selected onToggle={() => undefined} onSelect={() => undefined} />);

    const buttons = screen.getAllByRole("button");
    expect(buttons).toHaveLength(2);
    expect(buttons[0]).toHaveAttribute("aria-pressed", "true");
    expect(buttons[1]).toHaveAttribute("aria-pressed", "true");
  });
});
