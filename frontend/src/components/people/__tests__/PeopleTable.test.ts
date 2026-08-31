import { mount } from "@vue/test-utils";
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
  props: ["columns", "data"],
  setup(props: { columns: Array<{ cellRenderer?: Function }>; data: PersonListDto[] }) {
    return () =>
      h(
        "div",
        { class: "table-stub" },
        props.columns.map((col, index) =>
          col.cellRenderer
            ? h("div", { key: index }, [col.cellRenderer({ rowData: props.data[0] })])
            : null,
        ),
      );
  },
};

const AutoResizerStub = {
  template: "<div><slot :height=\"400\" :width=\"800\" /></div>",
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
        stubs: {
          ElAutoResizer: AutoResizerStub,
          ElTableV2: TableStub,
          ElButton: {
            props: ["type", "link", "class"],
            template:
              '<button class="el-button" :class="$attrs.class || class"><slot /></button>',
          },
          ElIcon: { template: "<span />" },
        },
      },
    });

    const nameButton = wrapper.find(".people-table__name");
    expect(nameButton.exists()).toBe(true);
    expect(nameButton.text()).toBe(samplePerson.fullName);
  });
});
