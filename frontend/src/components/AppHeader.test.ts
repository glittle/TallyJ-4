import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";

import AppHeader from "./AppHeader.vue";
import { pinia, i18n } from "../test/setup";
import { VERSION } from "./version";

// Mock the auth store
const mockLogout = vi.fn();
vi.mock("../stores/authStore", () => ({
  useAuthStore: () => ({
    email: "test@example.com",
    logout: mockLogout,
  }),
}));

describe("AppHeader", () => {
  let router: any;

  beforeEach(() => {
    router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: "/dashboard", name: "Dashboard" },
        { path: "/elections", name: "Elections" },
        { path: "/profile", name: "Profile" },
      ],
    });
  });

  it("renders properly", () => {
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    expect(wrapper.exists()).toBe(true);
  });

  it("displays the application title", () => {
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    expect(wrapper.text()).toContain("TallyJ 4");
  });

  it("displays user email in dropdown", () => {
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    expect(wrapper.text()).toContain("test@example.com");
  });

  it("shows correct page title for dashboard route", async () => {
    await router.push("/dashboard");
    await router.isReady();

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    expect(wrapper.text()).toContain("Dashboard");
  });

  it("shows correct page title for elections route", async () => {
    await router.push("/elections");
    await router.isReady();

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    expect(wrapper.text()).toContain("Elections");
  });

  it("handles logout command correctly", async () => {
    const mockRouterPush = vi.fn();
    router.push = mockRouterPush;

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
        mocks: {
          $message: { success: vi.fn() },
        },
      },
    });

    // Access the component instance and call handleCommand directly
    await wrapper.vm.handleCommand("logout");

    expect(mockLogout).toHaveBeenCalled();
    expect(mockRouterPush).toHaveBeenCalledWith("/login?mode=officer");
  });

  it("handles profile command correctly", async () => {
    const mockRouterPush = vi.fn();
    router.push = mockRouterPush;

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    // Access the component instance and call handleCommand directly
    await wrapper.vm.handleCommand("profile");

    expect(mockRouterPush).toHaveBeenCalledWith("/profile");
  });

  it("displays version tooltip on TallyJ 4 header", () => {
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    const headerH3 = wrapper.find(".header-left h3");
    expect(headerH3.exists()).toBe(true);
    expect(headerH3.attributes("title")).toContain("Version");
    expect(headerH3.attributes("title")).toContain(VERSION);
  });
});
