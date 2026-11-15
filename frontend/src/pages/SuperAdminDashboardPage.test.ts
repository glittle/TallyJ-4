import { defineComponent, h } from "vue";
import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n, pinia, router } from "@/test/setup";
import { useAuthStore } from "@/stores/authStore";
import { superAdminService } from "@/services/superAdminService";
import type { SuperAdminElection } from "@/services/superAdminService";
import SuperAdminDashboardPage from "./SuperAdminDashboardPage.vue";

vi.mock("@/services/superAdminService", () => ({
  superAdminService: {
    getSummary: vi.fn(),
    getElections: vi.fn(),
    getElectionDetail: vi.fn(),
  },
}));

const mockedService = vi.mocked(superAdminService);

const elections: SuperAdminElection[] = [
  {
    electionGuid: "stage-setup",
    name: "Setup race",
    electionStage: "SettingUp",
    voterCount: 1,
    ballotCount: 0,
    locationCount: 1,
  },
  {
    electionGuid: "stage-gather",
    name: "Gather race",
    electionStage: "GatheringBallots",
    voterCount: 2,
    ballotCount: 1,
    locationCount: 1,
  },
  {
    electionGuid: "stage-process",
    name: "Process race",
    electionStage: "ProcessingBallots",
    voterCount: 3,
    ballotCount: 2,
    locationCount: 1,
  },
  {
    electionGuid: "stage-final",
    name: "Final race",
    electionStage: "Finalized",
    voterCount: 4,
    ballotCount: 3,
    locationCount: 1,
  },
  {
    electionGuid: "stage-missing",
    name: "Missing stage",
    voterCount: 0,
    ballotCount: 0,
    locationCount: 0,
  },
];

const ElTableColumnStub = defineComponent({
  name: "ElTableColumn",
  props: {
    prop: { type: String, default: "" },
  },
  setup(props, { slots }) {
    return () =>
      h(
        "div",
        { "data-prop": props.prop || "col" },
        elections.map((row) =>
          h(
            "div",
            { key: `${row.electionGuid}-${props.prop}` },
            slots.default?.({ row }),
          ),
        ),
      );
  },
});

const stubs = {
  ElButton: { template: "<button><slot /></button>" },
  ElIcon: { template: "<i><slot /></i>" },
  ElAlert: { template: "<div />" },
  ElRow: { template: "<div><slot /></div>" },
  ElCol: { template: "<div><slot /></div>" },
  ElCard: { template: "<section><slot name='header' /><slot /></section>" },
  ElInput: { template: "<input />" },
  ElSelect: {
    props: ["placeholder"],
    emits: ["update:modelValue"],
    template: `
      <div class="select" :data-placeholder="placeholder">
        <slot />
        <button
          v-if="placeholder === 'Filter by status'"
          class="pick-gather"
          @click="$emit('update:modelValue', 'GatheringBallots')"
        >pick</button>
      </div>
    `,
  },
  ElOption: {
    props: ["label", "value"],
    template: `<span class="filter-option" :data-value="value">{{ label }}</span>`,
  },
  ElTable: {
    props: ["data"],
    emits: ["row-click"],
    template: `
      <div class="el-table">
        <button
          v-for="row in data || []"
          :key="row.electionGuid || row.email"
          class="open-row"
          @click="$emit('row-click', row)"
        >{{ row.name }}</button>
        <slot />
      </div>
    `,
  },
  ElTableColumn: ElTableColumnStub,
  ElPagination: { template: "<div />" },
  ElDrawer: { template: "<aside class='sa-drawer'><slot /></aside>" },
  ElSkeleton: { template: "<div />" },
  ElEmpty: { template: "<div />" },
  ElDescriptions: { template: "<div><slot /></div>" },
  ElDescriptionsItem: { template: "<div class='desc'><slot /></div>" },
  ElTag: {
    props: ["type"],
    template: `<span class="el-tag" :data-type="type"><slot /></span>`,
  },
  ElProgress: { template: "<div />" },
};

describe("SuperAdminDashboardPage election stages", () => {
  beforeEach(() => {
    useAuthStore(pinia).isSuperAdmin = true;
    mockedService.getSummary.mockResolvedValue({
      totalElections: elections.length,
      openElections: 1,
      upcomingElections: 0,
      completedElections: 1,
      archivedElections: 0,
    });
    mockedService.getElections.mockResolvedValue({
      items: elections,
      totalCount: elections.length,
      page: 1,
      pageSize: 25,
      totalPages: 1,
    });
    mockedService.getElectionDetail.mockImplementation(async (guid: string) => {
      const row = elections.find((item) => item.electionGuid === guid);
      return {
        electionGuid: guid,
        name: row?.name ?? "",
        electionStage: row?.electionStage,
        voterCount: row?.voterCount ?? 0,
        ballotCount: row?.ballotCount ?? 0,
        locationCount: row?.locationCount ?? 0,
        percentComplete: 40,
        owners: [],
      };
    });
  });

  async function mountPage() {
    const wrapper = mount(SuperAdminDashboardPage, {
      global: { plugins: [pinia, router, i18n], stubs },
    });
    await flushPromises();
    return wrapper;
  }

  it("offers the real stages and shows translated tags", async () => {
    const wrapper = await mountPage();
    const options = wrapper.findAll(".filter-option");
    const values = options.map((option) => option.attributes("data-value"));

    expect(values).toEqual(
      expect.arrayContaining([
        "",
        "SettingUp",
        "GatheringBallots",
        "ProcessingBallots",
        "Finalized",
      ]),
    );
    expect(values).not.toContain("Draft");
    expect(values).not.toContain("Voting");
    expect(values).not.toContain("Tallying");
    expect(wrapper.text()).toContain("All");
    expect(wrapper.text()).toContain("Setting Up");
    expect(wrapper.text()).toContain("Gathering Ballots");
    expect(wrapper.text()).toContain("Processing Ballots");
    expect(wrapper.text()).toContain("Finalized");
    expect(wrapper.text()).not.toContain("Draft");
    expect(wrapper.text()).not.toContain("Tallying");

    const tags = wrapper.findAll(".el-tag").map((tag) => ({
      type: tag.attributes("data-type"),
      text: tag.text(),
    }));
    expect(tags).toEqual(
      expect.arrayContaining([
        { type: "info", text: "Setting Up" },
        { type: "warning", text: "Gathering Ballots" },
        { type: "primary", text: "Processing Ballots" },
        { type: "success", text: "Finalized" },
        { type: "info", text: "-" },
      ]),
    );
  });

  it("filters by a real stage and shows that stage on the detail tag", async () => {
    const wrapper = await mountPage();
    mockedService.getElections.mockClear();

    await wrapper.get(".pick-gather").trigger("click");
    await new Promise((resolve) => setTimeout(resolve, 250));
    await flushPromises();

    expect(mockedService.getElections).toHaveBeenCalledWith(
      expect.objectContaining({ status: "GatheringBallots" }),
    );

    const processRow = wrapper
      .findAll(".open-row")
      .find((row) => row.text() === "Process race");
    expect(processRow).toBeTruthy();
    await processRow!.trigger("click");
    await flushPromises();

    const drawerTag = wrapper.get(".sa-drawer .el-tag");
    expect(drawerTag.attributes("data-type")).toBe("primary");
    expect(drawerTag.text()).toBe("Processing Ballots");
    expect(mockedService.getElectionDetail).toHaveBeenCalledWith(
      "stage-process",
    );
  });
});
