import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import LoginPage from "./LoginPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";

describe("LoginPage", () => {
  it("renders login title", () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    expect(wrapper.text()).toContain("Login");
  });

  it("keeps password login and steers new tellers to Google", () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    expect(wrapper.text()).toContain(
      "New teller? Sign in with Google to create an account",
    );
    expect(wrapper.text()).not.toContain("Don't have an account? Register");
    expect(wrapper.find('a[href="/register"]').exists()).toBe(false);
  });
});
