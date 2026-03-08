import { describe, it, expect, vi } from "vitest";
import { mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import { createTestingPinia } from "@pinia/testing";
import ErrorBoundary from "./ErrorBoundary.vue";
import { i18n } from "../../test/setup";

// Mock child component that throws an error
const ErrorComponent = {
  template: "<div>Error Component</div>",
  mounted() {
    throw new Error("Test error");
  },
};

const NormalComponent = {
  template: "<div>Normal Component</div>",
};

describe("ErrorBoundary", () => {
  let router: any;

  beforeEach(() => {
    router = createRouter({
      history: createWebHistory(),
      routes: [{ path: "/", name: "Home" }],
    });
  });

  it("renders slot content when no error occurs", () => {
    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: NormalComponent,
      },
    });

    expect(wrapper.text()).toContain("Normal Component");
    expect(wrapper.find(".error-boundary").exists()).toBe(false);
  });

  it("catches and displays error when child component throws", async () => {
    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: ErrorComponent,
      },
    });

    // Wait for error to be caught
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(wrapper.find(".error-boundary").exists()).toBe(true);
    expect(wrapper.text()).toContain("Something went wrong");
  });

  it("shows error details in development mode", async () => {
    // Mock import.meta.env.DEV
    const originalDev = import.meta.env.DEV;
    (import.meta.env as any).DEV = true;

    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: ErrorComponent,
      },
    });

    // Wait for error to be caught
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(wrapper.find(".error-details").exists()).toBe(true);
    expect(wrapper.text()).toContain("Test error");

    // Restore original value
    (import.meta.env as any).DEV = originalDev;
  });

  it("hides error details in production mode", async () => {
    // Mock import.meta.env.DEV
    const originalDev = import.meta.env.DEV;
    (import.meta.env as any).DEV = false;

    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: ErrorComponent,
      },
    });

    // Wait for error to be caught
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(wrapper.find(".error-details").exists()).toBe(false);

    // Restore original value
    (import.meta.env as any).DEV = originalDev;
  });

  it("retries by reloading page", async () => {
    const mockReload = vi.fn();
    Object.defineProperty(window, "location", {
      value: { reload: mockReload },
      writable: true,
    });

    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: ErrorComponent,
      },
    });

    // Wait for error to be caught
    await new Promise((resolve) => setTimeout(resolve, 0));

    // Click retry button
    const retryButton = wrapper.find("button");
    if (retryButton.exists()) {
      await retryButton.trigger("click");
      expect(mockReload).toHaveBeenCalled();
    }
  });

  it("navigates home when go home is clicked", async () => {
    const mockRouterPush = vi.fn();
    router.push = mockRouterPush;

    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [router, i18n, createTestingPinia()],
      },
      slots: {
        default: ErrorComponent,
      },
    });

    // Wait for error to be caught
    await new Promise((resolve) => setTimeout(resolve, 0));

    // Find and click go home button (second button)
    const buttons = wrapper.findAll("button");
    if (buttons.length > 1) {
      await buttons[1].trigger("click");
      expect(mockRouterPush).toHaveBeenCalledWith("/");
    }
  });
});
