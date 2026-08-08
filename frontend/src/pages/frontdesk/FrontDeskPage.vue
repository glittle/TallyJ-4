<script setup lang="ts">
import FrontDeskVotersTable from "@/components/frontdesk/FrontDeskVotersTable.vue";
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useLocalStorage } from "@/composables/useLocalStorage";
import { useNotifications } from "@/composables/useNotifications";
import { useViewportTableHeight } from "@/composables/useViewportTableHeight";
import { frontDeskService } from "@/services/frontDeskService";
import { signalrService } from "@/services/signalrService";
import { useElectionStore } from "@/stores/electionStore";
import { useLocationStore } from "@/stores/locationStore";
import type {
  CheckInVoterDto,
  FrontDeskVoterDto,
  RegistrationHistoryEntryDto,
  UnregisterVoterDto,
  UpdatePersonFlagsDto,
} from "@/types/FrontDesk";
import {
  getActiveTellerPayload,
  getActiveTellers,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { formatNumber } from "@/utils/formatNumber";
import {
  formatRegistrationHistoryDetails,
  sortRegistrationHistoryNewestFirst,
} from "@/utils/formatRegistrationHistory";
import { matchesFrontDeskVoterSearch } from "@/utils/searchStrategies";
import { Check, Close, Location, Search } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

const route = useRoute();
const { t } = useI18n();
const locationStore = useLocationStore();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.id as string);

const activeTellers = ref<ActiveTellers>(getActiveTellers());
const hasActiveTeller = computed(() =>
  Boolean(activeTellers.value.teller1.trim()),
);

function onTellersChanged(tellers: ActiveTellers) {
  activeTellers.value = tellers;
}

/** Placeholder until election setup exposes "Enable Envelope Numbers". */
const ENABLE_ENVELOPE_NUMBERS = false;

// Color mappings for consistent styling across filters and table
const methodTagColors: Record<string, string> = {
  "In Person": "#10b981", // green (success)
  Mail: "#3b82f6", // blue
  Online: "#6366f1", // indigo / primary-ish
  "Call-In": "#f59e0b", // amber
};

// Unique flag colors (nice distinct palette)
const flagColorPalette = [
  "#8b5cf6", // violet
  "#ec4899", // pink
  "#14b8a6", // teal
  "#f97316", // orange
  "#06b6d4", // cyan
  "#a855f7", // purple
];

function getFlagColor(flag: string, electionFlags: any[]): string {
  if (!electionFlags || electionFlags.length === 0) {
    return "#64748b";
  }
  const index = electionFlags.findIndex((f: any) => f === flag);
  return flagColorPalette[index % flagColorPalette.length] || "#64748b";
}

function getFlagAbbr(flag: string, electionFlags: any[]): string {
  const found = electionFlags?.find((f: any) => f === flag);
  return found?.abbr || flag.substring(0, 1).toUpperCase();
}

function getMethodFilterStyle(method: string, isActive: boolean) {
  const color = methodTagColors[method] || "#64748b";
  if (isActive) {
    return {
      backgroundColor: color,
      color: "#ffffff",
      borderColor: color,
    };
  }
  return {
    backgroundColor: "#ffffff",
    color: color,
    borderColor: color,
    borderWidth: "1.5px",
  };
}

function getFlagFilterStyle(
  flag: string,
  electionFlags: any[],
  isActive: boolean,
) {
  const color = getFlagColor(flag, electionFlags);
  if (isActive) {
    return {
      backgroundColor: color,
      color: "#ffffff",
      borderColor: color,
    };
  }
  return {
    backgroundColor: "#ffffff",
    color: color,
    borderColor: color,
    borderWidth: "1.5px",
  };
}

// State
const voters = ref<FrontDeskVoterDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const signalrInitialized = ref(false);
const searchQuery = ref("");

const currentElection = computed(() => electionStore.currentElection);

// Parse election flags
const electionFlags = computed(() => {
  if (!currentElection.value?.flags) {
    return [];
  }
  try {
    const parsed = JSON.parse(currentElection.value.flags);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return currentElection.value.flags
      .split(",")
      .map((f: string) => f.trim())
      .filter(Boolean);
  }
});

/** Matches --front-desk-table-max-width in style.less */
const FRONT_DESK_TABLE_WIDTH = 900;

const frontDeskTableColumnWidths = computed(() => {
  const hasFlags = electionFlags.value.length > 0;
  const base = {
    fullName: 250,
    method: 150,
    bahaiId: 120,
    area: 200,
    flags: 90,
    time: 130,
    envNum: 90,
  };
  const fixedSum =
    base.fullName +
    base.method +
    base.bahaiId +
    base.area +
    base.time +
    (ENABLE_ENVELOPE_NUMBERS ? base.envNum : 0) +
    (hasFlags ? base.flags : 0);
  return {
    ...base,
    fullName: base.fullName + (FRONT_DESK_TABLE_WIDTH - fixedSum),
    flags: hasFlags ? base.flags : 0,
  };
});

// Keyboard navigation
const searchInputRef = ref<HTMLInputElement | null>(null);
const voterTableRef = ref<InstanceType<typeof FrontDeskVotersTable> | null>(
  null,
);
const tableWrapperRef = ref<HTMLElement | null>(null);
const voterListContainerRef = ref<HTMLElement | null>(null);
const keyboardHintRef = ref<HTMLElement | null>(null);
const { height: tableHeight, remeasure: remeasureTableHeight } =
  useViewportTableHeight(tableWrapperRef, {
    paddingRootRef: voterListContainerRef,
    bottomRef: keyboardHintRef,
    min: 200,
  });
const selectedIndex = ref(0);
const selectedVoter = ref<FrontDeskVoterDto | null>(null);
const selectedVoterRegistrationHistory = computed(() =>
  sortRegistrationHistoryNewestFirst(
    selectedVoter.value?.registrationHistory ?? [],
  ),
);
const showRegistrationButtons = ref(false);
const selectedButtonIndex = ref(0);
const pendingVotingMethod = ref<string | null>(null);
const checkInInProgress = ref(false);
const pendingCheckInPersonGuid = ref<string | null>(null);
const registrationOverlayRef = ref<HTMLDivElement | null>(null);
const showEnvelopeDialog = ref(false);
const envelopeEditVoter = ref<FrontDeskVoterDto | null>(null);
const envelopeEditValue = ref<number | undefined>(undefined);
const highlightedPersonGuids = ref(new Set<string>());
const highlightTimers = new Map<string, number>();
const rowHighlightVersion = ref(0);

