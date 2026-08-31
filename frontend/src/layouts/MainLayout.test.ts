import { createTestingPinia } from "@pinia/testing";
import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
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

  return mount(MainLayout, {
    global: {
      plugins: [pinia, router, i18n],
      stubs: layoutStubs,
    },
  });
}

describe("MainLayout test-election banner", () => {
  it("renders the Test Election banner on an election page when showAsTest is true", async () => {
    const wrapper = await mountLayout(
      `/elections/${TEST_GUID}/frontdesk`,
      true,
    );

    const banner = wrapper.find("[data-testid='test-election-banner']");
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain("Test Election");
  });

  it("does not render the banner on Dashboard when leftover currentElection is a test copy", async () => {
    const wrapper = await mountLayout("/dashboard", true);

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });
});
