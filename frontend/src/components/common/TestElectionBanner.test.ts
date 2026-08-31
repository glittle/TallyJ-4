import { createTestingPinia } from "@pinia/testing";
import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { createRouter, createWebHistory } from "vue-router";
import { useElectionStore } from "@/stores/electionStore";
import type { ElectionDto } from "@/types";
import { i18n } from "@/test/setup";
import TestElectionBanner from "./TestElectionBanner.vue";

const TEST_GUID = "election-test-guid";

function testElection(overrides: Partial<ElectionDto> = {}): ElectionDto {
  return {
    electionGuid: TEST_GUID,
    name: "Copy of Live Election",
    electionStage: "SettingUp",
    showAsTest: true,
    ...overrides,
  };
}

async function mountBanner(options: {
  path: string;
  currentElection: ElectionDto | null;
}) {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      {
        path: "/dashboard",
        name: "Dashboard",
        component: { template: "<div />" },
      },
      {
        path: "/profile",
        name: "Profile",
        component: { template: "<div />" },
      },
      {
        path: "/elections/:id/frontdesk",
        name: "FrontDesk",
        component: { template: "<div />" },
      },
      {
        path: "/elections/:id",
        name: "Election",
        component: { template: "<div />" },
      },
    ],
  });

  await router.push(options.path);
  await router.isReady();

  const pinia = createTestingPinia({ stubActions: true });
  const electionStore = useElectionStore();
  electionStore.currentElection = options.currentElection;

  return mount(TestElectionBanner, {
    global: {
      plugins: [pinia, router, i18n],
      stubs: {
        ElIcon: { template: "<span class='el-icon-stub'><slot /></span>" },
        WarningFilled: { template: "<span />" },
      },
    },
  });
}

describe("TestElectionBanner", () => {
  it("shows the Test Election banner when the current election has showAsTest true", async () => {
    const wrapper = await mountBanner({
      path: `/elections/${TEST_GUID}/frontdesk`,
      currentElection: testElection({ showAsTest: true }),
    });

    const banner = wrapper.find("[data-testid='test-election-banner']");
    expect(banner.exists()).toBe(true);
    expect(banner.text()).toContain("Test Election");
  });

  it("hides the banner when showAsTest is false", async () => {
    const wrapper = await mountBanner({
      path: `/elections/${TEST_GUID}/frontdesk`,
      currentElection: testElection({ showAsTest: false }),
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the banner when showAsTest is null", async () => {
    const wrapper = await mountBanner({
      path: `/elections/${TEST_GUID}`,
      currentElection: {
        ...testElection(),
        showAsTest: null,
      } as ElectionDto,
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the banner when showAsTest is omitted", async () => {
    const wrapper = await mountBanner({
      path: `/elections/${TEST_GUID}`,
      currentElection: testElection({ showAsTest: undefined }),
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the banner when there is no current election", async () => {
    const wrapper = await mountBanner({
      path: `/elections/${TEST_GUID}/frontdesk`,
      currentElection: null,
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the banner on Dashboard even if leftover currentElection is a test copy", async () => {
    const wrapper = await mountBanner({
      path: "/dashboard",
      currentElection: testElection({ showAsTest: true }),
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });

  it("hides the banner when the route election does not match currentElection", async () => {
    const wrapper = await mountBanner({
      path: "/elections/other-guid/frontdesk",
      currentElection: testElection({ showAsTest: true }),
    });

    expect(wrapper.find("[data-testid='test-election-banner']").exists()).toBe(
      false,
    );
  });
});