type RegistrationFilter = "all" | "notRegistered" | "registered";

const REGISTRATION_FILTER_STORAGE_KEY = "tallyj.frontDesk.registrationFilters";
const DEFAULT_REGISTRATION_FILTER: RegistrationFilter = "notRegistered";

function isRegistrationFilter(value: unknown): value is RegistrationFilter {
  return value === "all" || value === "notRegistered" || value === "registered";
}

const registrationFiltersByElection = useLocalStorage<
  Partial<Record<string, RegistrationFilter>>
>(REGISTRATION_FILTER_STORAGE_KEY, {});

// Filters
const selectedMethodFilters = ref<string[]>([]);
const selectedFlagFilters = ref<string[]>([]);
const registrationFilter = computed({
  get(): RegistrationFilter {
    const stored = registrationFiltersByElection.value[electionGuid.value];
    return isRegistrationFilter(stored) ? stored : DEFAULT_REGISTRATION_FILTER;
  },
  set(value: RegistrationFilter) {
    registrationFiltersByElection.value = {
      ...registrationFiltersByElection.value,
      [electionGuid.value]: value,
    };
  },
});

// Voter filtering
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
        v.votingMethod && selectedMethodFilters.value.includes(v.votingMethod),
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

// Registration type options
const registrationTypes = computed(() => [
  {
    value: "I",
    label: t("frontDesk.votingMethod.inPerson"),
    key: "1",
    isVotingMethod: true,
  },
  {
    value: "M",
    label: t("frontDesk.votingMethod.mail"),
    key: "2",
    isVotingMethod: true,
  },
  {
    value: "O",
    label: t("frontDesk.votingMethod.online"),
    key: "3",
    isVotingMethod: true,
  },
  {
    value: "C",
    label: t("frontDesk.votingMethod.callIn"),
    key: "4",
    isVotingMethod: true,
  },
]);

type DialogButton = {
  value: string;
  label: string;
  key: string;
  isVotingMethod: boolean;
  isUnregister: boolean;
  isClose: boolean;
};

const dialogButtons = computed((): DialogButton[] => {
  const buttons: DialogButton[] = [];

  if (!selectedVoter.value?.isCheckedIn) {
    registrationTypes.value.forEach((type) => {
      buttons.push({
        value: type.value,
        label: type.label,
        key: "",
        isVotingMethod: true,
        isUnregister: false,
        isClose: false,
      });
    });
  }

  electionFlags.value.forEach((flag: string) => {
    buttons.push({
      value: flag,
      label: flag,
      key: "",
      isVotingMethod: false,
      isUnregister: false,
      isClose: false,
    });
  });

  if (selectedVoter.value?.isCheckedIn) {
    buttons.push({
      value: "__unregister__",
      label: t("frontDesk.dialog.unregister"),
      key: "",
      isVotingMethod: false,
      isUnregister: true,
      isClose: false,
    });
  }

  buttons.push({
    value: "__close__",
    label: t("common.close"),
    key: "",
    isVotingMethod: false,
    isUnregister: false,
    isClose: true,
  });

  buttons.forEach((button, index) => {
    button.key = String(index + 1);
  });

  return buttons;
});

// Compute counts for each voting method
const methodCounts = computed(() => {
  const counts: Record<string, number> = {};
  registrationTypes.value.forEach((method) => {
    counts[method.value] = checkedInVoters.value.filter(
      (v) => v.votingMethod === method.value,
    ).length;
  });
  return counts;
});

// Compute counts for each flag
const flagCounts = computed(() => {
  const counts: Record<string, number> = {};
  electionFlags.value.forEach((flag: string) => {
    counts[flag] = filteredVoters.value.filter((v) => {
      if (!v.flags) {
        return false;
      }
      const voterFlags = v.flags.split(",").map((f) => f.trim());
      return voterFlags.includes(flag);
    }).length;
  });
  return counts;
});

async function fetchEligibleVoters(guid: string) {
  loading.value = true;
  error.value = null;
  try {
    voters.value = (await frontDeskService.getEligibleVoters(guid)).sort(
      (a, b) => (a.fullName ?? "").localeCompare(b.fullName ?? ""),
    );
  } catch (e: any) {
    error.value = e.message || t("frontDesk.errors.fetchVoters");
    throw e;
  } finally {
    loading.value = false;
  }
}

function updateVoterInList(updatedVoter: FrontDeskVoterDto) {
  const index = voters.value.findIndex(
    (v) => v.personGuid === updatedVoter.personGuid,
  );
  if (index !== -1) {
    voters.value[index] = updatedVoter;
  }
  if (selectedVoter.value?.personGuid === updatedVoter.personGuid) {
    selectedVoter.value = updatedVoter;
  }
}

function focusRegistrationOverlay() {
  nextTick(() => {
    registrationOverlayRef.value?.focus();
  });
}

function getInitialDialogButtonIndex(): number {
  if (
    !hasActiveTeller.value ||
    (selectedVoter.value?.isCheckedIn && electionFlags.value.length === 0)
  ) {
    const closeIndex = dialogButtons.value.findIndex(
      (button) => button.isClose,
    );
    return closeIndex >= 0 ? closeIndex : 0;
  }
  return 0;
}

function isDialogButtonActionable(button: DialogButton): boolean {
  if (button.isClose) {
    return true;
  }
  return hasActiveTeller.value;
}

function openRegistrationDialog() {
  showRegistrationButtons.value = true;
  selectedButtonIndex.value = getInitialDialogButtonIndex();
  focusRegistrationOverlay();
  focusRegistrationButton();
}

function closeRegistrationDialog() {
  showRegistrationButtons.value = false;
  selectedButtonIndex.value = 0;
  pendingVotingMethod.value = null;
  checkInInProgress.value = false;
  pendingCheckInPersonGuid.value = null;

  nextTick(() => {
    searchInputRef.value?.focus();
  });
}

