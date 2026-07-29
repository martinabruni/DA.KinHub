import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FloatingBarCarousel, FloatingBarPage } from "./FloatingBars";

describe("FloatingBarCarousel", () => {
  it("shows one page at a time and switches with keyboard", () => {
    render(
      <FloatingBarCarousel routeKey="/" label="Bars" pageLabel={(current, total) => `Bar ${current} of ${total}`}>
        <FloatingBarPage label="Global"><span>Global bar</span></FloatingBarPage>
        <FloatingBarPage label="Context"><span>Context bar</span></FloatingBarPage>
      </FloatingBarCarousel>
    );

    const carousel = screen.getByLabelText("Bars");
    expect(screen.getByText("Global bar")).toBeInTheDocument();
    expect(screen.queryByText("Context bar")).not.toBeInTheDocument();

    fireEvent.keyDown(carousel, { key: "ArrowRight" });

    expect(screen.getByText("Context bar")).toBeInTheDocument();
    expect(screen.queryByText("Global bar")).not.toBeInTheDocument();
  });

  it("resets to the default page when the route changes", () => {
    const view = render(
      <FloatingBarCarousel routeKey="/kinlist" defaultIndex={1} label="Bars" pageLabel={(current, total) => `Bar ${current} of ${total}`}>
        <FloatingBarPage label="Global"><span>Global bar</span></FloatingBarPage>
        <FloatingBarPage label="Context"><span>Context bar</span></FloatingBarPage>
      </FloatingBarCarousel>
    );

    expect(screen.getByText("Context bar")).toBeInTheDocument();

    view.rerender(
      <FloatingBarCarousel routeKey="/settings" defaultIndex={0} label="Bars" pageLabel={(current, total) => `Bar ${current} of ${total}`}>
        <FloatingBarPage label="Global"><span>Global bar</span></FloatingBarPage>
        <FloatingBarPage label="Context"><span>Context bar</span></FloatingBarPage>
      </FloatingBarCarousel>
    );

    expect(screen.getByText("Global bar")).toBeInTheDocument();
    expect(screen.queryByText("Context bar")).not.toBeInTheDocument();
  });
});
