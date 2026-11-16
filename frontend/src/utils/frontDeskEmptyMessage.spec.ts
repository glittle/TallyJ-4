import { describe, expect, it } from "vitest";
import { frontDeskEmptyMessage } from "./frontDeskEmptyMessage";

function translate(
  key: string,
  params?: Record<string, string | number>,
): string {
  if (!params) {
    return key;
  }
  return `${key}:${JSON.stringify(params)}`;
}

describe("frontDeskEmptyMessage", () => {
  it("names Not registered and how many people are registered", () => {
    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "notRegistered",
          registeredCount: 29,
          notRegisteredCount: 0,
          searchQuery: "",
          extraFilterLabels: [],
        },
        translate,
      ),
    ).toBe('frontDesk.empty.notRegistered.other:{"count":"29"}');
  });

  it("uses the singular when one person is on the other list", () => {
    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "registered",
          registeredCount: 0,
          notRegisteredCount: 1,
          searchQuery: "",
          extraFilterLabels: [],
        },
        translate,
      ),
    ).toBe("frontDesk.empty.registered.one");
  });

  it("names extra filters along with the registration filter", () => {
    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "notRegistered",
          registeredCount: 29,
          notRegisteredCount: 0,
          searchQuery: "",
          extraFilterLabels: ["Ballot Not Received", "Mailed"],
        },
        translate,
      ),
    ).toBe(
      'frontDesk.empty.named:{"filter":"frontDesk.filters.nameNotRegistered, Ballot Not Received, Mailed"}',
    );
  });

  it("names a search on its own and under the active filter", () => {
    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "all",
          registeredCount: 0,
          notRegisteredCount: 0,
          searchQuery: " ada ",
          extraFilterLabels: [],
        },
        translate,
      ),
    ).toBe('frontDesk.empty.search:{"query":"ada"}');

    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "registered",
          registeredCount: 0,
          notRegisteredCount: 4,
          searchQuery: "ada",
          extraFilterLabels: [],
        },
        translate,
      ),
    ).toBe(
      'frontDesk.empty.searchUnder:{"query":"ada","filter":"frontDesk.filters.nameRegistered"}',
    );
  });

  it("says there are no eligible voters when All matches nobody", () => {
    expect(
      frontDeskEmptyMessage(
        {
          registrationFilter: "all",
          registeredCount: 0,
          notRegisteredCount: 0,
          searchQuery: "  ",
          extraFilterLabels: [],
        },
        translate,
      ),
    ).toBe("frontDesk.empty.everyone");
  });
});
