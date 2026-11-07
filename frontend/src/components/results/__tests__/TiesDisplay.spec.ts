import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { TieInfoDto } from "@/types";
import TiesDisplay from "../TiesDisplay.vue";

function tie(overrides: Partial<TieInfoDto> = {}): TieInfoDto {
  return {
    tieBreakGroup: 1,
    voteCount: 2,
    tieBreakRequired: false,
    isResolved: false,
    section: "E",
    personNames: ["Cole, Sam", "Best, Pat"],
    ...overrides,
  };
}

const stubs = {
  ElCard: { template: "<div class='card'><slot name='header' /><slot /></div>" },
  ElTag: { template: "<span class='tag'><slot /></span>" },
  ElAlert: {
    props: ["title"],
    template: "<div class='alert'>{{ title }}</div>",
  },
  ElEmpty: { template: "<div />" },
};

describe("TiesDisplay", () => {
  it("marks a decided runoff Resolved and does not call it Tie-break Required", () => {
    const wrapper = mount(TiesDisplay, {
      props: { ties: [tie({ isResolved: true })] },
      global: { plugins: [i18n], stubs },
    });

    expect(wrapper.text()).toContain("Resolved");
    expect(wrapper.text()).not.toContain("Tie-break Required");
    expect(wrapper.text()).toContain("Cole, Sam");
  });

  it("marks an open runoff Tie-break Required", () => {
    const wrapper = mount(TiesDisplay, {
      props: { ties: [tie({ tieBreakRequired: true, isResolved: false })] },
      global: { plugins: [i18n], stubs },
    });

    expect(wrapper.text()).toContain("Tie-break Required");
    expect(wrapper.text()).not.toContain("Resolved");
  });
});
