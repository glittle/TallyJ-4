import { Check, Clock } from "@element-plus/icons-vue";
import { flushPromises, mount } from "@vue/test-utils";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import type { PresentationDto } from "@/types";
import PresentationViewPage from "../PresentationViewPage.vue";

const fetchPresentationData = vi.fn();

const push = vi.fn();

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "election-1" } }),
  useRouter: () => ({ push }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => ({ fetchPresentationData }),
}));

const stubs = {
  ElIcon: { template: "<i class='el-icon'><slot /></i>" },
  ElEmpty: { template: "<div class='empty' />" },
};

function presentation(status: string): PresentationDto {
  return {
    electionName: "Assembly",
    numToElect: 9,
    totalBallots: 12,
    totalVotes: 40,
    electedPeople: [],
    extraPeople: [],
    hasTies: false,
    ties: [],
    status,
  };
}

describe("PresentationViewPage status banner", () => {
  let fullscreenElement: Element | null = null;
  const mounted: Array<{ unmount: () => void }> = [];

  beforeEach(() => {
    fetchPresentationData.mockReset();
    push.mockReset();
    fullscreenElement = null;
    vi.spyOn(Date, "now").mockReturnValue(1_000_000);
    Object.defineProperty(document, "fullscreenElement", {
      configurable: true,
      get: () => fullscreenElement,
    });
    i18n.global.mergeLocaleMessage("en", {
      presentation: {
        statusComplete: "LABEL_COMPLETE",
        statusInProgress: "LABEL_IN_PROGRESS",
      },
    });
  });

  afterEach(() => {
    for (const wrapper of mounted) {
      wrapper.unmount();
    }
    mounted.length = 0;
    vi.restoreAllMocks();
    i18n.global.mergeLocaleMessage("en", {
      presentation: {
        statusComplete: "Complete",
        statusInProgress: "In Progress",
      },
    });
  });

  async function mountPage() {
    const wrapper = mount(PresentationViewPage, {
      global: { plugins: [i18n], stubs },
    });
    mounted.push(wrapper);
    await flushPromises();
    return wrapper;
  }

  it("shows the complete banner for Final", async () => {
    fetchPresentationData.mockResolvedValue(presentation("Final"));

    const wrapper = await mountPage();
    const banner = wrapper.get(".status-banner");

    expect(fetchPresentationData).toHaveBeenCalledWith("election-1");
    expect(banner.classes()).toContain("status-complete");
    expect(banner.classes()).not.toContain("status-default");
    expect(banner.get(".status-text").text()).toBe("LABEL_COMPLETE");
    expect(banner.findComponent(Check).exists()).toBe(true);
    expect(banner.findComponent(Clock).exists()).toBe(false);
  });

  it("shows the in-progress banner for In Progress", async () => {
    fetchPresentationData.mockResolvedValue(presentation("In Progress"));

    const wrapper = await mountPage();
    const banner = wrapper.get(".status-banner");

    expect(banner.classes()).toContain("status-progress");
    expect(banner.classes()).not.toContain("status-default");
    expect(banner.classes()).not.toContain("status-complete");
    expect(banner.get(".status-text").text()).toBe("LABEL_IN_PROGRESS");
    expect(banner.findComponent(Clock).exists()).toBe(true);
    expect(banner.findComponent(Check).exists()).toBe(false);
  });

  it("still matches the same banners when case or spacing differs", async () => {
    fetchPresentationData.mockResolvedValue(presentation("  FINAL  "));
    const finalBanner = (await mountPage()).get(".status-banner");
    expect(finalBanner.classes()).toContain("status-complete");
    expect(finalBanner.get(".status-text").text()).toBe("LABEL_COMPLETE");

    fetchPresentationData.mockResolvedValue(presentation("in-progress"));
    const progressBanner = (await mountPage()).get(".status-banner");
    expect(progressBanner.classes()).toContain("status-progress");
    expect(progressBanner.get(".status-text").text()).toBe("LABEL_IN_PROGRESS");
  });

  it("says the tally has not been run instead of zero ballots", async () => {
    fetchPresentationData.mockResolvedValue({
      ...presentation("In Progress"),
      totalBallots: 0,
      totalVotes: 0,
    });

    const wrapper = await mountPage();
    const text = wrapper.text();

    expect(text).toContain("The tally has not been run");
    expect(text).not.toContain("0 ballots");
    expect(text).not.toContain("0 votes");
    expect(wrapper.find(".elected-title").exists()).toBe(false);
  });

  it("shows ballot and vote counts after a tally", async () => {
    fetchPresentationData.mockResolvedValue(presentation("Final"));

    const wrapper = await mountPage();
    const text = wrapper.text();

    expect(text).toContain("12 ballots");
    expect(text).toContain("40 votes");
    expect(text).not.toContain("The tally has not been run");
  });

  it("leaves for the results page when Escape is pressed", async () => {
    fetchPresentationData.mockResolvedValue(presentation("Final"));
    const wrapper = await mountPage();

    expect(wrapper.get(".sr-only").text()).toBe(
      "Press Escape to leave the presentation",
    );

    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(push).toHaveBeenCalledWith("/elections/election-1/results");

    wrapper.unmount();
    push.mockClear();
    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(push).not.toHaveBeenCalled();
  });

  it("ignores Escape while the browser is leaving fullscreen", async () => {
    fetchPresentationData.mockResolvedValue(presentation("Final"));
    const wrapper = await mountPage();
    fullscreenElement = document.body;

    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(push).not.toHaveBeenCalled();

    fullscreenElement = null;
    document.dispatchEvent(new Event("fullscreenchange"));
    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(push).not.toHaveBeenCalled();

    vi.spyOn(Date, "now").mockReturnValue(1_000_000 + 101);
    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(push).toHaveBeenCalledWith("/elections/election-1/results");
    wrapper.unmount();
  });
});
