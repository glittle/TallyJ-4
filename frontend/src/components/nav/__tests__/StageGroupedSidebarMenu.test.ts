import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import StageGroupedSidebarMenu from "../StageGroupedSidebarMenu.vue";
import { getGuestTellerRedirectPath } from "@/domain/guestTellerAccess";
import { STAGE_PAGES, STAGES } from "@/domain/electionStages";
import type { ElectionStage } from "@/domain/electionStages";

const mockRouterPush = vi.fn();
const mockRoutePath = vi.fn(() => "/elections/test-id");

vi.mock("vue-i18n", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("vue-router", () => ({
  useRoute: () => ({
    get path() {
      return mockRoutePath();
    },
  }),
  useRouter: () => ({
    push: mockRouterPush,
  }),
}));

vi.mock("@/stores/navUiStore", () => {
  const expansion: Record<string, boolean> = {};
  return {
    useNavUiStore: () => ({
      sidebarGroupExpansion: expansion,
      setGroupExpanded: vi.fn((stage: string, val: boolean) => {
        expansion[stage] = val;
      }),
      toggleGroup: vi.fn((stage: string) => {
        expansion[stage] = !expansion[stage];
      }),
    }),
  };
});

const globalStubs = {
  ElIcon: { template: "<span />" },
  Setting: { template: "<span />" },
  Monitor: { template: "<span />" },
  PieChart: { template: "<span />" },
  Document: { template: "<span />" },
  User: { template: "<span />" },
  UserFilled: { template: "<span />" },
  Location: { template: "<span />" },
  Tickets: { template: "<span />" },
  DataAnalysis: { template: "<span />" },
  Files: { template: "<span />" },
};

function mountMenu(props: {
  electionGuid?: string;
  currentStage?: ElectionStage;
  isGuestTeller?: boolean;
}) {
  return mount(StageGroupedSidebarMenu, {
    props: {
      electionGuid: "test-id",
      currentStage: "SettingUp",
      isGuestTeller: false,
      ...props,
    },
    global: { stubs: globalStubs },
  });
}

describe("StageGroupedSidebarMenu", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockRouterPush.mockReset();
    mockRoutePath.mockReturnValue("/elections/test-id");
  });

  describe("admin mode (isGuestTeller=false)", () => {
    it("renders all 4 stage groups", () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "SettingUp",
      });
      const groups = wrapper.findAll(".stage-group");
      expect(groups).toHaveLength(STAGES.length);
    });

    it("marks the active stage group with is-active-stage class", () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "GatheringBallots",
      });
      const groups = wrapper.findAll(".stage-group");
      const activeGroups = groups.filter((g) =>
        g.classes().includes("is-active-stage"),
      );
      expect(activeGroups).toHaveLength(1);
      expect(activeGroups[0]!.find('[aria-current="true"]').exists()).toBe(
        true,
      );
    });

    it("active stage group header has aria-current=true", () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "ProcessingBallots",
      });
      const headers = wrapper.findAll(".stage-group__header");
      const currentHeader = headers.find(
        (h) => h.attributes("aria-current") === "true",
      );
      expect(currentHeader).toBeDefined();
    });

    it("reflects updated currentStage when prop changes", async () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "SettingUp",
      });
      let groups = wrapper.findAll(".stage-group");
      expect(groups[0]!.classes().includes("is-active-stage")).toBe(true);

      await wrapper.setProps({ currentStage: "GatheringBallots" });

      groups = wrapper.findAll(".stage-group");
      expect(groups[1]!.classes().includes("is-active-stage")).toBe(true);
      expect(groups[0]!.classes().includes("is-active-stage")).toBe(false);
    });

    it("does not render the teller page list directly (no teller branch)", () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "SettingUp",
      });
      expect(wrapper.findAll(".stage-group").length).toBeGreaterThan(0);
    });
  });

  describe("GuestTeller mode (isGuestTeller=true)", () => {
    it("does not render stage group headers", () => {
      const wrapper = mountMenu({
        isGuestTeller: true,
        currentStage: "GatheringBallots",
      });
      expect(wrapper.find(".stage-group__header").exists()).toBe(false);
      expect(wrapper.find(".stage-group").exists()).toBe(false);
    });

    it("renders only the current stage's non-admin pages", () => {
      const wrapper = mountMenu({
        isGuestTeller: true,
        currentStage: "GatheringBallots",
      });
      const pages = wrapper.findAll(".stage-group__page");
      const expectedPages = STAGE_PAGES["GatheringBallots"].filter(
        (p) => !p.adminOnly,
      );
      expect(pages).toHaveLength(expectedPages.length);
    });

    it("renders no menu pages during ProcessingBallots (ballot entry uses per-ballot URLs)", () => {
      const wrapper = mountMenu({
        isGuestTeller: true,
        currentStage: "ProcessingBallots",
      });
      const pages = wrapper.findAll(".stage-group__page");
      expect(pages).toHaveLength(0);
    });

    it("renders landing and final results during Finalized", () => {
      const wrapper = mountMenu({
        isGuestTeller: true,
        currentStage: "Finalized",
      });
      const pages = wrapper.findAll(".stage-group__page");
      expect(pages).toHaveLength(2);
    });

    it("renders no pages when SettingUp (all admin-only)", () => {
      const wrapper = mountMenu({
        isGuestTeller: true,
        currentStage: "SettingUp",
      });
      const pages = wrapper.findAll(".stage-group__page");
      const expectedPages = STAGE_PAGES["SettingUp"].filter(
        (p) => !p.adminOnly,
      );
      expect(pages).toHaveLength(expectedPages.length);
    });
  });

  describe("teller redirect guard", () => {
    it("redirects teller to first page of current stage when route is outside stage pages", () => {
      mockRoutePath.mockReturnValue("/elections/test-id/tally");

      mountMenu({
        isGuestTeller: true,
        currentStage: "GatheringBallots",
        electionGuid: "test-id",
      });

      expect(mockRouterPush).toHaveBeenCalledWith(
        getGuestTellerRedirectPath("test-id", "GatheringBallots"),
      );
    });

    it("does not redirect teller when already on a valid stage page", () => {
      const tellerPages = STAGE_PAGES["GatheringBallots"].filter(
        (p) => !p.adminOnly,
      );
      if (tellerPages.length > 0) {
        mockRoutePath.mockReturnValue(tellerPages[0]!.routePath("test-id"));
        mountMenu({
          isGuestTeller: true,
          currentStage: "GatheringBallots",
          electionGuid: "test-id",
        });
        expect(mockRouterPush).not.toHaveBeenCalled();
      }
    });

    it("does not redirect admins regardless of route", () => {
      mockRoutePath.mockReturnValue("/elections/test-id/some-other-page");
      mountMenu({ isGuestTeller: false, currentStage: "GatheringBallots" });
      expect(mockRouterPush).not.toHaveBeenCalled();
    });
  });

  describe("page navigation", () => {
    it("calls router.push when a page item is clicked (admin)", async () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "SettingUp",
        electionGuid: "test-id",
      });
      const pages = wrapper.findAll(".stage-group__page");
      if (pages.length > 0) {
        await pages[0]!.trigger("click");
        expect(mockRouterPush).toHaveBeenCalledWith(
          STAGE_PAGES["SettingUp"][0]!.routePath("test-id"),
        );
      }
    });

    it("emits close-mobile-sidebar when a page is clicked", async () => {
      const wrapper = mountMenu({
        isGuestTeller: false,
        currentStage: "SettingUp",
        electionGuid: "test-id",
      });
      const pages = wrapper.findAll(".stage-group__page");
      if (pages.length > 0) {
        await pages[0]!.trigger("click");
        expect(wrapper.emitted("close-mobile-sidebar")).toBeTruthy();
      }
    });
  });
});
