import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import { nextTick } from "vue";
import ElementPlus from "element-plus";
import ErrorBoundary from "./ErrorBoundary.vue";
import { i18n } from "../../test/setup";

const ErrorComponent = {
  template: "<div>Error Component</div>",
  mounted() {
    throw new Error("Test error");
  },
};

const NormalComponent = {
  template: "<div>Normal Component</div>",
};

async function mountWithError() {
  const wrapper = mount(ErrorBoundary, {
    global: {
      plugins: [createTestingPinia(), i18n, ElementPlus],
    },
    slots: {
      default: ErrorComponent,
    },
  });

  await flushPromises();
  await nextTick();
  return wrapper;
}

describe("ErrorBoundary", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders slot content when no error occurs", () => {
    const wrapper = mount(ErrorBoundary, {
      global: {
        plugins: [createTestingPinia(), i18n, ElementPlus],
      },
      slots: {
        default: NormalComponent,
      },
    });

    expect(wrapper.text()).toContain("Normal Component");
    expect(wrapper.find(".error-boundary").exists()).toBe(false);
  });

  it("catches and displays error when child component throws", async () => {
    const wrapper = await mountWithError();

    expect(wrapper.find(".error-boundary").exists()).toBe(true);
    expect(wrapper.text()).toContain("Something went wrong");
  });

  it("shows error details in development mode", async () => {
    const originalDev = import.meta.env.DEV;
    (import.meta.env as any).DEV = true;

    const wrapper = await mountWithError();

    expect(wrapper.find(".error-details").exists()).toBe(true);
    expect(wrapper.text()).toContain("Test error");

    (import.meta.env as any).DEV = originalDev;
  });

  it("hides error details in production mode", async () => {
    const originalDev = import.meta.env.DEV;
    (import.meta.env as any).DEV = false;

    const wrapper = await mountWithError();

    expect(wrapper.find(".error-details").exists()).toBe(false);

    (import.meta.env as any).DEV = originalDev;
  });

  it("retries by reloading page", async () => {
    const mockReload = vi.fn();
    Object.defineProperty(window, "location", {
      value: { reload: mockReload, href: "" },
      writable: true,
      configurable: true,
    });

    const wrapper = await mountWithError();

    const retryButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Try Again"));
    expect(retryButton).toBeDefined();
    await retryButton!.trigger("click");
    expect(mockReload).toHaveBeenCalled();
  });

  it("navigates home when go home is clicked", async () => {
    const location = { reload: vi.fn(), href: "" };
    Object.defineProperty(window, "location", {
      value: location,
      writable: true,
      configurable: true,
    });

    const wrapper = await mountWithError();

    const goHomeButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Go Home"));
    expect(goHomeButton).toBeDefined();
    await goHomeButton!.trigger("click");
    expect(location.href).toBe("/");
  });
});