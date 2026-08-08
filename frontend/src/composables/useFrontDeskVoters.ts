import { useLocalStorage } from "@/composables/useLocalStorage";
import { frontDeskService } from "@/services/frontDeskService";
import { signalrService } from "@/services/signalrService";
import type {
  CheckInVoterDto,
  FrontDeskStatsDto,
  FrontDeskVoterDto,
  UnregisterVoterDto,
  UpdatePersonFlagsDto,
} from "@/types/FrontDesk";
import { matchesFrontDeskVoterSearch } from "@/utils/searchStrategies";
import { computed, ref, type Ref } from "vue";

export type RegistrationFilter = "all" | "notRegistered" | "registered";

const REGISTRATION_FILTER_STORAGE_KEY = "tallyj.frontDesk.registrationFilters";
const DEFAULT_REGISTRATION_FILTER: RegistrationFilter = "notRegistered";

function isRegistrationFilter(value: unknown): value is RegistrationFilter {
  return value === "all" || value === "notRegistered" || value === "registered";
}

export type UseFrontDeskVotersOptions = {
  electionGuid: Ref<string>;
  t: (key: string) => string;
  onPersonCheckedIn?: (voter: FrontDeskVoterDto) => void;
  onPersonFlagsUpdated?: (voter: FrontDeskVoterDto) => void;
};

/**
 * Front-desk voter list: fetch, filter, SignalR sync, and check-in mutations.
 * Keeps FrontDeskPage focused on keyboard UX and layout.
 */
