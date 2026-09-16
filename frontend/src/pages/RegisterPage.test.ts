import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import RegisterPage from "./RegisterPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { useAuthStore } from "@/stores/authStore";

describe("RegisterPage", () => {
  it("steers new tellers to Google and does not offer email signup", () => {
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    expect(wrapper.text()).toContain("Create a teller account");
    expect(wrapper.text()).toContain("Log in with Google");
    expect(wrapper.text()).toContain(
      "Open email and password signup is not available",
    );
    expect(wrapper.find("form").exists()).toBe(false);
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
    expect(wrapper.find('input[type="email"]').exists()).toBe(false);
  });

  it("does not expose or call open register", () => {
    const authStore = useAuthStore(pinia);
    expect(authStore).not.toHaveProperty("register");

    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    expect(wrapper.find("form").exists()).toBe(false);
  });
});
