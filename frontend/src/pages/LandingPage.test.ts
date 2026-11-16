import { describe, expect, it } from "vitest";
import { mount } from "@vue/test-utils";
import LandingPage from "./LandingPage.vue";
import { i18n, router } from "@/test/setup";

describe("LandingPage", () => {
  it("styles the status document like the other landing link and reads the catalog", () => {
    const wrapper = mount(LandingPage, {
      global: {
        plugins: [router, i18n],
        stubs: { ElIcon: true },
      },
    });

    const status = wrapper.get(".statusDocLink a");
    expect(status.classes()).toContain("secondary-link");
    expect(status.text()).toBe("Status & Feedback Document V4 (English only)");
    expect(wrapper.findAll("a.secondary-link").length).toBeGreaterThan(1);

    wrapper.unmount();
  });
});
