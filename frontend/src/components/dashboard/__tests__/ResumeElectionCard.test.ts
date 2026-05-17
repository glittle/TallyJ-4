import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import ResumeElectionCard from "../ResumeElectionCard.vue";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

const mockPush = vi.fn();
vi.mock("vue-router", () => ({
  useRouter: () => ({ push: mockPush }),
}));

const mockElections = vi.fn(() => []);

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    get elections() {
      return mockElections();
    },
  }),
}));

const globalStubs = {
  StageIndicator: { template: "<span class='stage-indicator' />" },
  ElIcon: { template: "<span />" },
  Setting: { template: "<span />" },
  Monitor: { template: "<span />" },
  PieChart: { template: "<span />" },
};

describe("ResumeElectionCard", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockPush.mockReset();
    mockElections.mockReturnValue([]);
  });

  it("renders nothing when there are no elections", () => {
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".resume-card").exists()).toBe(false);
  });

  it("renders the most recent election by dateOfElection descending", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "old",
        name: "Old Election",
        dateOfElection: "2020-01-01",
        voterCount: 10,
        ballotCount: 5,
        tallyStatus: "Setup",
      },
      {
        electionGuid: "new",
        name: "New Election",
        dateOfElection: "2026-04-21",
        voterCount: 20,
        ballotCount: 10,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".resume-card__name").text()).toBe("New Election");
  });

  it("renders a StageIndicator component", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "e1",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Counting",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".stage-indicator").exists()).toBe(true);
  });

  it("shows participation % when voterCount > 0", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "e1",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 50,
        ballotCount: 10,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.text()).toContain("20%");
  });

  it("shows — when voterCount is 0", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "e1",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.text()).toContain("—");
  });

  it("navigates to the election when Open is clicked", async () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "abc-123",
        name: "Nav Test",
        dateOfElection: "2026-01-01",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    await wrapper.find(".resume-card__open").trigger("click");
    expect(mockPush).toHaveBeenCalledWith("/elections/abc-123");
  });

  it("picks the election with the latest dateOfElection when there are multiple", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "a",
        name: "A",
        dateOfElection: "2025-06-01",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Setup",
      },
      {
        electionGuid: "b",
        name: "B",
        dateOfElection: "2026-04-21",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Setup",
      },
      {
        electionGuid: "c",
        name: "C",
        dateOfElection: "2024-01-15",
        voterCount: 0,
        ballotCount: 0,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(ResumeElectionCard, {
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".resume-card__name").text()).toBe("B");
  });
});
