import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { h } from "vue";
import type { FrontDeskVoterDto } from "@/types/FrontDesk";
import FrontDeskVotersTable from "../FrontDeskVotersTable.vue";

vi.mock("vue-i18n", async (importOriginal) => {
  const actual = await importOriginal<typeof import("vue-i18n")>();
  return {
    ...actual,
    useI18n: () => ({
      t: (key: string) => key,
    }),
  };
});

function voter(overrides: Partial<FrontDeskVoterDto> = {}): FrontDeskVoterDto {
  return {
    personGuid: "p-1",
    fullName: "Smith, Ada",
    isCheckedIn: false,
    ...overrides,
  };
}

const TableStub = {
  name: "ElTableV2",
  props: ["columns", "data"],
  setup(props: {
    columns: Array<{
      key?: string;
      cellRenderer?: (args: { rowData: FrontDeskVoterDto }) => unknown;
    }>;
    data: FrontDeskVoterDto[];
  }) {
    return () => {
      const row = props.data[0];
      return h(
        "div",
        { class: "table-stub" },
        props.columns
          .filter((col) => col.cellRenderer)
          .map((col, index) =>
            h("div", { key: col.key ?? index, class: `cell-${col.key}` }, [
              col.cellRenderer!({ rowData: row }),
            ]),
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

const columnWidths = {
  fullName: 220,
  method: 150,
  sms: 88,
  whatsApp: 88,
  bahaiId: 110,
  area: 160,
  flags: 0,
  time: 120,
  envNum: 90,
};

function mountTable(row: FrontDeskVoterDto) {
  return mount(FrontDeskVotersTable, {
    props: {
      voters: [row],
      loading: false,
      tableHeight: 400,
      selectedIndex: -1,
      rowHighlightVersion: 0,
      highlightedPersonGuids: new Set<string>(),
      electionFlags: [],
      enableEnvelopeNumbers: false,
      hasActiveTeller: true,
      columnWidths,
    },
    global: {
      directives: { loading: () => undefined },
      stubs: {
        ElAutoResizer: AutoResizerStub,
        ElTableV2: TableStub,
        ElTag: {
          props: ["type", "size"],
          template: '<span class="sms-tag"><slot /></span>',
        },
      },
    },
  });
}

describe("FrontDeskVotersTable SMS column", () => {
  it("shows never-seen / imported / OK / block reason for people with a phone", () => {
    const blocked = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "landline",
        },
      }),
    );
    expect(blocked.find(".cell-sms").text()).toBe(
      "people.phoneOnlineVoter.smsBlocked",
    );

    const ok = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
          smsStatus: "OK",
        },
      }),
    );
    expect(ok.find(".cell-sms").text()).toBe("people.phoneOnlineVoter.smsOk");

    const imported = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: null,
        },
      }),
    );
    expect(imported.find(".cell-sms").text()).toBe(
      "people.phoneOnlineVoter.imported",
    );

    const neverSeen = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: false,
          smsStatus: null,
        },
      }),
    );
    expect(neverSeen.find(".cell-sms").text()).toBe(
      "people.phoneOnlineVoter.neverSeen",
    );
  });

  it("does not show another identifier's status when the backend reports no P row", () => {
    const wrapper = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: false,
          whenRegistered: null,
          smsStatus: null,
        },
      }),
    );

    expect(wrapper.find(".cell-sms").text()).toBe(
      "people.phoneOnlineVoter.neverSeen",
    );
    expect(wrapper.find(".cell-sms").text()).not.toContain("admin");
  });

  it("shows a dash when the person has no phone", () => {
    const wrapper = mountTable(voter({ phoneOnlineVoter: null }));
    expect(wrapper.find(".cell-sms").text()).toBe("frontDesk.common.dash");
  });

  it("does not change the SMS cell when WhatsAppStatus is a different reason", () => {
    const wrapper = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "OK",
          whatsAppStatus: "no-wa",
        },
      }),
    );
    expect(wrapper.find(".cell-sms").text()).toBe(
      "people.phoneOnlineVoter.smsOk",
    );
  });
});

describe("FrontDeskVotersTable WhatsApp column", () => {
  it("shows never-seen / imported / OK / reason for people with a phone", () => {
    const noWa = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "OK",
          whatsAppStatus: "no-wa",
        },
      }),
    );
    expect(noWa.find(".cell-whatsApp").text()).toBe(
      "people.phoneOnlineVoter.whatsAppReason",
    );
    expect(noWa.find(".cell-sms").text()).toBe("people.phoneOnlineVoter.smsOk");

    const ok = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
          whatsAppStatus: "OK",
        },
      }),
    );
    expect(ok.find(".cell-whatsApp").text()).toBe(
      "people.phoneOnlineVoter.whatsAppOk",
    );

    const imported = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          whatsAppStatus: null,
        },
      }),
    );
    expect(imported.find(".cell-whatsApp").text()).toBe(
      "people.phoneOnlineVoter.imported",
    );

    const neverSeen = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: false,
          whatsAppStatus: null,
        },
      }),
    );
    expect(neverSeen.find(".cell-whatsApp").text()).toBe(
      "people.phoneOnlineVoter.neverSeen",
    );
  });

  it("does not show another identifier's WhatsApp status when the backend reports no P row", () => {
    const wrapper = mountTable(
      voter({
        phoneOnlineVoter: {
          hasPhoneRow: false,
          whenRegistered: null,
          whatsAppStatus: null,
        },
      }),
    );

    expect(wrapper.find(".cell-whatsApp").text()).toBe(
      "people.phoneOnlineVoter.neverSeen",
    );
    expect(wrapper.find(".cell-whatsApp").text()).not.toContain("OK");
    expect(wrapper.text()).not.toMatch(/\+\d/);
  });

  it("shows a dash when the person has no phone", () => {
    const wrapper = mountTable(voter({ phoneOnlineVoter: null }));
    expect(wrapper.find(".cell-whatsApp").text()).toBe("frontDesk.common.dash");
  });
});
