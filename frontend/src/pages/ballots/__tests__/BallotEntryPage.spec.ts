import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import BallotEntryPage from "../BallotEntryPage.vue";

vi.mock("vue-router", () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
  useRoute: () => ({
    params: {
      id: "test-election-guid",
      ballotId: "test-ballot-guid",
    },
  }),
}));

vi.mock("@/components/ballots/BallotEntryPanel.vue", () => ({
  default: {
    name: "BallotEntryPanel",
    template:
      '<div class="ballot-entry-panel-mock" data-testid="ballot-entry-panel"></div>',
    props: ["electionGuid", "ballotGuid", "showMetadata"],
  },
}));

vi.mock("@/components/tellers/ActiveTellerSelector.vue", () => ({
  default: {
    name: "ActiveTellerSelector",
    template: "<div></div>",
    props: ["electionGuid"],
  },
}));

describe("BallotEntryPage", () => {
  const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: { en: {} },
  });

  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("renders BallotEntryPanel with route params", async () => {
    const wrapper = mount(BallotEntryPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: {
            template:
              '<div class="el-card"><slot name="header"></slot><slot></slot></div>',
          },
        },
      },
    });

    await flushPromises();

    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.exists()).toBe(true);
    expect(panel.props("electionGuid")).toBe("test-election-guid");
    expect(panel.props("ballotGuid")).toBe("test-ballot-guid");
  });
});
