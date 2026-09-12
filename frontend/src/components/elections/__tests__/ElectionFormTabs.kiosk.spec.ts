import { describe, expect, it } from "vitest";
import { mount } from "@vue/test-utils";
import { reactive } from "vue";
import ElementPlus from "element-plus";
import { i18n } from "@/test/setup";
import ElectionFormTabs from "../ElectionFormTabs.vue";
import type { UpdateElectionDto } from "@/types";

function mountTabs(votingMethods: string) {
  const model = reactive<UpdateElectionDto>({
    name: "Setup",
    votingMethods,
    useOnlineVoting: false,
    useCallInButton: false,
  });

  const wrapper = mount(ElectionFormTabs, {
    props: {
      modelValue: model,
      availableElections: [],
      forceBasicTab: true,
    },
    global: { plugins: [i18n, ElementPlus] },
  });

  return { wrapper, model };
}

describe("ElectionFormTabs kiosk toggle", () => {
  it("enables kiosk on the voting-methods tab and turns on online voting", async () => {
    const { wrapper, model } = mountTabs("IP,OL");

    const votingTab = wrapper
      .findAll(".el-tabs__item")
      .find((tab) => tab.text().includes("Voting Methods"));
    expect(votingTab).toBeTruthy();
    await votingTab!.trigger("click");

    const switchEl = wrapper.get('[data-testid="election-kiosk-toggle"]');
    await switchEl.trigger("click");

    expect(model.votingMethods).toBe("IP,OL,K");
    expect(model.useOnlineVoting).toBe(true);
  });
});
