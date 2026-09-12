import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { defineComponent, h } from "vue";
import type { LocationDto } from "@/types";
import LocationsListPage from "../LocationsListPage.vue";

const mockLocations: LocationDto[] = [
  {
    locationGuid: "loc-hall",
    electionGuid: "elec-1",
    name: "Main Hall",
    contactInfo: "555-0100",
    locationType: "Manual",
    sortOrder: 1,
  },
  {
    locationGuid: "loc-named-online",
    electionGuid: "elec-1",
    name: "Online",
    contactInfo: "desk",
    locationType: "Manual",
    sortOrder: 2,
  },
  {
    locationGuid: "loc-true-online",
    electionGuid: "elec-1",
    name: "Hall A",
    contactInfo: "Online",
    locationType: "Online",
    sortOrder: 999,
  },
  {
    locationGuid: "loc-imported",
    electionGuid: "elec-1",
    name: "Hall B",
    contactInfo: "Imported",
    locationType: "Imported",
    sortOrder: 998,
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
      totalCount: 4,
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
        typeImported: "وارداتی",
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
        mockLocations.map((row) =>
          h(
            "div",
            {
              key: `${row.locationGuid}-${props.prop || "col"}`,
              "data-location-guid": row.locationGuid,
              "data-col": props.prop || "col",
            },
            slots.default?.({ row }),
          ),
        ),
      );
  },
});

describe("LocationsListPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockFetchLocations.mockReset().mockResolvedValue(undefined);
  });

  it("marks reserved Online and Imported rows by type, not by name", async () => {
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
                />
                <slot />
              </div>
            `,
          },
          ElTableColumn: ElTableColumnStub,
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
    expect(wrapper.text()).toContain("وارداتی");
    expect(wrapper.text()).toContain("Main Hall");

    const reservedNameTags = wrapper.findAll('[data-col="name"] .el-tag');
    expect(reservedNameTags).toHaveLength(2);
    expect(reservedNameTags.map((tag) => tag.text()).sort()).toEqual([
      "آنلاین",
      "وارداتی",
    ]);

    const onlineContact = wrapper.find(
      '[data-col="contactInfo"][data-location-guid="loc-true-online"]',
    );
    expect(onlineContact.text()).toBe("-");

    const importedContact = wrapper.find(
      '[data-col="contactInfo"][data-location-guid="loc-imported"]',
    );
    expect(importedContact.text()).toBe("-");

    const namedOnlineContact = wrapper.find(
      '[data-col="contactInfo"][data-location-guid="loc-named-online"]',
    );
    expect(namedOnlineContact.text()).toContain("desk");

    const rows = wrapper.findAll(".location-row");
    const trueOnline = rows.find(
      (row) => row.attributes("data-location-guid") === "loc-true-online",
    );
    const imported = rows.find(
      (row) => row.attributes("data-location-guid") === "loc-imported",
    );
    const namedOnline = rows.find(
      (row) => row.attributes("data-location-guid") === "loc-named-online",
    );
    expect(trueOnline?.classes()).toContain("is-reserved-location");
    expect(imported?.classes()).toContain("is-reserved-location");
    expect(namedOnline?.classes()).not.toContain("is-reserved-location");
  });
});
