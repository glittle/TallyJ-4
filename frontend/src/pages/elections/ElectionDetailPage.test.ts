import { createTestingPinia } from "@pinia/testing";
import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import { useElectionStore } from "@/stores/electionStore";
import type { ElectionDto } from "@/types";
import ElectionDetailPage from "./ElectionDetailPage.vue";

const TEST_GUID = "election-test-guid";

const isGuestTellerMock = vi.fn(() => false);

vi.mock("@/domain/guestTellerAccess", () => ({
  isGuestTeller: () => isGuestTellerMock(),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => ({ params: { id: TEST_GUID } }),
}));

const detailStubs = {
  SetupTipsCard: { template: "<div class='setup-tips-stub' />" },
  ElSkeleton: { template: "<div />" },
  ElCard: {
    template: '<div class="el-card"><slot name="header" /><slot /></div>',
  },
  ElDescriptions: { template: "<div><slot /></div>" },
  ElDescriptionsItem: { template: "<div><slot /></div>" },
  ElTag: { template: "<span><slot /></span>" },
  ElRow: { template: "<div><slot /></div>" },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElIcon: { template: "<span />" },
  ElEmpty: { template: "<div />" },
  Download: { template: "<span />" },
  Delete: { template: "<span />" },
  RefreshLeft: { template: "<span />" },
};

function testElection(overrides: Partial<ElectionDto> = {}): ElectionDto {
  return {
    electionGuid: TEST_GUID,
    name: "Practice Election",
    electionStage: "GatheringBallots",
    showAsTest: true,
    ...overrides,
  };
}

async function mountDetail(options: {
  currentElection: ElectionDto | null;
  loading?: boolean;
}) {
  const pinia = createTestingPinia({ stubActions: true });
  const electionStore = useElectionStore();
  electionStore.currentElection = options.currentElection;
  electionStore.loading = options.loading ?? false;

  const wrapper = mount(ElectionDetailPage, {
    global: {
      plugins: [pinia, i18n],
      stubs: detailStubs,
    },
  });
  await flushPromises();
  return wrapper;
}

describe("ElectionDetailPage reset control", () => {
  it("shows the reset control when the election is a test election", async () => {
    isGuestTellerMock.mockReturnValue(false);
    const wrapper = await mountDetail({
      currentElection: testElection({ showAsTest: true }),
    });

    const button = wrapper.find("[data-testid='reset-test-election']");
    expect(button.exists()).toBe(true);
    expect(button.text()).toContain("Reset test election data");
  });

  it("hides the reset control when showAsTest is false", async () => {
    isGuestTellerMock.mockReturnValue(false);
    const wrapper = await mountDetail({
      currentElection: testElection({ showAsTest: false }),
    });

    expect(wrapper.find("[data-testid='reset-test-election']").exists()).toBe(
      false,
    );
  });

  it("hides the reset control when showAsTest is unset", async () => {
    isGuestTellerMock.mockReturnValue(false);
    const wrapper = await mountDetail({
      currentElection: testElection({ showAsTest: undefined }),
    });

    expect(wrapper.find("[data-testid='reset-test-election']").exists()).toBe(
      false,
    );
  });

  it("hides the reset control for guest tellers even on a test election", async () => {
    isGuestTellerMock.mockReturnValue(true);
    const wrapper = await mountDetail({
      currentElection: testElection({ showAsTest: true }),
    });

    expect(wrapper.find("[data-testid='reset-test-election']").exists()).toBe(
      false,
    );
  });
});
