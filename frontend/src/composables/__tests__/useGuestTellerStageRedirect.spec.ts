import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { defineComponent, h, nextTick, ref } from "vue";
import { mount, type VueWrapper } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { useGuestTellerStageRedirect } from "../useGuestTellerStageRedirect";
import { useElectionStore } from "@/stores/electionStore";
import type { ElectionDto } from "@/types";

const mockRouterPush = vi.fn();
const routePath = ref("/elections/elec-1/frontdesk");
const routeId = ref("elec-1");

vi.mock("vue-router", () => ({
  useRoute: () => ({
    get path() {
      return routePath.value;
    },
    params: {
      get id() {
        return routeId.value;
      },
    },
  }),
  useRouter: () => ({
    push: mockRouterPush,
  }),
}));

const { isGuestTellerMock } = vi.hoisted(() => ({
  isGuestTellerMock: vi.fn(() => true),
}));

vi.mock("@/domain/guestTellerAccess", async () => {
  const actual = await vi.importActual<
    typeof import("@/domain/guestTellerAccess")
  >("@/domain/guestTellerAccess");
  return {
    ...actual,
    isGuestTeller: (...args: unknown[]) => isGuestTellerMock(...args),
  };
});

let wrapper: VueWrapper | null = null;

function mountHarness() {
  wrapper?.unmount();
  wrapper = mount(
    defineComponent({
      setup() {
        useGuestTellerStageRedirect();
        return () => h("div");
      },
    }),
  );
  return wrapper;
}

describe("useGuestTellerStageRedirect", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockRouterPush.mockReset();
    isGuestTellerMock.mockReset();
    isGuestTellerMock.mockReturnValue(true);
    routePath.value = "/elections/elec-1/frontdesk";
    routeId.value = "elec-1";
  });

  afterEach(() => {
    wrapper?.unmount();
    wrapper = null;
  });

  it("does not redirect before current election is loaded for the route", async () => {
    mountHarness();
    await nextTick();
    expect(mockRouterPush).not.toHaveBeenCalled();
  });

  it("redirects GuestTeller from Front Desk to Enter Ballots when stage becomes ProcessingBallots", async () => {
    const store = useElectionStore();
    store.currentElection = {
      electionGuid: "elec-1",
      name: "Test",
      electionStage: "GatheringBallots",
    } as ElectionDto;

    mountHarness();
    await nextTick();
    expect(mockRouterPush).not.toHaveBeenCalled();

    store.currentElection = {
      ...store.currentElection!,
      electionStage: "ProcessingBallots",
    };
    await nextTick();

    expect(mockRouterPush).toHaveBeenCalledWith("/elections/elec-1/ballots");
  });

  it("redirects GuestTeller to Front Desk when stage becomes GatheringBallots", async () => {
    routePath.value = "/elections/elec-1";
    const store = useElectionStore();
    store.currentElection = {
      electionGuid: "elec-1",
      name: "Test",
      electionStage: "SettingUp",
    } as ElectionDto;

    mountHarness();
    await nextTick();
    // SettingUp on landing is allowed — no redirect yet
    mockRouterPush.mockReset();

    store.currentElection = {
      ...store.currentElection!,
      electionStage: "GatheringBallots",
    };
    await nextTick();

    expect(mockRouterPush).toHaveBeenCalledWith("/elections/elec-1/frontdesk");
  });

  it("redirects GuestTeller to election landing when stage becomes Finalized", async () => {
    const store = useElectionStore();
    store.currentElection = {
      electionGuid: "elec-1",
      name: "Test",
      electionStage: "ProcessingBallots",
    } as ElectionDto;
    routePath.value = "/elections/elec-1/ballots";

    mountHarness();
    await nextTick();
    mockRouterPush.mockReset();

    store.currentElection = {
      ...store.currentElection!,
      electionStage: "Finalized",
    };
    await nextTick();

    expect(mockRouterPush).toHaveBeenCalledWith("/elections/elec-1");
  });

  it("does not move a FullTeller when the stage changes", async () => {
    isGuestTellerMock.mockReturnValue(false);
    routePath.value = "/elections/elec-1/people";
    const store = useElectionStore();
    store.currentElection = {
      electionGuid: "elec-1",
      name: "Test",
      electionStage: "SettingUp",
    } as ElectionDto;

    mountHarness();
    await nextTick();
    mockRouterPush.mockReset();

    store.currentElection = {
      ...store.currentElection!,
      electionStage: "GatheringBallots",
    };
    await nextTick();

    expect(mockRouterPush).not.toHaveBeenCalled();
  });

  it("does not redirect when already on the correct ProcessingBallots page", async () => {
    routePath.value = "/elections/elec-1/ballots";
    const store = useElectionStore();
    store.currentElection = {
      electionGuid: "elec-1",
      name: "Test",
      electionStage: "ProcessingBallots",
    } as ElectionDto;

    mountHarness();
    await nextTick();
    expect(mockRouterPush).not.toHaveBeenCalled();
  });
});
