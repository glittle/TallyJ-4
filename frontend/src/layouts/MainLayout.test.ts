import { createTestingPinia } from "@pinia/testing";
import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createRouter, createWebHistory } from "vue-router";
import { useElectionStore } from "@/stores/electionStore";
import { i18n } from "@/test/setup";
import MainLayout from "./MainLayout.vue";

vi.mock("@/composables/useGuestTellerStageRedirect", () => ({
  useGuestTellerStageRedirect: () => {},
}));

vi.mock("@/composables/useResponsive", () => ({
  useResponsive: () => ({ isMobile: { value: false } }),
}));

const TEST_GUID = "election-test-guid";

class RecordingResizeObserver {
  static instances: RecordingResizeObserver[] = [];
  callback: ResizeObserverCallback;
  target: Element | null = null;
  disconnected = false;

  constructor(callback: ResizeObserverCallback) {
    this.callback = callback;
    RecordingResizeObserver.instances.push(this);
  }

  observe(target: Element) {
    this.target = target;
  }

  unobserve() {}

  disconnect() {
    this.disconnected = true;
  }
}

beforeEach(() => {
  RecordingResizeObserver.instances = [];
  vi.stubGlobal("ResizeObserver", RecordingResizeObserver);
});

const layoutStubs = {
  AppHeader: { template: "<div class='app-header-stub' />" },
  AppSidebar: { template: "<div class='app-sidebar-stub' />" },
  ElContainer: { template: "<div class='el-container'><slot /></div>" },
  ElAside: { template: "<aside><slot /></aside>" },
  ElHeader: { template: "<header><slot /></header>" },
  ElMain: { template: "<main><slot /></main>" },
  RouterView: { template: "<div class='router-view-stub' />" },
  ElIcon: { template: "<span><slot /></span>" },
  WarningFilled: { template: "<span />" },
};

async function mountLayout(path: string, showAsTest: boolean | null) {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      {
        path: "/dashboard",
        component: { template: "<div />" },
      },
      {
        path: "/elections/:id/frontdesk",
        component: { template: "<div />" },
      },
      {
        path: "/elections/:id/presentation",
        component: { template: "<div />" },
        meta: { projector: true },
      },
    ],
  });
  await router.push(path);
  await router.isReady();

  const pinia = createTestingPinia({ stubActions: true });
  const electionStore = useElectionStore();
  electionStore.currentElection =
    showAsTest === null
      ? null
      : {
          electionGuid: TEST_GUID,
          name: "Copy of Live Election",
          electionStage: "SettingUp",
          showAsTest,
        };

  return {
    wrapper: mount(MainLayout, {
      global: {
        plugins: [pinia, router, i18n],
        stubs: layoutStubs,
      },
    }),
    router,
  };
}

describe("MainLayout test-election banner", () => {
  it("renders the Test Election banner on an election page when showAsTest is true", async () => {
    const { wrapper } = await mountLayout(
      `/elections/${TEST_GUID}/frontdesk`,
      true,
    );

    const banner = wrapper.find("[data-testid='test-election-banner']");
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain("Test Election");
  });

  it("does not render the banner on Dashboard when leftover currentElection is a test copy", async () => {
    const { wrapper } = await mountLayout("/dashboard", true);

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the teller shell on the projector presentation", async () => {
    const { wrapper } = await mountLayout(
      `/elections/${TEST_GUID}/presentation`,
      true,
    );

    expect(wrapper.find(".app-sidebar-stub").exists()).toBe(false);
    expect(wrapper.find(".app-header-stub").exists()).toBe(false);
    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
    expect(
      wrapper.find("[data-testid='owner-restriction-banner']").exists(),
    ).toBe(false);
    expect(wrapper.classes()).toContain("projector-layout");
  });

  it("measures the header again when it returns from the projector route", async () => {
    const rect = vi
      .spyOn(HTMLElement.prototype, "getBoundingClientRect")
      .mockReturnValue({ height: 80 } as DOMRect);
    const { wrapper, router } = await mountLayout(
      `/elections/${TEST_GUID}/frontdesk`,
      false,
    );
    await flushPromises();

    const first = RecordingResizeObserver.instances.at(-1);
    expect(first?.disconnected).toBe(false);
    expect(first?.target).toBe(wrapper.get("header").element);
    expect(wrapper.attributes("style")).toContain("--app-header-offset: 80px");

    await router.push(`/elections/${TEST_GUID}/presentation`);
    await flushPromises();
    expect(first?.disconnected).toBe(true);
    expect(wrapper.attributes("style") ?? "").not.toContain(
      "--app-header-offset",
    );

    rect.mockReturnValue({ height: 64.2 } as DOMRect);
    await router.push(`/elections/${TEST_GUID}/frontdesk`);
    await flushPromises();

    const second = RecordingResizeObserver.instances.at(-1);
    expect(second).not.toBe(first);
    expect(second?.disconnected).toBe(false);
    expect(second?.target).toBe(wrapper.get("header").element);
    expect(wrapper.attributes("style")).toContain("--app-header-offset: 65px");

    wrapper.unmount();
    expect(second?.disconnected).toBe(true);
    rect.mockRestore();
  });
});
