import type { FormInstance } from "element-plus";
import { describe, expect, it } from "vitest";
import { applyServerFieldErrors } from "../formServerErrors";

describe("applyServerFieldErrors", () => {
  it("sets the first server message on the matching field", () => {
    const name = { validateState: "", validateMessage: "" };
    const form = {
      getField: (prop: string) => (prop === "name" ? name : undefined),
    } as unknown as FormInstance;

    applyServerFieldErrors(form, {
      name: ["Name is required", "ignored"],
      missing: ["no field"],
    });

    expect(name.validateState).toBe("error");
    expect(name.validateMessage).toBe("Name is required");
  });

  it("does nothing when the form is missing", () => {
    expect(() =>
      applyServerFieldErrors(undefined, { name: ["Name is required"] }),
    ).not.toThrow();
  });
});
