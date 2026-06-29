import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";

import AppHeader from "./AppHeader.vue";
import { pinia, i18n } from "../test/setup";

const mockLogout = vi.fn();

vi.mock("../stores/authStore", () => ({
  useAuthStore: () => ({
    email: "test@example.com",
    name: null,
    logout: mockLogout,
  }),
}));

vi.mock("../stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: null,
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showInfoMessage: vi.fn(),
  }),
}));

describe("AppHeader", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    vi.clearAllMocks();
    router = createRouter({
      history: createWebHistory(),
      routes: [
        {
          path: "/dashboard",
          name: "Dashboard",
          meta: { titleKey: "nav.dashboard" },
        },
        {
          path: "/elections",
          name: "Elections",
          meta: { titleKey: "nav.elections" },
        },
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
      },
    });

    await wrapper.vm.handleCommand("logout");

    expect(mockLogout).toHaveBeenCalled();
    expect(mockRouterPush).toHaveBeenCalledWith("/");
  });

  it("handles profile command correctly", async () => {
    const mockRouterPush = vi.fn();
    router.push = mockRouterPush;

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    await wrapper.vm.handleCommand("profile");

    expect(mockRouterPush).toHaveBeenCalledWith("/profile");
  });
});