function flashUpdatedRow(personGuid: string) {
  const existing = highlightTimers.get(personGuid);
  if (existing) {
    window.clearTimeout(existing);
  }

  const applyHighlight = () => {
    const next = new Set(highlightedPersonGuids.value);
    next.add(personGuid);
    highlightedPersonGuids.value = next;
  };

  if (highlightedPersonGuids.value.has(personGuid)) {
    const without = new Set(highlightedPersonGuids.value);
    without.delete(personGuid);
    highlightedPersonGuids.value = without;
    nextTick(applyHighlight);
  } else {
    applyHighlight();
  }

  rowHighlightVersion.value++;

  const timer = window.setTimeout(() => {
    const updated = new Set(highlightedPersonGuids.value);
    updated.delete(personGuid);
    highlightedPersonGuids.value = updated;
    highlightTimers.delete(personGuid);
    rowHighlightVersion.value++;
  }, 2000);

  highlightTimers.set(personGuid, timer);
}

async function checkInVoter(guid: string, checkInDto: CheckInVoterDto) {
  error.value = null;
  try {
    const updatedVoter = await frontDeskService.checkInVoter(guid, checkInDto);
    updateVoterInList(updatedVoter);
    return updatedVoter;
  } catch (e: any) {
    error.value = e.message || t("frontDesk.errors.checkIn");
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
  } catch (e: any) {
    error.value = e.message || t("frontDesk.errors.unregister");
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
  } catch (e: any) {
    error.value = e.message || t("frontDesk.errors.updatePersonFlags");
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
      flashUpdatedRow(voter.personGuid);

      if (pendingCheckInPersonGuid.value === voter.personGuid) {
        showSuccessMessage(
          t("frontDesk.messages.checkInSuccess", { name: voter.fullName }),
        );
        closeRegistrationDialog();
      }
    });

    connection.on("PersonFlagsUpdated", (voter: FrontDeskVoterDto) => {
      updateVoterInList(voter);
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
    console.error("Failed to leave election group for front desk updates:", e);
  }
}

onMounted(async () => {
  window.addEventListener("keydown", handlePageKeydown);
  await loadData();
  await electionStore.fetchElectionById(electionGuid.value);
  await initializeSignalR();
  await joinElection(electionGuid.value);

  await nextTick();
  remeasureTableHeight();
  focusSearchInput();
});

onUnmounted(async () => {
  window.removeEventListener("keydown", handlePageKeydown);
  highlightTimers.forEach((timer) => window.clearTimeout(timer));
  highlightTimers.clear();
  await leaveElection(electionGuid.value);
});

watch(hasActiveTeller, (active) => {
  if (!active && showRegistrationButtons.value) {
    selectedButtonIndex.value = getInitialDialogButtonIndex();
    focusRegistrationButton();
  }
  nextTick(remeasureTableHeight);
});

// Watch search query and update selection
watch(searchQuery, () => {
  selectedIndex.value = 0;
  updateSelectedVoter();
  if (!checkInInProgress.value) {
    closeRegistrationDialog();
  }
});

// Watch filtered voters and update selection
watch(allVoters, () => {
  updateSelectedVoter();
});

async function loadData() {
  try {
    await fetchEligibleVoters(electionGuid.value);
    await locationStore.fetchLocations(electionGuid.value);
    nextTick(() => {
      updateSelectedVoter();
    });
  } catch (err: any) {
    showErrorMessage(err.message || t("frontDesk.errors.loadData"));
  }
}

function updateSelectedVoter() {
  const voterList = allVoters.value;
  if (
    voterList.length > 0 &&
    selectedIndex.value >= 0 &&
    selectedIndex.value < voterList.length
  ) {
    selectedVoter.value = voterList[selectedIndex.value]!;
  } else {
    selectedVoter.value = null;
  }
  scrollToSelectedRow();
}

function scrollToSelectedRow() {
  voterTableRef.value?.scrollToSelectedRow(selectedIndex.value);
}

function isDialogButtonFocusable(button: DialogButton): boolean {
  if (!isDialogButtonActionable(button)) {
    return false;
  }
  return !(button.isClose && checkInInProgress.value);
}

function getNextDialogButtonIndex(
  currentIndex: number,
  direction: 1 | -1,
): number {
  const buttons = dialogButtons.value;
  if (buttons.length === 0) {
    return 0;
  }

  let nextIndex = currentIndex;
  for (let step = 0; step < buttons.length; step++) {
    nextIndex =
      direction === 1
        ? (nextIndex + 1) % buttons.length
        : (nextIndex - 1 + buttons.length) % buttons.length;
    const button = buttons[nextIndex];
    if (button && isDialogButtonFocusable(button)) {
      return nextIndex;
    }
  }

  return currentIndex;
}

function getDialogButtonIndex(value: string): number {
  return dialogButtons.value.findIndex((button) => button.value === value);
}

function getDialogButtonKey(value: string): string {
  return (
    dialogButtons.value.find((button) => button.value === value)?.key ?? ""
  );
}

function clickDialogButton(value: string) {
  const button = dialogButtons.value.find(
    (dialogButton) => dialogButton.value === value,
  );
  if (button) {
    handleButtonClick(button);
  }
}

function isDialogButtonKeyboardFocused(value: string): boolean {
  if (pendingVotingMethod.value === value) {
    return false;
  }
  return getDialogButtonIndex(value) === selectedButtonIndex.value;
}

function getVotingMethodLabel(method?: string): string {
  const match = registrationTypes.value.find((type) => type.value === method);
  return match?.label ?? method ?? t("frontDesk.common.dash");
}

