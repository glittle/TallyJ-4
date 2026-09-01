import { mount } from "@vue/test-utils";
import { ElButton } from "element-plus";
import { describe, expect, it, vi } from "vitest";
import { h } from "vue";
import type { PersonListDto } from "../../../types";
import PeopleTable from "../PeopleTable.vue";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

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
      },
      global: {
        components: { ElButton },
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
});