export function useFrontDeskVoters(options: UseFrontDeskVotersOptions) {
  const voters = ref<FrontDeskVoterDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  /** Authoritative check-in totals from `VoterCountUpdated` (not search-filtered). */
  const frontDeskStats = ref<FrontDeskStatsDto | null>(null);
  const searchQuery = ref("");

  const registrationFiltersByElection = useLocalStorage<
    Partial<Record<string, RegistrationFilter>>
  >(REGISTRATION_FILTER_STORAGE_KEY, {});

  const selectedMethodFilters = ref<string[]>([]);
  const selectedFlagFilters = ref<string[]>([]);
  const registrationFilter = computed({
    get(): RegistrationFilter {
      const stored =
        registrationFiltersByElection.value[options.electionGuid.value];
      return isRegistrationFilter(stored)
        ? stored
        : DEFAULT_REGISTRATION_FILTER;
    },
    set(value: RegistrationFilter) {
      registrationFiltersByElection.value = {
        ...registrationFiltersByElection.value,
        [options.electionGuid.value]: value,
      };
    },
  });

  const filteredVoters = computed(() => {
    if (!searchQuery.value.trim()) {
      return voters.value;
    }
    return voters.value.filter((voter) =>
      matchesFrontDeskVoterSearch(voter, searchQuery.value),
    );
  });

  const checkedInVoters = computed(() =>
    filteredVoters.value.filter((v) => v.isCheckedIn),
  );
  const notCheckedInVoters = computed(() =>
    filteredVoters.value.filter((v) => !v.isCheckedIn),
  );

  /** Prefer server stats for filter badges when the list is unfiltered. */
  const registrationFilterCounts = computed(() => {
    const stats = frontDeskStats.value;
    if (!searchQuery.value.trim() && stats) {
      return {
        all: stats.totalEligible,
        notRegistered: stats.notYetCheckedIn,
        registered: stats.checkedIn,
      };
    }
    return {
      all: filteredVoters.value.length,
      notRegistered: notCheckedInVoters.value.length,
      registered: checkedInVoters.value.length,
    };
  });

  const filteredByRegistration = computed(() => {
    switch (registrationFilter.value) {
      case "registered":
        return checkedInVoters.value;
      case "notRegistered":
        return notCheckedInVoters.value;
      case "all":
      default:
        return filteredVoters.value;
    }
  });

  const filteredByConditions = computed(() => {
    let result = filteredByRegistration.value;

    if (selectedMethodFilters.value.length > 0) {
      result = result.filter(
        (v) =>
          v.votingMethod &&
          selectedMethodFilters.value.includes(v.votingMethod),
      );
    }

    if (selectedFlagFilters.value.length > 0) {
      result = result.filter((v) => {
        if (!v.flags) {
          return false;
        }
        const voterFlags = v.flags.split(",").map((f) => f.trim());
        return selectedFlagFilters.value.every((flag) =>
          voterFlags.includes(flag),
        );
      });
    }

    return result;
  });

  const allVoters = computed(() => filteredByConditions.value);

  const hasActiveFilters = computed(
    () =>
      selectedMethodFilters.value.length > 0 ||
      selectedFlagFilters.value.length > 0,
  );

  function methodCountsFor(
    registrationTypes: { value: string }[],
  ): Record<string, number> {
    const counts: Record<string, number> = {};
    registrationTypes.forEach((method) => {
      counts[method.value] = checkedInVoters.value.filter(
        (v) => v.votingMethod === method.value,
      ).length;
    });
    return counts;
  }

  function flagCountsFor(electionFlags: string[]): Record<string, number> {
    const counts: Record<string, number> = {};
    electionFlags.forEach((flag: string) => {
      counts[flag] = filteredVoters.value.filter((v) => {
        if (!v.flags) {
          return false;
        }
        const voterFlags = v.flags.split(",").map((f) => f.trim());
        return voterFlags.includes(flag);
      }).length;
    });
    return counts;
  }

  async function fetchEligibleVoters(
    guid: string,
    fetchOptions: { silent?: boolean } = {},
  ) {
    if (!fetchOptions.silent) {
      loading.value = true;
    }
    error.value = null;
    try {
      voters.value = (await frontDeskService.getEligibleVoters(guid)).sort(
        (a, b) => (a.fullName ?? "").localeCompare(b.fullName ?? ""),
      );
    } catch (e: unknown) {
      const message =
        e instanceof Error
          ? e.message
          : options.t("frontDesk.errors.fetchVoters");
      error.value = message || options.t("frontDesk.errors.fetchVoters");
      throw e;
    } finally {
      if (!fetchOptions.silent) {
        loading.value = false;
      }
    }
  }

  function updateVoterInList(updatedVoter: FrontDeskVoterDto) {
    const index = voters.value.findIndex(
      (v) => v.personGuid === updatedVoter.personGuid,
    );
    if (index !== -1) {
      voters.value[index] = updatedVoter;
    }
  }

  async function checkInVoter(guid: string, checkInDto: CheckInVoterDto) {
    error.value = null;
    try {
      const updatedVoter = await frontDeskService.checkInVoter(
        guid,
        checkInDto,
      );
      updateVoterInList(updatedVoter);
      return updatedVoter;
    } catch (e: unknown) {
      const message =
        e instanceof Error ? e.message : options.t("frontDesk.errors.checkIn");
      error.value = message || options.t("frontDesk.errors.checkIn");
      throw e;
    }
  }

  async function unregisterVoter(
    guid: string,
    unregisterDto: UnregisterVoterDto,
  ) {
    loading.value = true;
    error.value = null;
    try {
      const updatedVoter = await frontDeskService.unregisterVoter(
        guid,
        unregisterDto,
      );
      updateVoterInList(updatedVoter);
      return updatedVoter;
    } catch (e: unknown) {
      const message =
        e instanceof Error
          ? e.message
          : options.t("frontDesk.errors.unregister");
      error.value = message || options.t("frontDesk.errors.unregister");
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function savePersonFlags(
    guid: string,
    updateFlagsDto: UpdatePersonFlagsDto,
  ) {
    loading.value = true;
    error.value = null;
    try {
      const updatedVoter = await frontDeskService.updatePersonFlags(
        guid,
        updateFlagsDto,
      );
      const index = voters.value.findIndex(
        (v) => v.personGuid === updatedVoter.personGuid,
      );
      if (index !== -1) {
        voters.value[index] = updatedVoter;
      }
      return updatedVoter;
    } catch (e: unknown) {
      const message =
        e instanceof Error
          ? e.message
          : options.t("frontDesk.errors.updatePersonFlags");
      error.value = message || options.t("frontDesk.errors.updatePersonFlags");
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToFrontDeskHub();

      connection.on("PersonCheckedIn", (voter: FrontDeskVoterDto) => {
        updateVoterInList(voter);
        options.onPersonCheckedIn?.(voter);
      });

      connection.on("PersonFlagsUpdated", (voter: FrontDeskVoterDto) => {
        updateVoterInList(voter);
        options.onPersonFlagsUpdated?.(voter);
      });

      connection.on("VoterCountUpdated", (stats: FrontDeskStatsDto) => {
        frontDeskStats.value = stats;
      });

      const refreshEligibleVoters = () => {
        frontDeskStats.value = null;
        void fetchEligibleVoters(options.electionGuid.value, {
          silent: true,
        }).catch((e) => {
          console.error("Failed to refresh voters after person update:", e);
        });
      };
      connection.on("PersonAdded", refreshEligibleVoters);
      connection.on("PersonUpdated", refreshEligibleVoters);
      connection.on("PersonDeleted", refreshEligibleVoters);

      connection.on("reloadPage", () => {
        void (async () => {
          try {
            await fetchEligibleVoters(options.electionGuid.value, {
              silent: true,
            });
            frontDeskStats.value = await frontDeskService.getStats(
              options.electionGuid.value,
            );
          } catch (e) {
            console.error("Failed to refresh front desk after reloadPage:", e);
          }
        })();
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for front desk:", e);
    }
  }

  async function joinElection(guid: string) {
    try {
      await signalrService.joinFrontDeskElection(guid);
    } catch (e) {
      console.error("Failed to join election group for front desk updates:", e);
    }
  }

  async function leaveElection(guid: string) {
    try {
      await signalrService.leaveFrontDeskElection(guid);
    } catch (e) {
      console.error(
        "Failed to leave election group for front desk updates:",
        e,
      );
    }
  }

  function toggleMethodFilter(method: string) {
    const index = selectedMethodFilters.value.indexOf(method);
    if (index > -1) {
      selectedMethodFilters.value.splice(index, 1);
    } else {
      selectedMethodFilters.value.push(method);
    }
  }

  function toggleFlagFilter(flag: string) {
    const index = selectedFlagFilters.value.indexOf(flag);
    if (index > -1) {
      selectedFlagFilters.value.splice(index, 1);
    } else {
      selectedFlagFilters.value.push(flag);
    }
  }

  function clearFilters() {
    selectedMethodFilters.value = [];
    selectedFlagFilters.value = [];
  }

  return {
    voters,
    loading,
    error,
    frontDeskStats,
    searchQuery,
    selectedMethodFilters,
    selectedFlagFilters,
    registrationFilter,
    filteredVoters,
    checkedInVoters,
    notCheckedInVoters,
    registrationFilterCounts,
    allVoters,
    hasActiveFilters,
    methodCountsFor,
    flagCountsFor,
    fetchEligibleVoters,
    updateVoterInList,
    checkInVoter,
    unregisterVoter,
    savePersonFlags,
    initializeSignalR,
    joinElection,
    leaveElection,
    toggleMethodFilter,
    toggleFlagFilter,
    clearFilters,
  };
}