function handleRegistrationKeydown(event: KeyboardEvent) {
  const buttons = dialogButtons.value;

  if (event.key === "ArrowLeft") {
    event.preventDefault();
    selectedButtonIndex.value = getNextDialogButtonIndex(
      selectedButtonIndex.value,
      -1,
    );
    focusRegistrationButton();
  } else if (event.key === "ArrowRight") {
    event.preventDefault();
    selectedButtonIndex.value = getNextDialogButtonIndex(
      selectedButtonIndex.value,
      1,
    );
    focusRegistrationButton();
  } else if (event.key === "Tab") {
    event.preventDefault();
    selectedButtonIndex.value = getNextDialogButtonIndex(
      selectedButtonIndex.value,
      event.shiftKey ? -1 : 1,
    );
    focusRegistrationButton();
  } else if (event.key === "Enter") {
    event.preventDefault();
    if (checkInInProgress.value) {
      return;
    }
    const selectedButton = buttons[selectedButtonIndex.value];
    if (selectedButton && isDialogButtonActionable(selectedButton)) {
      handleButtonClick(selectedButton);
    }
  } else if (event.key >= "1" && event.key <= "9") {
    event.preventDefault();
    const index = parseInt(event.key) - 1;
    if (index >= 0 && index < buttons.length) {
      const button = buttons[index];
      if (!button) {
        showErrorMessage(
          t("frontDesk.messages.invalidButton", { index: index + 1 }),
        );
        return;
      }
      if (!isDialogButtonActionable(button)) {
        return;
      }
      handleButtonClick(button);
    }
  }
}

function focusRegistrationButton() {
  nextTick(() => {
    const overlay = registrationOverlayRef.value;
    const selectedButton = dialogButtons.value[selectedButtonIndex.value];
    if (
      !overlay ||
      !selectedButton ||
      !isDialogButtonFocusable(selectedButton)
    ) {
      return;
    }

    const target = overlay.querySelector<HTMLButtonElement>(
      `[data-dialog-button="${selectedButton.value}"]`,
    );
    target?.focus();
  });
}

function isSearchInputFocused(): boolean {
  const inputComponent = searchInputRef.value as { $el?: HTMLElement } | null;
  const wrapper = inputComponent?.$el;
  if (!wrapper) {
    return false;
  }
  return wrapper.contains(document.activeElement);
}

function focusSearchInput() {
  nextTick(() => {
    searchInputRef.value?.focus();
  });
}

function hasBlockingElementPlusOverlay(): boolean {
  return Array.from(document.querySelectorAll(".el-overlay")).some(
    (overlay) =>
      overlay instanceof HTMLElement &&
      overlay.style.display !== "none" &&
      getComputedStyle(overlay).display !== "none",
  );
}

function handlePageKeydown(event: KeyboardEvent) {
  if (event.key !== "Escape") {
    return;
  }
  if (hasBlockingElementPlusOverlay()) {
    return;
  }

  // Layered Escape: (1) close registration panel, (2) clear search, (3) focus search
  if (showRegistrationButtons.value) {
    event.preventDefault();
    if (!checkInInProgress.value) {
      closeRegistrationDialog();
    }
    return;
  }

  if (isSearchInputFocused()) {
    if (searchQuery.value) {
      event.preventDefault();
      searchQuery.value = "";
    }
    return;
  }

  event.preventDefault();
  focusSearchInput();
}

function handleSearchKeydown(event: KeyboardEvent) {
  if (showRegistrationButtons.value) {
    if (event.key === "Escape") {
      return;
    }
    handleRegistrationKeydown(event);
    return;
  }

  const voterList = allVoters.value;

  if (event.key === "ArrowDown") {
    event.preventDefault();
    selectedIndex.value = Math.min(
      voterList.length - 1,
      selectedIndex.value + 1,
    );
    updateSelectedVoter();
  } else if (event.key === "ArrowUp") {
    event.preventDefault();
    selectedIndex.value = Math.max(0, selectedIndex.value - 1);
    updateSelectedVoter();
  } else if (event.key === "Enter") {
    event.preventDefault();
    if (selectedVoter.value) {
      openRegistrationDialog();
    }
  }
}

function handleRowClick(row: FrontDeskVoterDto) {
  selectedVoter.value = row;
  selectedIndex.value = allVoters.value.findIndex(
    (v) => v.personGuid === row.personGuid,
  );
  openRegistrationDialog();
}

async function confirmCheckIn(votingMethod: string) {
  if (
    !selectedVoter.value ||
    checkInInProgress.value ||
    !hasActiveTeller.value
  ) {
    return;
  }

  const personGuid = selectedVoter.value.personGuid;

  pendingVotingMethod.value = votingMethod;
  checkInInProgress.value = true;
  pendingCheckInPersonGuid.value = personGuid;

  try {
    await checkInVoter(electionGuid.value, {
      personGuid,
      votingMethod,
      ...getActiveTellerPayload(),
      votingLocationGuid: undefined,
    });
  } catch (err: any) {
    pendingVotingMethod.value = null;
    checkInInProgress.value = false;
    pendingCheckInPersonGuid.value = null;
    showErrorMessage(err.message || t("frontDesk.errors.checkIn"));
  }
}

async function handleButtonClick(button: DialogButton) {
  if (!selectedVoter.value || checkInInProgress.value) {
    return;
  }
  if (!isDialogButtonActionable(button)) {
    return;
  }

  if (button.isClose) {
    if (!checkInInProgress.value) {
      closeRegistrationDialog();
    }
  } else if (button.isUnregister) {
    await handleUnregisterSelected();
  } else if (button.isVotingMethod) {
    pendingVotingMethod.value = button.value;
    await confirmCheckIn(button.value);
  } else {
    await toggleFlag(button.value);
  }
}

function hasFlag(voter: FrontDeskVoterDto, flag: string): boolean {
  if (!voter.flags) {
    return false;
  }
  const flags = voter.flags.split(",").map((f) => f.trim());
  return flags.includes(flag);
}

async function toggleFlag(flag: string) {
  if (!selectedVoter.value || !hasActiveTeller.value) {
    return;
  }

  const currentFlags = selectedVoter.value.flags
    ? selectedVoter.value.flags
        .split(",")
        .map((f) => f.trim())
        .filter(Boolean)
    : [];

  const hasCurrentFlag = currentFlags.includes(flag);

  if (hasCurrentFlag) {
    try {
      await ElMessageBox.confirm(
        t("frontDesk.confirm.removeFlag.message", {
          flag,
          name: selectedVoter.value.fullName,
        }),
        t("frontDesk.confirm.removeFlag.title"),
        {
          confirmButtonText: t("frontDesk.confirm.removeFlag.confirm"),
          cancelButtonText: t("common.cancel"),
          type: "warning",
        },
      );
    } catch {
      return;
    }

    const updatedFlags = currentFlags.filter((f) => f !== flag);
    await updatePersonFlags(updatedFlags);
  } else {
    currentFlags.push(flag);
    await updatePersonFlags(currentFlags);
  }
}

