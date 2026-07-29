import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { Button, ButtonLink, Tabs, TextField } from "./core";

describe("ui/core", () => {
  it("defaults button type to button", () => {
    render(<Button>Save</Button>);

    expect(screen.getByRole("button", { name: "Save" })).toHaveAttribute("type", "button");
  });

  it("renders a navigation action as a link without nesting a button", () => {
    render(<MemoryRouter><ButtonLink to="/settings">Settings</ButtonLink></MemoryRouter>);

    expect(screen.getByRole("link", { name: "Settings" })).toHaveAttribute("href", "/settings");
    expect(screen.queryByRole("button", { name: "Settings" })).not.toBeInTheDocument();
  });

  it("keeps helper and error descriptions stable on text fields", () => {
    render(<TextField label="Family name" helper="Choose a shared label" error="Name is required" value="" onChange={() => undefined} />);

    const input = screen.getByRole("textbox");
    const describedBy = input.getAttribute("aria-describedby");

    expect(describedBy).toBeTruthy();
    expect(describedBy?.split(" ")).toHaveLength(2);
    expect(screen.getByText("Choose a shared label")).toHaveAttribute("id", describedBy?.split(" ")[0]);
    expect(screen.getByText("Name is required")).toHaveAttribute("id", describedBy?.split(" ")[1]);
  });

  it("supports keyboard navigation in tabs with roving tabindex", () => {
    const onValueChange = vi.fn();

    render(<Tabs label="Sections" value="first" onValueChange={onValueChange} items={[{ value: "first", label: "First" }, { value: "second", label: "Second" }]} />);

    const firstTab = screen.getByRole("tab", { name: "First" });
    fireEvent.keyDown(firstTab.parentElement!, { key: "ArrowRight" });

    expect(onValueChange).toHaveBeenCalledWith("second");
    expect(firstTab).toHaveAttribute("tabindex", "0");
  });
});
