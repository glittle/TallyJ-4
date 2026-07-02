import { describe, it, expect, vi } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import { createRouter, createWebHistory } from "vue-router";
import DashboardPage from "./DashboardPage.vue";
import { i18n } from "../test/setup";

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

const dashboardStubs = {
  ElCard: {
    template: '<div class="el-card"><slot name="header" /><slot /></div>',
  },
  ElRow: { template: "<div><slot /></div>" },
  ElCol: { template: "<div><slot /></div>" },
  ElButton: { template: "<button><slot /></button>" },
  ElIcon: { template: "<span />" },
  ElInput: { template: "<input />" },
  ElSelect: { template: "<select><slot /></select>" },
  ElOption: { template: "<option />" },
  ElTable: { template: "<table><slot /></table>" },
  ElTableColumn: { template: "<th><slot /></th>" },
  ElPagination: { template: "<div />" },
  ElEmpty: { template: "<div />" },
  ElTag: { template: "<span><slot /></span>" },
  ElDatePicker: { template: "<input />" },
  ResumeElectionCard: { template: "<div />" },
  SetupTipsCard: { template: "<div />" },
};

const router = createRouter({
  history: createWebHistory(),
  routes: [],
});

async function mountDashboard() {
  const wrapper = mount(DashboardPage, {
    global: {
      plugins: [createTestingPinia({ stubActions: false }), i18n, router],
      stubs: dashboardStubs,
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

  it("displays total elections label in the header", async () => {
    const wrapper = await mountDashboard();
    expect(wrapper.find(".stat-label").text()).toContain("Total Elections");
  });
});
