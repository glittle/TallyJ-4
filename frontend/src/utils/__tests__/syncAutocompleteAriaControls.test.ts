import { describe, expect, it } from "vitest";
import { syncAutocompleteAriaControls } from "../syncAutocompleteAriaControls";

describe("syncAutocompleteAriaControls", () => {
  it("drops aria-controls until the list exists, then restores it", () => {
    document.body.innerHTML = `
      <input id="search" aria-controls="missing-list" aria-activedescendant="missing-list-item--1" />
    `;
    const input = document.getElementById("search") as HTMLInputElement;

    syncAutocompleteAriaControls(document.body);
    expect(input.hasAttribute("aria-controls")).toBe(false);
    expect(input.dataset.listboxId).toBe("missing-list");
    expect(input.hasAttribute("aria-activedescendant")).toBe(false);

    const list = document.createElement("ul");
    list.id = "missing-list";
    document.body.append(list);
    syncAutocompleteAriaControls(document.body);
    expect(input.getAttribute("aria-controls")).toBe("missing-list");
  });
});
