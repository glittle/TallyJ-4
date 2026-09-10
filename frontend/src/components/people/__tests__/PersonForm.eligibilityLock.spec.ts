import { beforeEach, describe, expect, it, vi } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import PersonForm from "../PersonForm.vue";
import { i18n, pinia } from "@/test/setup";
import type { PersonDetailDto, PersonListDto } from "@/types/Person";

const mockGetDetails = vi.fn();

vi.mock("@/services/peopleService", () => ({
  peopleService: {
    getDetails: (...args: unknown[]) => mockGetDetails(...args),
  },
}));

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => ({
    peopleList: [],
    createPerson: vi.fn(),
    updatePerson: vi.fn(),
    deletePerson: vi.fn(),
  }),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: { votingMethods: "P" },
    fetchElectionById: vi.fn().mockResolvedValue(undefined),
  }),
}));

vi.mock("@/stores/eligibilityStore", () => ({
  useEligibilityStore: () => ({
    groupedReasons: {
      X: [
        {
          reasonGuid: "x01",
          code: "X01",
          description: "Deceased",
          canVote: false,
          canReceiveVotes: false,
          internalOnly: false,
        },
      ],
      V: [
        {
          reasonGuid: "v01",
          code: "V01",
          description: "Youth",
          canVote: true,
          canReceiveVotes: false,
          internalOnly: false,
        },
      ],
    },
    getByCode: (code: string) =>
      code === "V01"
        ? { canVote: true, canReceiveVotes: false }
        : { canVote: false, canReceiveVotes: false },
    fetchReasons: vi.fn().mockResolvedValue(undefined),
  }),
}));

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

const person: PersonListDto = {
  personGuid: "11111111-1111-1111-1111-111111111111",
  fullName: "Pat Smith",
};

function details(overrides: Partial<PersonDetailDto> = {}): PersonDetailDto {
  return {
    personGuid: person.personGuid,
    electionGuid: "22222222-2222-2222-2222-222222222222",
    lastName: "Smith",
    firstName: "Pat",
    fullName: "Pat Smith",
    voteCount: 0,
    canDelete: false,
    ...overrides,
  };
}

async function mountEditForm(detailOverrides: Partial<PersonDetailDto> = {}) {
  mockGetDetails.mockResolvedValue(details(detailOverrides));
  const wrapper = mount(PersonForm, {
    props: {
      electionGuid: "22222222-2222-2222-2222-222222222222",
      person,
      isEdit: true,
    },
    global: {
      plugins: [pinia, i18n, ElementPlus],
    },
  });
  await flushPromises();
  return wrapper;
}

describe("PersonForm eligibility lock after accepted ballot", () => {
  beforeEach(() => {
    mockGetDetails.mockReset();
  });

  it("explains the lock when the person has a voting method", async () => {
    const wrapper = await mountEditForm({ votingMethod: "P" });
    expect(wrapper.text()).toContain(
      "This person has already voted. Statuses that remove the right to vote are not available.",
    );
  });

  it("explains the lock when the person has an accepted online ballot", async () => {
    const wrapper = await mountEditForm({ hasOnlineBallot: true });
    expect(wrapper.text()).toContain(
      "This person has already voted. Statuses that remove the right to vote are not available.",
    );
  });

  it("does not show the lock when the person has not voted", async () => {
    const wrapper = await mountEditForm({});
    expect(wrapper.text()).not.toContain(
      "This person has already voted. Statuses that remove the right to vote are not available.",
    );
  });
});
