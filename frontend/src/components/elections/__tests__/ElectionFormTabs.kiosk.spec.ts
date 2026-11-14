import { describe, expect, it } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
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

  it("keeps the paper defaults when kiosk is enabled on a blank method list", async () => {
    const { wrapper, model } = mountTabs("");

    const votingTab = wrapper
      .findAll(".el-tabs__item")
      .find((tab) => tab.text().includes("Voting Methods"));
    expect(votingTab).toBeTruthy();
    await votingTab!.trigger("click");

    const switchEl = wrapper.get('[data-testid="election-kiosk-toggle"]');
    await switchEl.trigger("click");

    expect(model.votingMethods).toBe("PMDK");
    expect(model.useOnlineVoting).toBe(true);
  });

  it("shows in person, dropped off, and mailed as always on when the stored methods omit them", async () => {
    const { wrapper, model } = mountTabs("K");

    const votingTab = wrapper
      .findAll(".el-tabs__item")
      .find((tab) => tab.text().includes("Voting Methods"));
    expect(votingTab).toBeTruthy();
    await votingTab!.trigger("click");

    expect(wrapper.text()).toContain(
      "In person, dropped off, and mailed are always available and cannot be turned off.",
    );

    for (const testId of [
      "always-on-in-person",
      "always-on-dropped-off",
      "always-on-mailed",
    ]) {
      const toggle = wrapper.get(`[data-testid="${testId}"]`);
      expect(toggle.classes()).toContain("is-disabled");
      expect(toggle.classes()).toContain("is-checked");
      await toggle.trigger("click");
    }

    expect(model.votingMethods).toBe("K");
    expect(
      wrapper.get('[data-testid="election-kiosk-toggle"]').classes(),
    ).toContain("is-checked");
  });

  it("exposes Can Add People on the advanced tab for guest Name not in the List", async () => {
    const { wrapper, model } = mountTabs("IP");

    const advancedTab = wrapper
      .findAll(".el-tabs__item")
      .find((tab) => tab.text().includes("Advanced"));
    expect(advancedTab).toBeTruthy();
    await advancedTab!.trigger("click");

    expect(wrapper.text()).toContain("Can Add People?");
    const switchEl = wrapper.get(
      '[data-testid="guest-tellers-can-add-people"]',
    );
    await switchEl.trigger("click");
    expect(model.guestTellersCanAddPeople).toBe(true);
  });
});

describe("ElectionFormTabs list-mode shortfall", () => {
  async function openOnlineTab(
    model: UpdateElectionDto,
    eligibleReceiverCount: number | null,
  ) {
    const wrapper = mount(ElectionFormTabs, {
      props: {
        modelValue: model,
        availableElections: [],
        forceBasicTab: true,
        eligibleReceiverCount,
      },
      global: { plugins: [i18n, ElementPlus] },
    });
    const onlineTab = wrapper
      .findAll(".el-tabs__item")
      .find((tab) => tab.text().includes("Online Voting"));
    expect(onlineTab).toBeTruthy();
    await onlineTab!.trigger("click");
    await flushPromises();
    return wrapper;
  }

  it("warns when list mode has fewer people than Number to Elect", async () => {
    const model = reactive<UpdateElectionDto>({
      name: "Setup",
      useOnlineVoting: true,
      onlineSelectionProcess: "A",
      numberToElect: 9,
    });

    const wrapper = await openOnlineTab(model, 8);
    const alert = wrapper.get("[data-testid='online-list-shortfall']");
    expect(alert.text()).toContain("Only 8 people can be voted for");
    expect(alert.text()).toContain("elects 9");

    model.onlineSelectionProcess = "B";
    await flushPromises();
    expect(wrapper.find("[data-testid='online-list-shortfall']").exists()).toBe(
      false,
    );
  });

  it("stays quiet when typed names are allowed or the list is long enough", async () => {
    const model = reactive<UpdateElectionDto>({
      name: "Setup",
      useOnlineVoting: true,
      onlineSelectionProcess: "C",
      numberToElect: 9,
    });

    const both = await openOnlineTab(model, 1);
    expect(both.find("[data-testid='online-list-shortfall']").exists()).toBe(
      false,
    );

    model.onlineSelectionProcess = "A";
    model.numberToElect = 1;
    const enough = await openOnlineTab(model, 1);
    expect(enough.find("[data-testid='online-list-shortfall']").exists()).toBe(
      false,
    );
  });
});
