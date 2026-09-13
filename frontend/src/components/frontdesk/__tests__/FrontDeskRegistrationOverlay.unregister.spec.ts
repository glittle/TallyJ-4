import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import type { FrontDeskVoterDto } from "@/types/FrontDesk";
import FrontDeskRegistrationOverlay from "../FrontDeskRegistrationOverlay.vue";
import { i18n } from "@/test/setup";

vi.mock("@element-plus/icons-vue", () => ({
  Check: { template: "<span />" },
  Close: { template: "<span />" },
}));

function voter(overrides: Partial<FrontDeskVoterDto> = {}): FrontDeskVoterDto {
  return {
    personGuid: "p-1",
    fullName: "Lee, Pat",
    isCheckedIn: true,
    ...overrides,
  };
}

const stubs = {
  ElAlert: { template: "<div />" },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElTag: { template: "<span><slot /></span>" },
  ElIcon: { template: "<span />" },
  ElTimeline: { template: "<div />" },
  ElTimelineItem: { template: "<div />" },
};

function mountOverlay(row: FrontDeskVoterDto) {
  return mount(FrontDeskRegistrationOverlay, {
    props: {
      voter: row,
      registrationHistory: [],
      registrationTypes: [{ value: "P", label: "In Person" }],
      electionFlags: [],
      hasActiveTeller: true,
      checkInInProgress: false,
      pendingVotingMethod: null,
      selectedButtonIndex: 0,
      getDialogButtonKey: () => "1",
      isDialogButtonKeyboardFocused: () => false,
      hasFlag: () => false,
      getVotingMethodLabel: (method?: string) => method ?? "",
      formatTime: () => "",
      formatTimeline: () => "",
    },
    global: { plugins: [i18n], stubs },
  });
}

describe("FrontDeskRegistrationOverlay Unregister", () => {
  it("hides Unregister for Processed online with no RegistrationTime", () => {
    const wrapper = mountOverlay(
      voter({
        onlineBallotStatus: "Processed",
      }),
    );

    expect(wrapper.find('[data-dialog-button="__unregister__"]').exists()).toBe(
      false,
    );
  });

  it("shows Unregister when RegistrationTime is set", () => {
    const wrapper = mountOverlay(
      voter({
        votingMethod: "P",
        registrationTime: "2026-09-13T12:00:00Z",
      }),
    );

    expect(wrapper.find('[data-dialog-button="__unregister__"]').exists()).toBe(
      true,
    );
  });
});
