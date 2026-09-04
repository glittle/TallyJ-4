import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { LocationDto } from "@/types";
import LocationsListPage from "../LocationsListPage.vue";

const mockLocations: LocationDto[] = [
  {
    locationGuid: "loc-hall",
    electionGuid: "elec-1",
    name: "Main Hall",
    locationType: "Manual",
    sortOrder: 1,
  },
  {
    locationGuid: "loc-named-online",
    electionGuid: "elec-1",
    name: "Online",
    locationType: "Manual",
    sortOrder: 2,
  },
  {
    locationGuid: "loc-true-online",
    electionGuid: "elec-1",
    name: "Hall A",
    locationType: "Online",
    sortOrder: 999,
  },
];

const mockFetchLocations = vi.fn();

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "elec-1" } }),
}));

vi.mock("@/stores/locationStore", () => ({
  useLocationStore: () => ({
    loading: false,
    sortedLocations: mockLocations,
    pagination: {
      pageNumber: 1,
      pageSize: 50,
      totalCount: 3,
      totalPages: 1,
    },
    fetchLocations: mockFetchLocations,
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showErrorMessage: vi.fn(),
  }),
}));

const i18n = createI18n({
  legacy: false,
  locale: "fa",
  messages: {
    fa: {
      locations: {
        typeOnline: "آنلاین",
        onlineVotingBadge: "رای‌گیری آنلاین",
        form: {
          name: "Name",
          contactInfo: "Contact",
          coordinates: "Coordinates",
          ballots: "Ballots",
          sortOrder: "Sort",
          titleAdd: "Add",
          titleEdit: "Edit",
        },
        editDrawerTitle: "Edit {name}",
        tallyStatus: "Status",
        button: {
          addLocation: "Add Location",
        },
      },
    },
  },
});

describe("LocationsListPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockFetchLocations.mockReset().mockResolvedValue(undefined);
  });

  it("marks the true Online location by type, not by the name Online", async () => {
    const wrapper = mount(LocationsListPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: { template: "<div><slot name='header' /><slot /></div>" },
          ElTable: {
            props: ["data", "rowClassName"],
            template: `
              <div class="el-table">
                <div
                  v-for="row in data"
                  :key="row.locationGuid"
                  class="location-row"
                  :class="typeof rowClassName === 'function' ? rowClassName({ row }) : ''"
                  :data-location-guid="row.locationGuid"
                >
                  <slot />
                </div>
              </div>
            `,
          },
          ElTableColumn: {
            props: ["prop"],
            template: `
              <div>
                <div
                  v-for="row in [
                    { locationGuid: 'loc-hall', name: 'Main Hall', locationType: 'Manual' },
                    { locationGuid: 'loc-named-online', name: 'Online', locationType: 'Manual' },
                    { locationGuid: 'loc-true-online', name: 'Hall A', locationType: 'Online' },
                  ]"
                  :key="row.locationGuid + (prop || 'col')"
                >
                  <slot name="default" :row="row" />
                </div>
              </div>
            `,
          },
          ElButton: {
            template:
              '<button type="button" @click="$emit(\'click\')"><slot /></button>',
          },
          ElTag: {
            template: '<span class="el-tag"><slot /></span>',
          },
          ElIcon: true,
          ElDrawer: true,
          ElPagination: true,
        },
      },
    });
    await flushPromises();

    expect(mockFetchLocations).toHaveBeenCalledWith("elec-1", 1, 50);
    expect(wrapper.text()).toContain("آنلاین");
    expect(wrapper.text()).toContain("رای‌گیری آنلاین");
    expect(wrapper.text()).toContain("Main Hall");

    const badgeCount = wrapper
      .findAll(".el-tag")
      .filter((tag) => tag.text().includes("رای‌گیری آنلاین")).length;
    expect(badgeCount).toBe(1);

    const rows = wrapper.findAll(".location-row");
    const trueOnline = rows.find(
      (row) => row.attributes("data-location-guid") === "loc-true-online",
    );
    const namedOnline = rows.find(
      (row) => row.attributes("data-location-guid") === "loc-named-online",
    );
    expect(trueOnline?.classes()).toContain("is-online-location");
    expect(namedOnline?.classes()).not.toContain("is-online-location");
  });
});
