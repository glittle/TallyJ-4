import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import BallotViewFilterSelect from "../BallotViewFilterSelect.vue";
import { computerFilterValue } from "@/utils/ballotViewFilter";

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      ballots: {
        allBallots: "All ballots",
        allAtLocation: "All at {name}",
        viewFilterLabel: "Ballots to show",
        viewFilterPlaceholder: "Search",
      },
    },
  },
});

describe("BallotViewFilterSelect", () => {
  it("shows Location / Computer when closed for a computer filter", () => {
    const wrapper = mount(BallotViewFilterSelect, {
      props: {
        modelValue: computerFilterValue("loc-1", "AA"),
        locations: [
          {
            locationGuid: "loc-1",
            electionGuid: "e1",
            name: "Main Hall",
            sortOrder: 1,
          },
        ],
        ballots: [
          {
            ballotGuid: "b1",
            ballotCode: "A1",
            locationGuid: "loc-1",
            locationName: "Main Hall",
            ballotNumAtComputer: 1,
            computerCode: "AA",
            statusCode: "Ok",
            voteCount: 1,
          },
        ],
        computersByLocation: {},
      },
      global: {
        plugins: [i18n],
        stubs: {
          ElSelect: {
            template: '<div class="el-select"><slot name="label" /></div>',
          },
          ElOption: true,
          ElOptionGroup: true,
        },
      },
    });

    expect(wrapper.text()).toContain("Main Hall / AA");
    expect(wrapper.text()).not.toContain(computerFilterValue("loc-1", "AA"));
  });
});
