import { describe, expect, it } from "vitest";
import { router } from "../router";

describe("unknown routes", () => {
  it("sends unknown addresses to the not-found page", () => {
    expect(router.resolve("/elections/abc/does-not-exist").name).toBe(
      "not-found",
    );
    expect(router.resolve("/elections/abc/reports").name).toBe("not-found");
    expect(router.resolve("/elections/abc/audit-logs").name).toBe("not-found");
    expect(router.resolve("/super-admin/audit-logs").name).toBe("not-found");
    expect(router.resolve("/no-such-page").name).toBe("not-found");
  });

  it("keeps known pages on their own routes", () => {
    expect(router.resolve("/").name).toBe("landing");
    expect(router.resolve("/login").name).not.toBe("not-found");
    expect(
      router.resolve("/dashboard").matched.some((r) => r.path === "/dashboard"),
    ).toBe(true);
    expect(
      router
        .resolve("/audit-logs")
        .matched.some((r) => r.path === "/audit-logs"),
    ).toBe(true);
    expect(
      router
        .resolve("/elections/abc/tie-management")
        .matched.some((r) => r.path === "/elections/:id/tie-management"),
    ).toBe(true);
  });
});
