/** Keep aria-controls only while the suggestion list is in the document. */
export function syncAutocompleteAriaControls(root: ParentNode): void {
  const inputs = root.querySelectorAll<HTMLInputElement>(
    "input[aria-controls], input[data-listbox-id]",
  );
  inputs.forEach((input) => {
    const current = input.getAttribute("aria-controls");
    const listboxId = input.dataset.listboxId || current;
    if (!listboxId) {
      return;
    }
    input.dataset.listboxId = listboxId;
    if (document.getElementById(listboxId)) {
      if (input.getAttribute("aria-controls") !== listboxId) {
        input.setAttribute("aria-controls", listboxId);
      }
    } else if (input.hasAttribute("aria-controls")) {
      input.removeAttribute("aria-controls");
    }

    const activeId = input.getAttribute("aria-activedescendant");
    if (activeId && !document.getElementById(activeId)) {
      input.removeAttribute("aria-activedescendant");
    }
  });
}