async function updatePersonFlags(flags: string[]) {
  if (!selectedVoter.value) {
    return;
  }

  try {
    await savePersonFlags(electionGuid.value, {
      personGuid: selectedVoter.value.personGuid,
      flags: flags.join(", "),
    });

    showSuccessMessage(t("frontDesk.messages.flagsUpdated"));
  } catch (err: any) {
    showErrorMessage(err.message || t("frontDesk.errors.updateFlags"));
  }
}

async function handleUnregister(voter: FrontDeskVoterDto) {
  try {
    await ElMessageBox.confirm(
      t("frontDesk.confirm.unregister.message", { name: voter.fullName }),
      t("frontDesk.confirm.unregister.title"),
      {
        confirmButtonText: t("frontDesk.confirm.unregister.confirm"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );

    await unregisterVoter(electionGuid.value, {
      personGuid: voter.personGuid,
      reason: t("frontDesk.unregisterReason"),
    });

    showSuccessMessage(t("frontDesk.messages.unregistered"));
    return true;
  } catch (err: any) {
    if (err !== "cancel") {
      showErrorMessage(err.message || t("frontDesk.errors.unregister"));
    }
    return false;
  }
}

async function handleUnregisterSelected() {
  if (!selectedVoter.value || !hasActiveTeller.value) {
    return;
  }

  const unregistered = await handleUnregister(selectedVoter.value);
  if (unregistered) {
    selectedButtonIndex.value = 0;
    focusRegistrationOverlay();
  }
}

function formatTime(time?: string): string {
  if (!time) {
    return "";
  }
  const date = new Date(time);
  return date.toLocaleString();
}

function formatTimeline(entry: RegistrationHistoryEntryDto): string {
  return formatRegistrationHistoryDetails(entry, {
    t,
    getVotingMethodLabel,
  });
}

function handleLocationChange(locationGuid: string | undefined) {
  locationStore.selectLocation(locationGuid ?? null);
  if (locationGuid) {
    showSuccessMessage(t("locations.locationSelected"));
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

function openEnvelopeDialog(voter: FrontDeskVoterDto) {
  if (!hasActiveTeller.value) {
    return;
  }
  envelopeEditVoter.value = voter;
  envelopeEditValue.value = voter.envNum ?? undefined;
  showEnvelopeDialog.value = true;
}

// function resetEnvelopeDialog() {
//   envelopeEditVoter.value = null;
//   envelopeEditValue.value = undefined;
//   envelopeSaving.value = false;
// }

// async function saveEnvelopeNumber(clear = false) {
//   if (
//     !envelopeEditVoter.value ||
//     envelopeSaving.value ||
//     !hasActiveTeller.value
//   ) {
//     return;
//   }

//   const personGuid = envelopeEditVoter.value.personGuid;
//   const envNum = clear
//     ? null
//     : typeof envelopeEditValue.value === "number" && envelopeEditValue.value > 0
//       ? envelopeEditValue.value
//       : null;

//   envelopeSaving.value = true;
//   try {
//     const updatedVoter = await frontDeskService.updateEnvelopeNumber(
//       electionGuid.value,
//       { personGuid, envNum },
//     );
//     updateVoterInList(updatedVoter);
//     showSuccessMessage(
//       clear || envNum === null
//         ? t("frontDesk.envelope.cleared")
//         : t("frontDesk.envelope.saved"),
//     );
//     showEnvelopeDialog.value = false;
//   } catch (err: any) {
//     showErrorMessage(err.message || t("frontDesk.errors.updateEnvelope"));
//   } finally {
//     envelopeSaving.value = false;
//   }
// }
</script>

<template>
  <div class="front-desk-page">
    <div class="front-desk-content-column">
      <header class="front-desk-toolbar">
        <div class="toolbar-primary">
          <div class="toolbar-tellers">
            <ActiveTellerSelector
              :election-guid="electionGuid"
              @tellers-changed="onTellersChanged"
            />
          </div>
          <div
            v-if="locationStore.locations.length > 1"
            class="toolbar-location"
          >
            <el-icon class="location-icon" aria-hidden="true">
              <Location />
            </el-icon>
            <el-select
              :model-value="locationStore.selectedLocationGuid"
              :placeholder="$t('locations.selectLocation')"
              clearable
              class="location-select"
              :aria-label="$t('locations.currentLocation')"
              @update:model-value="handleLocationChange"
            >
              <el-option
                v-for="location in locationStore.sortedLocations"
                :key="location.locationGuid"
                :label="location.name"
                :value="location.locationGuid"
              />
            </el-select>
          </div>
        </div>
      </header>

      <el-alert
        v-if="!hasActiveTeller"
        type="warning"
        :title="$t('frontDesk.tellerRequired.title')"
        :description="$t('frontDesk.tellerRequired.message')"
        show-icon
        :closable="false"
        class="teller-required-alert"
      />

      <section class="front-desk-workspace">
        <div class="search-zone">
          <div class="search-row">
            <div class="search">
              <label class="search-label" for="front-desk-search-input">{{
                $t("frontDesk.section.quickCheckIn")
              }}</label>
              <el-input
                id="front-desk-search-input"
                ref="searchInputRef"
                v-model="searchQuery"
                class="search-input"
                :placeholder="$t('frontDesk.search.placeholder')"
                clearable
                @keydown="handleSearchKeydown"
              >
                <template #prefix>
                  <el-icon>
                    <Search />
                  </el-icon>
                </template>
              </el-input>
            </div>
            <el-radio-group
              v-model="registrationFilter"
              class="registration-filter"
            >
              <el-radio-button value="all">
                {{
                  $t("frontDesk.filters.registrationAll", {
                    count: formatNumber(filteredVoters.length),
                  })
                }}
              </el-radio-button>
              <el-radio-button value="notRegistered">
                {{
                  $t("frontDesk.filters.registrationNotRegistered", {
                    count: formatNumber(notCheckedInVoters.length),
                  })
                }}
              </el-radio-button>
              <el-radio-button value="registered">
                {{
                  $t("frontDesk.filters.registrationRegistered", {
                    count: formatNumber(checkedInVoters.length),
                  })
                }}
              </el-radio-button>
            </el-radio-group>
          </div>
        </div>

        <!-- UNIFIED FILTERS ROW -->
        <div
          v-if="registrationTypes.length > 0 || electionFlags.length > 0"
          class="filters-bar"
        >
          <div>
            <div class="filter-group">
              <span class="filter-label">{{
                $t("frontDesk.filters.votingMethods")
              }}</span>

              <el-button
                v-for="method in registrationTypes"
                :key="method.value"
                size="small"
                :style="
                  getMethodFilterStyle(
                    method.label,
                    selectedMethodFilters.includes(method.value),
                  )
                "
                :class="{
                  'filter-active': selectedMethodFilters.includes(method.value),
                }"
                @click="toggleMethodFilter(method.value)"
              >
                {{ method.label }} ({{ methodCounts[method.value] || 0 }})
              </el-button>
            </div>

            <div v-if="electionFlags.length > 0" class="filter-group">
              <span class="filter-label">{{
                $t("frontDesk.filters.flags")
              }}</span>

              <el-button
                v-for="flag in electionFlags"
                :key="flag"
                size="small"
                :style="
                  getFlagFilterStyle(
                    flag,
                    electionFlags,
                    selectedFlagFilters.includes(flag),
                  )
                "
                :class="{ 'filter-active': selectedFlagFilters.includes(flag) }"
                @click="toggleFlagFilter(flag)"
              >
                {{ flag }} - {{ getFlagAbbr(flag, electionFlags) }} ({{
                  flagCounts[flag] || 0
                }})
              </el-button>
            </div>
          </div>

          <el-button
            :class="{ hasActive: hasActiveFilters }"
            size="small"
            class="clear-btn"
            @click="clearFilters"
          >
            {{ $t("common.clearFilters") }}
          </el-button>
        </div>

        <div ref="voterListContainerRef" class="voter-list-container">
          <div ref="tableWrapperRef" class="table-wrapper">
            <FrontDeskVotersTable
              ref="voterTableRef"
              :voters="allVoters"
              :loading="loading"
              :table-height="tableHeight"
              :selected-index="selectedIndex"
              :row-highlight-version="rowHighlightVersion"
              :highlighted-person-guids="highlightedPersonGuids"
              :election-flags="electionFlags"
              :enable-envelope-numbers="ENABLE_ENVELOPE_NUMBERS"
              :has-active-teller="hasActiveTeller"
              :column-widths="frontDeskTableColumnWidths"
              @row-click="handleRowClick"
              @open-envelope="openEnvelopeDialog"
            />
          </div>
          <div ref="keyboardHintRef" class="keyboard-hint">
            {{
              $t("frontDesk.keyboardHint", {
                notCheckedIn: notCheckedInVoters.length,
                checkedIn: checkedInVoters.length,
              })
            }}
          </div>

          <div
            v-if="showRegistrationButtons && selectedVoter"
            ref="registrationOverlayRef"
            class="registration-overlay"
            tabindex="-1"
            @keydown.capture="handleRegistrationKeydown"
          >
            <div class="registration-buttons">
              <el-alert
                v-if="!hasActiveTeller"
                type="warning"
                :title="$t('frontDesk.tellerRequired.title')"
                :description="$t('frontDesk.tellerRequired.message')"
                show-icon
                :closable="false"
                class="teller-required-alert"
              />
              <div class="registration-header">
                <div class="selected-voter-info">
                  <strong>
                    {{
                      selectedVoter.isCheckedIn
                        ? $t("frontDesk.dialog.update")
                        : $t("frontDesk.dialog.checkIn")
                    }}
                    {{ selectedVoter.fullName }}
                  </strong>
                  <span v-if="selectedVoter.bahaiId" class="voter-detail">
                    {{ $t("frontDesk.dialog.id") }}
                    {{ selectedVoter.bahaiId }}
                  </span>
                  <span v-if="selectedVoter.area" class="voter-detail">
                    {{ $t("frontDesk.dialog.area") }}
                    {{ selectedVoter.area }}
                  </span>
                </div>
                <div class="registration-header-actions">
                  <el-button
                    v-if="selectedVoter.isCheckedIn"
                    type="default"
                    size="large"
                    data-dialog-button="__unregister__"
                    class="unregister-button dialog-option-button"
                    :disabled="!hasActiveTeller"
                    :class="{
                      'keyboard-focused-button':
                        isDialogButtonKeyboardFocused('__unregister__'),
                    }"
                    @click="handleUnregisterSelected"
                  >
                    {{ $t("frontDesk.dialog.unregister") }}
                    <kbd>{{ getDialogButtonKey("__unregister__") }}</kbd>
                  </el-button>
                  <el-button
                    type="default"
                    size="large"
                    data-dialog-button="__close__"
                    class="close-dialog-button"
                    :class="{
                      'keyboard-focused-button':
                        isDialogButtonKeyboardFocused('__close__'),
                    }"
                    :disabled="checkInInProgress"
                    :title="$t('common.close')"
                    @click="closeRegistrationDialog"
                  >
                    <el-icon>
                      <Close />
                    </el-icon>
                    {{ $t("common.close") }}
                  </el-button>
                </div>
              </div>

              <!-- Checked-in status -->
              <div
                v-if="selectedVoter.isCheckedIn"
                class="button-section checked-in-section"
              >
                <h4>{{ $t("frontDesk.dialog.currentRegistration") }}</h4>
                <div class="checked-in-details">
                  <el-tag type="success" size="large">
                    {{ getVotingMethodLabel(selectedVoter.votingMethod) }}
                  </el-tag>
                  <span v-if="selectedVoter.envNum" class="checked-in-detail">
                    {{
                      $t("frontDesk.dialog.envelope", {
                        num: selectedVoter.envNum,
                      })
                    }}
                  </span>
                  <span
                    v-if="selectedVoter.registrationTime"
                    class="checked-in-detail"
                  >
                    {{ formatTime(selectedVoter.registrationTime) }}
                  </span>
                </div>
              </div>

              <!-- Voting Methods -->
              <div v-if="!selectedVoter.isCheckedIn" class="button-section">
                <h4>{{ $t("frontDesk.dialog.votingMethod") }}</h4>
                <div
                  class="button-group"
                  :class="{ 'check-in-pending': checkInInProgress }"
                >
                  <el-button
                    v-for="type in registrationTypes"
                    :key="type.value"
                    :data-dialog-button="type.value"
                    :type="
                      pendingVotingMethod === type.value ? 'primary' : 'default'
                    "
                    size="large"
                    class="dialog-option-button"
                    :disabled="
                      !hasActiveTeller ||
                      (checkInInProgress && pendingVotingMethod !== type.value)
                    "
                    :class="{
                      'keyboard-focused-button': isDialogButtonKeyboardFocused(
                        type.value,
                      ),
                      'pending-button': pendingVotingMethod === type.value,
                    }"
                    @click="clickDialogButton(type.value)"
                  >
                    {{ type.label }}
                    <kbd>{{ getDialogButtonKey(type.value) }}</kbd>
                  </el-button>
                </div>
              </div>

              <!-- Flags -->
              <div v-if="electionFlags.length > 0" class="button-section">
                <h4>{{ $t("frontDesk.dialog.flags") }}</h4>
                <div class="button-group">
                  <el-button
                    v-for="flag in electionFlags"
                    :key="flag"
                    :data-dialog-button="flag"
                    :type="hasFlag(selectedVoter, flag) ? 'success' : 'default'"
                    size="large"
                    class="dialog-option-button"
                    :disabled="!hasActiveTeller || checkInInProgress"
                    :class="{
                      'keyboard-focused-button':
                        isDialogButtonKeyboardFocused(flag),
                    }"
                    @click="clickDialogButton(flag)"
                  >
                    {{ flag }}
                    <kbd>{{ getDialogButtonKey(flag) }}</kbd>
                    <el-icon
                      v-if="hasFlag(selectedVoter, flag)"
                      style="margin-left: 5px"
                    >
                      <Check />
                    </el-icon>
                  </el-button>
                </div>
              </div>

              <!-- Registration history -->
              <div
                v-if="selectedVoterRegistrationHistory.length"
                class="dialog-history-section"
              >
                <h4>{{ $t("frontDesk.dialog.registrationHistory") }}</h4>
                <el-timeline>
                  <el-timeline-item
                    v-for="(entry, index) in selectedVoterRegistrationHistory"
                    :key="index"
                    :timestamp="formatTime(entry.timestamp)"
                  >
                    {{ formatTimeline(entry) }}
                  </el-timeline-item>
                </el-timeline>
              </div>

              <div class="instruction-text">
                <template v-if="checkInInProgress">
                  {{ $t("frontDesk.dialog.checkingIn") }}
                </template>
                <template v-else>
                  {{ $t("frontDesk.dialog.instructions") }}
                </template>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style lang="less">
