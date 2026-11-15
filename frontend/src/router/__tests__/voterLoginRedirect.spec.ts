import { flushPromises } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { router } from "../router";

describe("officer login voter mode", () => {
  afterEach(async () => {
    await router.push("/");
    await flushPromises();
  });

  it("sends /login?mode=voter to the voter sign-in page", async () => {
    await router.push({ path: "/login", query: { mode: "voter" } });
    await flushPromises();

    expect(router.currentRoute.value.name).toBe("voter-auth");
    expect(router.currentRoute.value.path).toBe("/voter-auth");
    expect(router.currentRoute.value.query.mode).toBeUndefined();
  });

  it("keeps officer mode on the officer login page", async () => {
    await router.push({ path: "/login", query: { mode: "officer" } });
    await flushPromises();

    expect(router.currentRoute.value.path).toBe("/login");
    expect(router.currentRoute.value.query.mode).toBe("officer");
  });
});
