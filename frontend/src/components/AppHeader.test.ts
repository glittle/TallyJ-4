import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";

import AppHeader from "./AppHeader.vue";
import { pinia, i18n } from "../test/setup";

const mockLogout = vi.fn();
const authState = {
  email: "test@example.com" as string | null,
  name: null as string | null,
  logout: mockLogout,
};

vi.mock("../stores/authStore", () => ({
  useAuthStore: () => authState,
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
    authState.email = "test@example.com";
    authState.name = null;
    router = createRouter({
      history: createWebHistory(),
      routes: [
        {
          path: "/dashboard",
          name: "Dashboard",
          component: { template: "<div />" },
          meta: { titleKey: "nav.dashboard" },
        },
        {
          path: "/elections",
          name: "Elections",
          component: { template: "<div />" },
          meta: { titleKey: "nav.elections" },
        },
        {
          path: "/profile",
          name: "Profile",
          component: { template: "<div />" },
        },
        {
          path: "/elections/:id/ballots",
          name: "Ballots",
          component: { template: "<div />" },
          meta: { titleKey: "ballots.management" },
        },
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

  it("places status chips on a row after the title bar", async () => {
    await router.push("/elections/abc/ballots");
    await router.isReady();

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    expect(wrapper.find(".header-bar .page-title").exists()).toBe(true);
    expect(wrapper.find(".header-status .page-title").exists()).toBe(false);
    const html = wrapper.html();
    expect(html.indexOf("header-status")).toBeGreaterThan(
      html.indexOf("header-bar"),
    );
  });

  it("handles logout command correctly", async () => {
    const mockRouterPush = vi.fn();
    router.push = mockRouterPush;

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    await (
      wrapper.vm as unknown as {
        handleCommand: (command: string) => Promise<void>;
      }
    ).handleCommand("logout");

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

    await (
      wrapper.vm as unknown as {
        handleCommand: (command: string) => Promise<void>;
      }
    ).handleCommand("profile");

    expect(mockRouterPush).toHaveBeenCalledWith("/profile");
  });

  it("names the account button with the email when no name is set", () => {
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    const button = wrapper.get("button.user-dropdown");
    expect(button.attributes("aria-label")).toBeUndefined();
    expect(button.get(".username").text()).toBe("test@example.com");
    expect(button.text()).not.toContain("User account menu");
    expect(wrapper.get("nav.header-right").attributes("aria-label")).toBe(
      "User account menu",
    );
  });

  it("names the account button with the person's name when one is set", () => {
    authState.name = "Ada Lovelace";
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });
    const button = wrapper.get("button.user-dropdown");
    expect(button.get(".username").text()).toBe("Ada Lovelace");
    expect(button.text()).not.toContain("test@example.com");
  });

  it("keeps the account name in the accessibility tree when the narrow layout hides it", async () => {
    const { readFileSync } = await import("node:fs");
    const { dirname, resolve } = await import("node:path");
    const { fileURLToPath } = await import("node:url");
    const source = readFileSync(
      resolve(dirname(fileURLToPath(import.meta.url)), "AppHeader.vue"),
      "utf8",
    );
    const start = source.indexOf("@media (max-width: 768px)");
    const end = source.indexOf("@media (max-width: 480px)", start);
    const narrow = source.slice(start, end);
    const usernameStart = narrow.indexOf(".username");
    const block = narrow.slice(
      usernameStart,
      narrow.indexOf("}", usernameStart),
    );
    expect(usernameStart).toBeGreaterThan(-1);
    expect(block).not.toMatch(/display:\s*none/);
    expect(block).toContain("clip: rect(0, 0, 0, 0)");
  });
});
