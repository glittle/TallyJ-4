import { createTestingPinia } from "@pinia/testing";
import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { h } from "vue";
import { createRouter, createWebHistory } from "vue-router";
import { i18n } from "../test/setup";
import DashboardPage from "./DashboardPage.vue";

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

vi.mock("@/utils/activeElectionHubStorage", () => ({
  getActiveElectionHubGuid: vi.fn(() => null),
}));

const seededElection = {
  electionGuid: "abc-guid",
  name: "Live Election",
  dateOfElection: "2026-01-01",
  voterCount: 10,
  ballotCount: 5,
  showAsTest: false,
};

const dashboardStubs = {
  ElCard: {
    template: '<div class="el-card"><slot name="header" /><slot /></div>',
  },
  ElRow: { template: "<div><slot /></div>" },
  ElCol: { template: "<div><slot /></div>" },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElIcon: { template: "<span />" },
  ElInput: { template: "<input />" },
  ElSelect: { template: "<select><slot /></select>" },
  ElOption: { template: "<option />" },
  ElTable: { template: "<table><slot /></table>" },
  ElTableColumn: {
    setup(_, { slots }) {
      return () => h("th", slots.default?.({ row: seededElection }));
    },
  },
  "el-table-column": {
    setup(_, { slots }) {
      return () => h("th", slots.default?.({ row: seededElection }));
    },
  },
  ElPagination: { template: "<div />" },
  ElEmpty: { template: "<div />" },
  ElTag: { template: "<span><slot /></span>" },
  ElDatePicker: { template: "<input />" },
  ElSpace: { template: "<div><slot /></div>" },
  ElSkeleton: { template: "<div />" },
  CopyDocument: { template: "<span />" },
};

const router = createRouter({
  history: createWebHistory(),
  routes: [],
});

async function mountDashboard() {
  const wrapper = mount(DashboardPage, {
    global: {
      plugins: [
        createTestingPinia({
          stubActions: true,
          initialState: {
            election: {
              elections: [seededElection],
              loading: false,
            },
          },
        }),
        i18n,
        router,
      ],
      stubs: dashboardStubs,
      directives: { loading: {} },
    },
  });
  await flushPromises();
  return wrapper;
}

describe("DashboardPage", () => {
  it("renders dashboard page", async () => {
    const wrapper = await mountDashboard();
    expect(wrapper.exists()).toBe(true);
  });

  it("renders a duplicate control for listed elections", async () => {
    const wrapper = await mountDashboard();
    const button = wrapper.find('button[aria-label="Duplicate as test copy"]');
    expect(button.exists()).toBe(true);
    expect(button.text()).toContain("Copy");
  });
});
