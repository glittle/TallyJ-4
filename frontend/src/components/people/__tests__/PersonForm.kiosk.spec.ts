import { beforeEach, describe, expect, it, vi } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import PersonForm from "../PersonForm.vue";
import { i18n, pinia } from "@/test/setup";
import type { PersonDetailDto, PersonListDto } from "@/types/Person";

const mockGetDetails = vi.fn();
const mockGenerateKioskCode = vi.fn();
const currentElection = { votingMethods: "IP,OL,K" };

vi.mock("@/services/peopleService", () => ({
  peopleService: {
    getDetails: (...args: unknown[]) => mockGetDetails(...args),
    generateKioskCode: (...args: unknown[]) => mockGenerateKioskCode(...args),
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
    currentElection,
    fetchElectionById: vi.fn().mockResolvedValue(undefined),
  }),
}));

vi.mock("@/stores/eligibilityStore", () => ({
  useEligibilityStore: () => ({
    groupedReasons: {},
    getByCode: vi.fn(),
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
    canDelete: true,
    ...overrides,
  };
}

describe("PersonForm kiosk generate", () => {
  beforeEach(() => {
    currentElection.votingMethods = "IP,OL,K";
    mockGetDetails.mockReset();
    mockGenerateKioskCode.mockReset();
    mockGetDetails.mockResolvedValue(details());
    mockGenerateKioskCode.mockResolvedValue("SMART");
  });

  it("mints a code from the person record when the teller generates one", async () => {
    const wrapper = mount(PersonForm, {
      props: {
        electionGuid: "22222222-2222-2222-2222-222222222222",
        person,
        isEdit: true,
      },
      global: { plugins: [i18n, ElementPlus, pinia] },
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Kiosk Code");
    const button = wrapper.get('[data-testid="generate-kiosk-code"]');
    expect(button.text()).toContain("Make a kiosk code");

    mockGetDetails.mockResolvedValueOnce(
      details({
        kioskCode: "SMART",
        kioskCodeExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
      }),
    );
    await button.trigger("click");
    await flushPromises();

    expect(mockGenerateKioskCode).toHaveBeenCalledWith(person.personGuid);
    expect(wrapper.find(".kiosk-code-field input").element).toBeTruthy();
  });

  it("hides kiosk controls when the election does not support kiosk", async () => {
    currentElection.votingMethods = "IP,OL";
    const wrapper = mount(PersonForm, {
      props: {
        electionGuid: "22222222-2222-2222-2222-222222222222",
        person,
        isEdit: true,
      },
      global: { plugins: [i18n, ElementPlus, pinia] },
    });
    await flushPromises();

    expect(wrapper.text()).not.toContain("Kiosk Code");
  });

  it("hides Make/Renew when the person already has an accepted or processed ballot", async () => {
    mockGetDetails.mockResolvedValue(
      details({
        kioskCode: "SMART",
        hasAcceptedBallot: true,
        onlineBallotStatus: "Processed",
      }),
    );

    const wrapper = mount(PersonForm, {
      props: {
        electionGuid: "22222222-2222-2222-2222-222222222222",
        person,
        isEdit: true,
      },
      global: { plugins: [i18n, ElementPlus, pinia] },
    });
    await flushPromises();

    expect(wrapper.find('[data-testid="generate-kiosk-code"]').exists()).toBe(
      false,
    );
  });
});
