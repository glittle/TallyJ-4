import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { LocationDto } from "@/types";
import LocationForm from "../LocationForm.vue";

const mockUpdateLocation = vi.fn();
const mockCreateLocation = vi.fn();

vi.mock("@/stores/locationStore", () => ({
  useLocationStore: () => ({
    updateLocation: mockUpdateLocation,
    createLocation: mockCreateLocation,
    deleteLocation: vi.fn(),
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      locations: {
        typeOnline: "Online",
        form: {
          name: "Location Name",
          namePlaceholder: "Enter location name",
          nameRequired: "required",
          nameMaxLength: "too long",
          contactInfo: "Contact Info",
          contactInfoPlaceholder: "Enter contact",
          contactInfoMaxLength: "too long",
          longitude: "Longitude",
          longitudePlaceholder: "long",
          longitudeHelp: "long help",
          longitudeInvalid: "invalid",
          latitude: "Latitude",
          latitudePlaceholder: "lat",
          latitudeHelp: "lat help",
          latitudeInvalid: "invalid",
          sortOrder: "Sort Order",
          sortOrderHelp: "Used to order locations",
          sortOrderInvalid: "invalid",
          onlineSortOnlyHelp: "Only the sort order can be changed",
          save: "Save",
          create: "Create",
          cancel: "Cancel",
          delete: "Delete",
          updated: "updated",
          created: "created",
        },
      },
    },
  },
});

const onlineLocation: LocationDto = {
  locationGuid: "loc-online",
  electionGuid: "elec-1",
  name: "Hall A",
  contactInfo: "should hide",
  longitude: "1",
  latitude: "2",
  sortOrder: 999,
  locationType: "Online",
};

const paperLocation: LocationDto = {
  locationGuid: "loc-hall",
  electionGuid: "elec-1",
  name: "Main Hall",
  contactInfo: "555",
  longitude: "-122.4",
  latitude: "37.7",
  sortOrder: 1,
  locationType: "Manual",
};

function mountForm(location: LocationDto) {
  return mount(LocationForm, {
    props: {
      electionGuid: "elec-1",
      location,
      isEdit: true,
      showDelete: true,
    },
    global: {
      plugins: [i18n],
      stubs: {
        ElForm: {
          template: "<form><slot /></form>",
          methods: {
            validate(cb: (valid: boolean) => void) {
              cb(true);
            },
            resetFields() {},
          },
        },
        ElFormItem: { template: "<div><slot /></div>" },
        ElInput: true,
        ElInputNumber: true,
        ElButton: {
          template:
            '<button type="button" @click="$emit(\'click\')"><slot /></button>',
        },
      },
    },
  });
}

describe("LocationForm", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockUpdateLocation.mockReset().mockResolvedValue(undefined);
    mockCreateLocation.mockReset().mockResolvedValue(undefined);
  });

  it("locks name, contact, and coordinates for the Online location", () => {
    const wrapper = mountForm(onlineLocation);

    expect(wrapper.find('[data-testid="online-location-name"]').text()).toBe(
      "Online",
    );
    expect(wrapper.find("input").exists()).toBe(false);
    expect(wrapper.text()).not.toContain("Contact Info");
    expect(wrapper.text()).not.toContain("Longitude");
    expect(wrapper.text()).not.toContain("Latitude");
    expect(wrapper.text()).toContain("Sort Order");
    expect(wrapper.text()).toContain("Only the sort order can be changed");
    expect(wrapper.text()).not.toContain("Delete");
  });

  it("keeps name and contact editable for a paper location", () => {
    const wrapper = mountForm(paperLocation);

    expect(wrapper.find('[data-testid="online-location-name"]').exists()).toBe(
      false,
    );
    expect(wrapper.text()).toContain("Contact Info");
    expect(wrapper.text()).toContain("Longitude");
    expect(wrapper.text()).toContain("Delete");
  });

  it("posts only sortOrder when saving the Online location", async () => {
    const wrapper = mountForm(onlineLocation);
    const saveButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Save"));
    expect(saveButton).toBeTruthy();

    await saveButton!.trigger("click");
    await flushPromises();

    expect(mockUpdateLocation).toHaveBeenCalledWith("elec-1", "loc-online", {
      sortOrder: 999,
    });
  });
});
