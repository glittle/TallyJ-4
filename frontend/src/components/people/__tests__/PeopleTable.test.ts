import { mount } from "@vue/test-utils";
import { ElButton } from "element-plus";
import { describe, expect, it, vi } from "vitest";
import { h } from "vue";
import type { PersonListDto } from "../../../types";
import PeopleTable from "../PeopleTable.vue";

vi.mock("vue-i18n", async (importOriginal) => {
  const actual = await importOriginal<typeof import("vue-i18n")>();
  return {
    ...actual,
    useI18n: () => ({
      t: (key: string) => key,
    }),
  };
});

const samplePerson: PersonListDto = {
  personGuid: "p-1",
  fullName: "Afonso [Little], Pedro [Glen]",
  email: "pedro@example.com",
  phone: "555-0100",
  area: "A",
};

const TableStub = {
  name: "ElTableV2",
  props: ["columns", "data"],
  setup(props: {
    columns: Array<{
      cellRenderer?: (args: { rowData: PersonListDto }) => unknown;
    }>;
    data: PersonListDto[];
  }) {
    return () => {
      const row = props.data[0];
      return h(
        "div",
        { class: "table-stub" },
        props.columns
          .filter((col) => col.cellRenderer)
          .map((col, index) =>
            h("div", { key: index }, [col.cellRenderer!({ rowData: row })]),
          ),
      );
    };
  },
};

const AutoResizerStub = {
  name: "ElAutoResizer",
  setup(
    _props: unknown,
    {
      slots,
    }: {
      slots: {
        default?: (props: { height: number; width: number }) => unknown;
      };
    },
  ) {
    return () => h("div", slots.default?.({ height: 400, width: 800 }));
  },
};

describe("PeopleTable", () => {
  it("renders names as primary link buttons that use the link token class", () => {
    const wrapper = mount(PeopleTable, {
      props: {
        people: [samplePerson],
        loading: false,
        tableHeight: 400,
        selectedGuids: [],
      },
      global: {
        components: { ElButton, ElCheckbox: { template: "<span class='cb' />" } },
        directives: { loading: () => undefined },
        stubs: {
          ElAutoResizer: AutoResizerStub,
          ElTableV2: TableStub,
          ElIcon: { template: "<span />" },
        },
      },
    });

    const nameButton = wrapper.find(".people-table__name");
    expect(nameButton.exists()).toBe(true);
    expect(nameButton.text()).toBe(samplePerson.fullName);
  });

  it("renders a compact SMS hint for people who have a phone", () => {
    const withPhone: PersonListDto = {
      ...samplePerson,
      phoneOnlineVoter: {
        hasPhoneRow: true,
        whenRegistered: null,
        smsStatus: "landline",
      },
    };
    const wrapper = mount(PeopleTable, {
      props: {
        people: [withPhone],
        loading: false,
        tableHeight: 400,
        selectedGuids: [],
      },
      global: {
        components: {
          ElButton,
          ElCheckbox: { template: "<span class='cb' />" },
          ElTag: { template: "<span><slot /></span>" },
        },
        directives: { loading: () => undefined },
        stubs: {
          ElAutoResizer: AutoResizerStub,
          ElTableV2: TableStub,
          ElIcon: { template: "<span />" },
          ElTag: {
            props: ["type", "size"],
            template: '<span class="sms-tag"><slot /></span>',
          },
        },
      },
    });

    expect(wrapper.text()).toContain("people.phoneOnlineVoter.smsBlocked");
  });

  it("does not invent an SMS hint when the person has no phone", () => {
    const noPhone: PersonListDto = {
      ...samplePerson,
      phone: undefined,
      phoneOnlineVoter: null,
    };
    const wrapper = mount(PeopleTable, {
      props: {
        people: [noPhone],
        loading: false,
        tableHeight: 400,
        selectedGuids: [],
      },
      global: {
        components: { ElButton, ElCheckbox: { template: "<span class='cb' />" } },
        directives: { loading: () => undefined },
        stubs: {
          ElAutoResizer: AutoResizerStub,
          ElTableV2: TableStub,
          ElIcon: { template: "<span />" },
        },
      },
    });

    expect(wrapper.text()).not.toContain("people.phoneOnlineVoter.neverSeen");
    expect(wrapper.text()).not.toContain("people.phoneOnlineVoter.smsOk");
  });

  it("emits selected person guids from the existing people table", async () => {
    const wrapper = mount(PeopleTable, {
      props: {
        people: [samplePerson],
        loading: false,
        tableHeight: 400,
        selectedGuids: [],
      },
      global: {
        components: {
          ElButton,
          ElCheckbox: {
            props: ["modelValue"],
            emits: ["change"],
            template:
              '<button class="select-stub" @click="$emit(\'change\', true)" />',
          },
        },
        directives: { loading: () => undefined },
        stubs: {
          ElAutoResizer: AutoResizerStub,
          ElTableV2: TableStub,
          ElIcon: { template: "<span />" },
        },
      },
    });

    await wrapper.find(".select-stub").trigger("click");
    expect(wrapper.emitted("update:selectedGuids")?.[0]).toEqual([
      [samplePerson.personGuid],
    ]);
  });
});
