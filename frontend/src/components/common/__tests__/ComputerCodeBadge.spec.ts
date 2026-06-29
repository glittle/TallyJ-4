import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { describe, expect, it, beforeEach, vi } from "vitest";
import ComputerCodeBadge from "../ComputerCodeBadge.vue";
import { setComputerCode } from "@/utils/computerCodeStorage";

const electionGuid = "test-election-guid";

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: electionGuid } }),
}));

describe("ComputerCodeBadge", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("is display-only with no editable input", () => {
    setComputerCode(electionGuid, "B");

    const i18n = createI18n({
      legacy: false,
      locale: "en",
      messages: {
        en: {
          ballots: {
            computerCodeBadge: "This workstation's computer code",
            computerCodeShort: "{code}",
            computerCodeUnset: "Awaiting code",
          },
        },
      },
    });

    const wrapper = mount(ComputerCodeBadge, {
      global: {
        plugins: [i18n],
        mocks: {
          $route: { params: { id: electionGuid } },
        },
        stubs: { ElIcon: true },
      },
    });

    expect(wrapper.find("input").exists()).toBe(false);
    expect(wrapper.find("button").exists()).toBe(false);
    expect(wrapper.find(".computer-code-badge").exists()).toBe(true);
    expect(wrapper.text()).toContain("B");
  });
});