import { describe, it, expect, beforeEach, vi } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import { createTestingPinia } from "@pinia/testing";
import AppSidebar from "./AppSidebar.vue";
import { i18n } from "../test/setup";
import type { ElectionDto } from "@/types";

const authState = {
  name: "Alice",
  authMethod: "Local",
  isSuperAdmin: false,
};

vi.mock("../stores/authStore", () => ({
  useAuthStore: () => authState,
}));

const ELECTION_GUID = "elec-sidebar";

function testElection(stage: ElectionDto["electionStage"]): ElectionDto {
  return {
    electionGuid: ELECTION_GUID,
    name: "Sidebar Election",
    electionStage: stage,
  } as ElectionDto;
}

async function mountSidebar(options: {
  guest: boolean;
  stage: ElectionDto["electionStage"];
}) {
  authState.name = options.guest ? "Teller" : "Alice";
  authState.authMethod = options.guest ? "AccessCode" : "Local";

  const router = createRouter({
    history: createWebHistory(),
    routes: [
      {
        path: "/elections/:id",
        name: "Election",
        component: { template: "<div />" },
      },
      {
        path: "/elections/:id/frontdesk",
        name: "FrontDesk",
        component: { template: "<div />" },
      },
      {
        path: "/elections",
        name: "Elections",
        component: { template: "<div />" },
      },
    ],
  });
  await router.push(`/elections/${ELECTION_GUID}/frontdesk`);
  await router.isReady();

  const pinia = createTestingPinia({
    stubActions: false,
    initialState: {
      election: {
        currentElection: testElection(options.stage),
      },
    },
  });

  const wrapper = mount(AppSidebar, {
    global: {
      plugins: [pinia, router, i18n],
      stubs: {
        SidebarStageHeader: {
          name: "SidebarStageHeader",
          props: ["electionGuid", "stage"],
          template:
            '<div class="mock-stage-header" :data-stage="stage" :data-guid="electionGuid" />',
        },
        StageGroupedSidebarMenu: {
          name: "StageGroupedSidebarMenu",
          props: ["electionGuid", "currentStage", "isGuestTeller"],
          template:
            '<div class="mock-stage-menu" :data-guest="isGuestTeller ? \'true\' : \'false\'" :data-stage="currentStage" />',
        },
        ElIcon: { template: "<span />" },
        ElMenu: { template: "<div><slot /></div>" },
        ElMenuItem: { template: "<div><slot /></div>" },
        ElSkeleton: { template: "<div class='skeleton' />" },
        ArrowLeft: { template: "<span />" },
        Expand: { template: "<span />" },
        Fold: { template: "<span />" },
        HomeFilled: { template: "<span />" },
        Setting: { template: "<span />" },
        User: { template: "<span />" },
      },
    },
  });
  await flushPromises();
  return wrapper;
}

describe("AppSidebar election nav", () => {
  beforeEach(() => {
    authState.name = "Alice";
    authState.authMethod = "Local";
    authState.isSuperAdmin = false;
  });

  it("hides the stage switcher from GuestTellers", async () => {
    const wrapper = await mountSidebar({
      guest: true,
      stage: "GatheringBallots",
    });

    expect(wrapper.find(".mock-stage-header").exists()).toBe(false);
    expect(wrapper.find(".back-to-elections").exists()).toBe(false);
    const menu = wrapper.find(".mock-stage-menu");
    expect(menu.exists()).toBe(true);
    expect(menu.attributes("data-guest")).toBe("true");
    expect(menu.attributes("data-stage")).toBe("GatheringBallots");
  });

  it("shows the stage switcher to FullTellers", async () => {
    const wrapper = await mountSidebar({
      guest: false,
      stage: "ProcessingBallots",
    });

    expect(wrapper.find(".mock-stage-header").exists()).toBe(true);
    expect(wrapper.find(".back-to-elections").exists()).toBe(true);
    const menu = wrapper.find(".mock-stage-menu");
    expect(menu.exists()).toBe(true);
    expect(menu.attributes("data-guest")).toBe("false");
    expect(menu.attributes("data-stage")).toBe("ProcessingBallots");
  });

  it("passes Finalized to the guest menu after the election is locked", async () => {
    const wrapper = await mountSidebar({
      guest: true,
      stage: "Finalized",
    });

    expect(wrapper.find(".mock-stage-header").exists()).toBe(false);
    expect(wrapper.find(".mock-stage-menu").exists()).toBe(true);
    expect(wrapper.find(".mock-stage-menu").attributes("data-stage")).toBe(
      "Finalized",
    );
    expect(wrapper.find(".mock-stage-menu").attributes("data-guest")).toBe(
      "true",
    );
  });
});
