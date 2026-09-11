import { mount, flushPromises } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { describe, expect, it, beforeEach, vi } from "vitest";
import ComputerCodeBadge from "../ComputerCodeBadge.vue";
import {
  resetComputerCodeCache,
  setComputerCode,
} from "@/utils/computerCodeStorage";

const electionGuid = "test-election-guid";

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: electionGuid } }),
}));

function mountBadge() {
  const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
      en: {
        ballots: {
          computerCodeBadge: "This workstation's computer code",
          computerCodeShort: "{code}",
        },
      },
    },
  });

  return mount(ComputerCodeBadge, {
    global: {
      plugins: [i18n],
      mocks: {
        $route: { params: { id: electionGuid } },
      },
      stubs: { ElIcon: true },
    },
  });
}

describe("ComputerCodeBadge", () => {
  beforeEach(() => {
    localStorage.clear();
    resetComputerCodeCache();
  });

  it("is display-only with no editable input", () => {
    setComputerCode(electionGuid, "B");

    const wrapper = mountBadge();

    expect(wrapper.find("input").exists()).toBe(false);
    expect(wrapper.find("button").exists()).toBe(false);
    expect(wrapper.find(".computer-code-badge").exists()).toBe(true);
    expect(wrapper.text()).toContain("B");
  });

  it("hides until a computer code is assigned", async () => {
    const wrapper = mountBadge();

    expect(wrapper.find(".computer-code-badge").exists()).toBe(false);
    expect(wrapper.text().trim()).toBe("");

    setComputerCode(electionGuid, "AA");
    await flushPromises();

    expect(wrapper.find(".computer-code-badge").exists()).toBe(true);
    expect(wrapper.text()).toContain("AA");
  });
});
