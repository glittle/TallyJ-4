import { mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { PersonResultDto } from "@/types";
import ResultsTable from "../ResultsTable.vue";

function row(overrides: Partial<PersonResultDto> = {}): PersonResultDto {
  return {
    personGuid: "p1",
    fullName: "Best, Pat",
    voteCount: 10,
    rank: 1,
    section: "E",
    isTied: false,
    tieBreakRequired: false,
    closeToNext: false,
    closeToPrev: false,
    ...overrides,
  };
}

const ElTable = {
  props: ["data"],
  template: '<div class="el-table"><slot /></div>',
};

const ElTableColumn = {
  props: ["label"],
  template: `
    <div class="col">
      <span class="label">{{ label }}</span>
      <span v-for="row in $parent.data" :key="row.personGuid" class="cell">
        <slot :row="row" />
      </span>
    </div>
  `,
};

function mountTable(results: PersonResultDto[]) {
  return mount(ResultsTable, {
    props: { results },
    global: {
      plugins: [i18n, ElementPlus],
      stubs: {
        ElTable,
        ElTableColumn,
        "el-table": ElTable,
        "el-table-column": ElTableColumn,
      },
    },
  });
}

function columnCells(wrapper: ReturnType<typeof mountTable>, label: string) {
  const column = wrapper
    .findAll(".col")
    .find((col) => col.get(".label").text() === label);
  expect(column, label).toBeTruthy();
  return column!.findAll(".cell").map((cell) => cell.text());
}

describe("ResultsTable tie-break count", () => {
  it("shows the count column when a runoff count is stored", () => {
    const wrapper = mountTable([
      row({ personGuid: "p1", tieBreakCount: 5, isTied: true }),
      row({ personGuid: "p2", fullName: "Cole, Ann", tieBreakCount: 0 }),
    ]);

    expect(columnCells(wrapper, "Tie-Break Count")).toEqual(["5", "0"]);
    expect(wrapper.text()).not.toContain("Tie-break Required");
  });

  it("shows the column for an unresolved tie and leaves an unset count blank", () => {
    const wrapper = mountTable([
      row({ tieBreakRequired: true, tieBreakCount: null, isTied: true }),
    ]);

    expect(columnCells(wrapper, "Tie-Break Count")).toEqual([""]);
    expect(wrapper.text()).toContain("Tie-break Required");
  });

  it("omits the count column when no tie-break is in play", () => {
    const wrapper = mountTable([row()]);

    const labels = wrapper.findAll(".label").map((label) => label.text());
    expect(labels).not.toContain("Tie-Break Count");
    expect(wrapper.text()).not.toContain("Tie-break Required");
  });
});
