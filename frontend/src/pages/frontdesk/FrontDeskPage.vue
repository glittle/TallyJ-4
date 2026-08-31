<script setup lang="ts">
import FrontDeskFiltersBar from "@/components/frontdesk/FrontDeskFiltersBar.vue";
import FrontDeskRegistrationOverlay from "@/components/frontdesk/FrontDeskRegistrationOverlay.vue";
import FrontDeskVotersTable from "@/components/frontdesk/FrontDeskVotersTable.vue";
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useFrontDeskRegistration } from "@/composables/useFrontDeskRegistration";
import { useFrontDeskVoters } from "@/composables/useFrontDeskVoters";
import { useNotifications } from "@/composables/useNotifications";
import { useViewportTableHeight } from "@/composables/useViewportTableHeight";
import { useElectionStore } from "@/stores/electionStore";
import { useLocationStore } from "@/stores/locationStore";
import type { FrontDeskVoterDto } from "@/types/FrontDesk";
import {
  getActiveTellers,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { formatNumber } from "@/utils/formatNumber";
import { formatLocationLabel } from "@/utils/locationDisplay";
import { sortRegistrationHistoryNewestFirst } from "@/utils/formatRegistrationHistory";
import { Location, Search } from "@element-plus/icons-vue";
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

const currentElection = computed(() => electionStore.currentElection);

const electionFlags = computed(() => {
  if (!currentElection.value?.flags) {
    return [] as string[];
  }
  try {
    const parsed = JSON.parse(currentElection.value.flags);
    return Array.isArray(parsed) ? (parsed as string[]) : [];
  } catch {
    return currentElection.value.flags
      .split(",")
      .map((f: string) => f.trim())
      .filter(Boolean);
  }
});

/** Matches --front-desk-table-max-width in styles/tokens.less */
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

const searchInputRef = ref<HTMLInputElement | null>(null);
const voterTableRef = ref<InstanceType<typeof FrontDeskVotersTable> | null>(
  null,
);
const tableWrapperRef = ref<HTMLElement | null>(null);
const voterListContainerRef = ref<HTMLElement | null>(null);
const keyboardHintRef = ref<HTMLElement | null>(null);
const registrationOverlayRef = ref<InstanceType<
  typeof FrontDeskRegistrationOverlay
> | null>(null);
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
const showEnvelopeDialog = ref(false);
const envelopeEditVoter = ref<FrontDeskVoterDto | null>(null);
const envelopeEditValue = ref<number | undefined>(undefined);
const highlightedPersonGuids = ref(new Set<string>());
const highlightTimers = new Map<string, number>();
const rowHighlightVersion = ref(0);

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

// Late-bound registration callbacks for SignalR (registration composable created after voters API)
const registrationApi = {
  closeRegistrationDialog: () => {},
  pendingCheckInPersonGuid: { value: null as string | null },
};

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

const {
  loading,
  searchQuery,
  selectedMethodFilters,
  selectedFlagFilters,
  registrationFilter,
  checkedInVoters,
  notCheckedInVoters,
  registrationFilterCounts,
  allVoters,
  hasActiveFilters,
  methodCountsFor,
  flagCountsFor,
  fetchEligibleVoters,
  checkInVoter,
  unregisterVoter,
  savePersonFlags,
  initializeSignalR,
  joinElection,
  leaveElection,
  toggleMethodFilter,
  toggleFlagFilter,
  clearFilters,
} = useFrontDeskVoters({
  electionGuid,
  t,
  onPersonCheckedIn: (voter) => {
    flashUpdatedRow(voter.personGuid);
    if (selectedVoter.value?.personGuid === voter.personGuid) {
      selectedVoter.value = voter;
    }
    if (registrationApi.pendingCheckInPersonGuid.value === voter.personGuid) {
      showSuccessMessage(
        t("frontDesk.messages.checkInSuccess", { name: voter.fullName }),
      );
      registrationApi.closeRegistrationDialog();
    }
  },
  onPersonFlagsUpdated: (voter) => {
    if (selectedVoter.value?.personGuid === voter.personGuid) {
      selectedVoter.value = voter;
    }
  },
});

const {
  showRegistrationButtons,
  selectedButtonIndex,
  pendingVotingMethod,
  checkInInProgress,
  pendingCheckInPersonGuid,
  openRegistrationDialog,
  closeRegistrationDialog,
  focusRegistrationButton,
  getInitialDialogButtonIndex,
  handleRegistrationKeydown,
  clickDialogButton,
  getDialogButtonKey,
  isDialogButtonKeyboardFocused,
  hasFlag,
  getVotingMethodLabel,
  handleUnregisterSelected,
  formatTime,
  formatTimeline,
} = useFrontDeskRegistration({
  electionGuid,
  hasActiveTeller,
  electionFlags,
  registrationTypes,
  selectedVoter,
  searchInputRef: searchInputRef as any,
  registrationOverlayRef: registrationOverlayRef as any,
  checkInVoter,
  unregisterVoter,
  savePersonFlags,
  t: t as any,
  showSuccessMessage,
  showErrorMessage,
});

registrationApi.closeRegistrationDialog = closeRegistrationDialog;
registrationApi.pendingCheckInPersonGuid = pendingCheckInPersonGuid;

const methodCounts = computed(() => methodCountsFor(registrationTypes.value));
const flagCounts = computed(() => flagCountsFor(electionFlags.value));

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

function handleLocationChange(locationGuid: string | undefined) {
  locationStore.selectLocation(locationGuid ?? null);
  if (locationGuid) {
    showSuccessMessage(t("locations.locationSelected"));
  }
}

function openEnvelopeDialog(voter: FrontDeskVoterDto) {
  if (!hasActiveTeller.value) {
    return;
  }
  envelopeEditVoter.value = voter;
  envelopeEditValue.value = voter.envNum ?? undefined;
  showEnvelopeDialog.value = true;
}

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

watch(hasActiveTeller, (active) => {
  if (!active && showRegistrationButtons.value) {
    selectedButtonIndex.value = getInitialDialogButtonIndex();
    focusRegistrationButton();
  }
  nextTick(remeasureTableHeight);
});

watch(searchQuery, () => {
  selectedIndex.value = 0;
  updateSelectedVoter();
  if (!checkInInProgress.value) {
    closeRegistrationDialog();
  }
});

watch(allVoters, () => {
  updateSelectedVoter();
});

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
                :label="formatLocationLabel($t, location)"
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
                    count: formatNumber(registrationFilterCounts.all),
                  })
                }}
              </el-radio-button>
              <el-radio-button value="notRegistered">
                {{
                  $t("frontDesk.filters.registrationNotRegistered", {
                    count: formatNumber(registrationFilterCounts.notRegistered),
                  })
                }}
              </el-radio-button>
              <el-radio-button value="registered">
                {{
                  $t("frontDesk.filters.registrationRegistered", {
                    count: formatNumber(registrationFilterCounts.registered),
                  })
                }}
              </el-radio-button>
            </el-radio-group>
          </div>
        </div>

        <FrontDeskFiltersBar
          :registration-types="registrationTypes"
          :election-flags="electionFlags"
          :selected-method-filters="selectedMethodFilters"
          :selected-flag-filters="selectedFlagFilters"
          :method-counts="methodCounts"
          :flag-counts="flagCounts"
          :has-active-filters="hasActiveFilters"
          @toggle-method="toggleMethodFilter"
          @toggle-flag="toggleFlagFilter"
          @clear="clearFilters"
        />

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

          <FrontDeskRegistrationOverlay
            v-if="showRegistrationButtons && selectedVoter"
            ref="registrationOverlayRef"
            :voter="selectedVoter"
            :registration-history="selectedVoterRegistrationHistory"
            :registration-types="registrationTypes"
            :election-flags="electionFlags"
            :has-active-teller="hasActiveTeller"
            :check-in-in-progress="checkInInProgress"
            :pending-voting-method="pendingVotingMethod"
            :selected-button-index="selectedButtonIndex"
            :get-dialog-button-key="getDialogButtonKey"
            :is-dialog-button-keyboard-focused="isDialogButtonKeyboardFocused"
            :has-flag="hasFlag"
            :get-voting-method-label="getVotingMethodLabel"
            :format-time="formatTime"
            :format-timeline="formatTimeline"
            @keydown="handleRegistrationKeydown"
            @unregister="handleUnregisterSelected"
            @close="closeRegistrationDialog"
            @click-button="clickDialogButton"
          />
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

  .keyboard-hint {
    width: var(--front-desk-table-max-width);
    max-width: 100%;
    margin-top: 10px;
    margin-inline: auto;
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }
}
</style>