.front-desk-page {
  display: flex;
  flex-direction: column;
  min-height: 0;
  width: 100%;

  .front-desk-content-column {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-3);
    width: 100%;
    max-width: var(--front-desk-content-max-width);
    margin-inline: auto;
    flex: 1;
    min-height: 0;
  }

  .visually-hidden {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
  }

  .front-desk-toolbar {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: var(--spacing-4);
    flex-wrap: wrap;
    padding: var(--spacing-2) var(--spacing-3);
    background: var(--color-frontdesk-toolbar-bg);
    color: var(--el-text-color-secondary);
    font-size: var(--el-font-size-base);
  }

  .toolbar-primary {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 50px;
    min-width: 0;
  }

  .toolbar-location {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);

    .location-icon {
      color: var(--el-color-primary);
      font-size: 16px;
    }

    .location-select {
      width: 200px;
    }
  }

  .toolbar-tellers {
    margin-left: var(--spacing-4);
  }

  .teller-required-alert {
    margin: 0;
  }

  .front-desk-workspace {
    --front-desk-content-padding-x: var(--spacing-3);
    display: flex;
    flex-direction: column;
    gap: var(--spacing-3);
    flex: 1;
    min-height: 0;
  }

  .search-zone {
    padding: 0 var(--front-desk-content-padding-x) var(--spacing-1);
  }

  .search-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    width: 90%;
    margin: 0 auto;
    .search {
      flex: 1;
      display: flex;
      align-items: center;
      gap: var(--spacing-2);
    }
  }

  .search-label {
    font-size: var(--el-font-size-base);
    font-weight: var(--font-weight-medium);
    color: var(--el-text-color-regular);
    white-space: nowrap;
  }

  .search-input {
    width: 350px;
    flex-shrink: 0;

    .el-input__wrapper {
      background: var(--color-frontdesk-search-bg);
      border: 1px solid var(--color-frontdesk-search-border);
      box-shadow: var(--color-frontdesk-search-shadow);
      transition:
        border-color 0.2s ease,
        box-shadow 0.2s ease;
    }

    .el-input__inner:focus {
      box-shadow: none;
    }

    .el-input__inner {
      font-size: var(--el-font-size-base);
    }

    .el-input__prefix .el-icon {
      font-size: 1em;
      color: var(--el-color-primary);
    }
  }

  .el-radio-button__inner {
    font-weight: normal;
  }

  .registration-filter {
    flex-wrap: wrap;
  }

  .filters-bar {
    display: flex;
    padding: 0 var(--front-desk-content-padding-x);
    border-bottom: 1px solid var(--el-border-color-lighter);
    font-size: var(--font-size-sm);

    > div {
      flex: 1;
    }

    .filter-group {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: center;
      gap: 6px;
      margin-bottom: var(--spacing-2);
    }

    .filter-label {
      color: var(--el-text-color-secondary);
      margin-right: 4px;
    }

    .filter-divider {
      width: 1px;
      height: 16px;
      background: var(--el-border-color-lighter);
      margin: 0 4px;
    }

    .filter-chip {
      padding: 2px 8px;
      font-weight: normal;
      font-size: var(--font-size-sm);

      &.el-button--primary.is-text {
        color: var(--color-frontdesk-filter-active-text) !important;
        background-color: var(--color-frontdesk-filter-active-bg) !important;
        border-color: var(--color-frontdesk-filter-active-bg) !important;
      }

      &.el-button--primary.is-text:hover,
      &.el-button--primary.is-text:focus {
        color: var(--color-frontdesk-filter-active-text) !important;
        background-color: var(--color-frontdesk-filter-active-bg) !important;
        border-color: var(--color-frontdesk-filter-active-bg) !important;
        opacity: 0.92;
      }
    }

    .flag-legend {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding-bottom: var(--spacing-2);

      .legend-label {
        font-weight: var(--font-weight-medium);
        color: var(--el-text-color-secondary);
        font-size: var(--font-size-sm);
      }

      .legend-tag {
        margin: 0;
      }
    }
  }

  .voter-list-container {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    flex: 1;
    min-height: 0;
    width: 100%;
    padding: 0 var(--front-desk-content-padding-x);
  }

  .table-wrapper {
    width: var(--front-desk-table-max-width);
    max-width: 100%;
    margin-inline: auto;
  }

  .registration-overlay {
    position: absolute;
    inset: 0;
    z-index: 10;
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding: 24px 16px;
    background: color-mix(in srgb, var(--el-bg-color) 88%, transparent);
    backdrop-filter: blur(2px);
    overflow-y: auto;
    outline: none;
  }

  .registration-buttons {
    width: 100%;
    position: relative;
    max-width: 900px;
    padding: 20px;
    background: var(--color-orange-50);
    border-radius: 8px;
    border: 1px solid var(--el-border-color);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  }

  .registration-mode {
    position: absolute;
    top: -12px;
    font-size: 16px;
    font-weight: var(--font-weight-medium);
    color: var(--el-text-color-regular);
  }

  .button-section {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 10px 0;
      font-size: 14px;
      color: var(--el-text-color-regular);
    }
  }

  .registration-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 20px;
  }

  .clearFilters {
    visibility: hidden;
    &.has-active-filters {
      visibility: visible;
    }
  }

  .selected-voter-info {
    display: flex;
    flex: 1;
    flex-wrap: wrap;
    align-items: flex-end;
    gap: 0 4em;
    min-width: 0;
    font-size: 18px;

    strong {
      font-size: 1.4em;
    }
  }

  .voter-detail {
    display: flex;
    flex-direction: column;
    margin: 0;
    dt {
      font-size: 0.8rem;
      color: var(--el-text-color-secondary);
    }
    dd {
      margin: 0;
      color: var(--el-text-color-regular);
    }
  }

  .registration-header-actions {
    display: flex;
    align-items: flex-start;
    justify-content: flex-end;
    gap: 10px;
    flex: 0 0 auto;
  }

  .unregister-button,
  .dialog-option-button {
    flex: 0 0 auto;
    width: auto;
    position: relative;
    white-space: nowrap;
    padding-right: 2.25rem;
  }

  .close-dialog-button {
    flex: 0 0 auto;
    position: relative;
  }

  .envelope-dialog-prompt {
    margin: 0 0 16px;
    color: var(--el-text-color-regular);
  }

  .button-group {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 10px;
    margin-bottom: 10px;
  }

  .registration-buttons .el-button kbd {
    position: absolute;
    top: 50%;
    right: 10px;
    transform: translateY(-50%);
    font-size: 10px;
    padding: 2px 4px;
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
    line-height: 1;
  }

  .keyboard-focused-button.el-button:not(.unregister-button) {
    border: 2px solid var(--el-color-primary) !important;
    box-shadow: 0 0 0 1px var(--el-color-primary-light-5);
  }

  .keyboard-focused-button.el-button--success:not(.unregister-button) {
    border-color: var(--el-color-success) !important;
    box-shadow: 0 0 0 1px var(--el-color-success-light-5);
  }

  .unregister-button.keyboard-focused-button.el-button {
    border: 2px solid var(--el-text-color-secondary) !important;
    box-shadow: 0 0 0 1px var(--el-border-color);
  }

  .pending-button.el-button {
    background-color: var(--el-color-primary) !important;
    border-color: var(--el-color-primary) !important;
    color: #fff !important;
    box-shadow: 0 0 0 2px var(--el-color-primary-light-5);
  }

  .pending-button.el-button:hover,
  .pending-button.el-button:focus {
    background-color: var(--el-color-primary-dark-2) !important;
    border-color: var(--el-color-primary-dark-2) !important;
    color: #fff !important;
  }

  .check-in-pending .el-button:not(.pending-button) {
    opacity: 0.55;
  }

  .clear-btn {
    opacity: 0.25;
    &.hasActive {
      opacity: 1;
    }
  }

  .instruction-text {
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    margin-top: 10px;
  }

  .keyboard-hint {
    width: var(--front-desk-table-max-width);
    max-width: 100%;
    margin-top: 10px;
    margin-inline: auto;
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .stats-section {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .progress-section {
    margin-top: 16px;
  }

  .progress-label {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    font-weight: bold;
  }

  .progress-percentage {
    color: var(--el-color-primary);
  }

  .checked-in-section {
    .checked-in-details {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 16px;
      margin-bottom: 16px;
      .el-tag {
        --el-tag-font-size: 16px;
      }
    }

    .checked-in-detail {
      color: var(--el-text-color-secondary);
      font-size: 14px;
    }
  }

  .dialog-history-section {
    margin-top: 20px;
    padding-top: 16px;
    max-height: 300px;
    overflow-y: auto;
    border-top: 1px solid var(--el-border-color);

    h4 {
      margin: 0 0 12px 0;
      font-size: 14px;
      color: var(--el-text-color-regular);
    }
  }

  .performed-by {
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-style: italic;
  }
}
</style>
