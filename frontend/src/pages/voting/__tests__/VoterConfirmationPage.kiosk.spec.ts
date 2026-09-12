import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { reactive } from "vue";
import { i18n } from "@/test/setup";
import VoterConfirmationPage from "../VoterConfirmationPage.vue";

const push = vi.fn();
const routeQuery = { kiosk: "1" };
const logout = vi.fn().mockResolvedValue(undefined);

const storeState = reactive({
  voterId: "SMART",
  voterIdType: "C",
  isAuthenticated: true,
  electionInfo: { electionGuid: "e1", name: "Kiosk Election" },
  voteStatus: null,
  checkVoteStatus: vi.fn(),
  logout,
});

vi.mock("vue-router", () => ({
  useRouter: () => ({ push }),
  useRoute: () => ({ query: routeQuery }),
}));

vi.mock("@/stores/onlineVotingStore", () => ({
  useOnlineVotingStore: () => storeState,
}));

describe("VoterConfirmationPage kiosk handoff", () => {
  beforeEach(() => {
    push.mockClear();
    logout.mockClear();
    storeState.voterId = "SMART";
    storeState.isAuthenticated = true;
    routeQuery.kiosk = "1";
  });

  it("logs out on mount so the next voter does not inherit the session", async () => {
    mount(VoterConfirmationPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: { template: "<div><slot /></div>" },
          ElResult: {
            template: "<div><slot name='extra' /></div>",
          },
          ElButton: {
            template: "<button v-bind='$attrs'><slot /></button>",
          },
        },
      },
    });
    await flushPromises();

    expect(logout).toHaveBeenCalled();
    expect(storeState.checkVoteStatus).not.toHaveBeenCalled();
  });

  it("sends the next voter to the code tab after logout", async () => {
    const wrapper = mount(VoterConfirmationPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: { template: "<div><slot /></div>" },
          ElResult: {
            template: "<div><slot name='extra' /></div>",
          },
          ElButton: {
            template: "<button v-bind='$attrs'><slot /></button>",
          },
        },
      },
    });
    await flushPromises();

    const next = wrapper
      .findAll("button")
      .find((btn) => btn.text().includes("Ready for next voter"));
    expect(next).toBeTruthy();
    await next!.trigger("click");
    await flushPromises();

    expect(logout).toHaveBeenCalled();
    expect(push).toHaveBeenCalledWith({
      name: "voter-auth",
      query: { tab: "code" },
    });
  });
});
