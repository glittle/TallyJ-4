<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { frontDeskService } from "@/services/frontDeskService";
import { signalrService } from "@/services/signalrService";
import { useElectionStore } from "@/stores/electionStore";
import { useLocationStore } from "@/stores/locationStore";
import type {
  CheckInVoterDto,
  FrontDeskStatsDto,
  FrontDeskVoterDto,
  UnregisterVoterDto,
  UpdatePersonFlagsDto,
} from "@/types/FrontDesk";
import {
  Check,
  Clock,
  Search,
  User,
  UserFilled,
  Warning,
} from "@element-plus/icons-vue";
import type { ElTable } from "element-plus";
import { ElMessageBox } from "element-plus";
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();
const locationStore = useLocationStore();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.id as string);

// State
const voters = ref<FrontDeskVoterDto[]>([]);
const stats = ref<FrontDeskStatsDto | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);
const signalrInitialized = ref(false);
const searchQuery = ref("");

// will need locations for check-in location dropdown and to display location in voter history
// const locations = computed(() => locationStore.locations);

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

// Keyboard navigation
const searchInputRef = ref<HTMLInputElement | null>(null);
const voterTableRef = ref<InstanceType<typeof ElTable> | null>(null);
const selectedIndex = ref(0);
const selectedVoter = ref<FrontDeskVoterDto | null>(null);
const showRegistrationButtons = ref(false);
const selectedButtonIndex = ref(0);

// History dialog
const showHistoryDialog = ref(false);
const historyVoter = ref<FrontDeskVoterDto | null>(null);

// Filters
const selectedMethodFilters = ref<string[]>([]);
const selectedFlagFilters = ref<string[]>([]);
const showAllRegistered = ref(false);

// Voter filtering
const filteredVoters = computed(() => {
  if (!searchQuery.value.trim()) {
    return voters.value;
  }
  const query = searchQuery.value.toLowerCase();
  return voters.value.filter(
    (voter) =>
      voter.fullName.toLowerCase().includes(query) ||
      voter.bahaiId?.toLowerCase().includes(query) ||
      voter.area?.toLowerCase().includes(query),
  );
});

const checkedInVoters = computed(() =>
  filteredVoters.value.filter((v) => v.isCheckedIn),
);
const notCheckedInVoters = computed(() =>
  filteredVoters.value.filter((v) => !v.isCheckedIn),
);

// Filter voters based on selected filters
const filteredByConditions = computed(() => {
  let result = showAllRegistered.value
    ? filteredVoters.value
    : notCheckedInVoters.value;

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

// Merge not-checked-in and checked-in voters into one list
const allVoters = computed(() => {
  if (
    selectedMethodFilters.value.length > 0 ||
    selectedFlagFilters.value.length > 0 ||
    showAllRegistered.value
  ) {
    return filteredByConditions.value;
  }

  return filteredVoters.value;
});

// Registration type options
const registrationTypes = [
  { value: "I", label: "In Person", key: "1", isVotingMethod: true },
  { value: "M", label: "Mail", key: "2", isVotingMethod: true },
  { value: "O", label: "Online", key: "3", isVotingMethod: true },
  { value: "C", label: "Call-In", key: "4", isVotingMethod: true },
];

// All buttons including voting methods and flags
const allButtons = computed(() => {
  const buttons = [...registrationTypes];
  electionFlags.value.forEach((flag: string, index: number) => {
    buttons.push({
      value: flag,
      label: flag,
      key: String(5 + index),
      isVotingMethod: false,
    });
  });
  return buttons;
});

// Compute counts for each voting method
const methodCounts = computed(() => {
  const counts: Record<string, number> = {};
  registrationTypes.forEach((method) => {
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

// Generate abbreviations for flags (first letters, max 3 chars)
function getFlagAbbr(flag: string): string {
  return flag
    .split(" ")
    .map((word) => word[0])
    .join("")
    .toUpperCase()
    .slice(0, 3);
}

async function fetchStats(guid: string) {
  try {
    stats.value = await frontDeskService.getStats(guid);
  } catch (e: any) {
    console.error("Failed to fetch stats:", e);
  }
}

async function fetchEligibleVoters(guid: string) {
  loading.value = true;
  error.value = null;
  try {
    voters.value = (await frontDeskService.getEligibleVoters(guid)).sort(
      (a, b) => a.fullName.localeCompare(b.fullName),
    );
    await fetchStats(guid);
  } catch (e: any) {
    error.value = e.message || "Failed to fetch eligible voters";
    throw e;
  } finally {
    loading.value = false;
  }
}

async function checkInVoter(guid: string, checkInDto: CheckInVoterDto) {
  loading.value = true;
  error.value = null;
  try {
    const updatedVoter = await frontDeskService.checkInVoter(guid, checkInDto);
    const index = voters.value.findIndex(
      (v) => v.personGuid === updatedVoter.personGuid,
    );
    if (index !== -1) {
      voters.value[index] = updatedVoter;
    }
    return updatedVoter;
  } catch (e: any) {
    error.value = e.message || "Failed to check in voter";
    throw e;
  } finally {
    loading.value = false;
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
    const index = voters.value.findIndex(
      (v) => v.personGuid === updatedVoter.personGuid,
    );
    if (index !== -1) {
      voters.value[index] = updatedVoter;
    }
    return updatedVoter;
  } catch (e: any) {
    error.value = e.message || "Failed to unregister voter";
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
    error.value = e.message || "Failed to update person flags";
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
      const index = voters.value.findIndex(
        (v) => v.personGuid === voter.personGuid,
      );
      if (index !== -1) {
        voters.value[index] = voter;
      }
    });

    connection.on("VoterCountUpdated", (updatedStats: FrontDeskStatsDto) => {
      stats.value = updatedStats;
    });

    connection.on("PersonFlagsUpdated", (voter: FrontDeskVoterDto) => {
      const index = voters.value.findIndex(
        (v) => v.personGuid === voter.personGuid,
      );
      if (index !== -1) {
        voters.value[index] = voter;
      }
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
  await loadData();
  await electionStore.fetchElectionById(electionGuid.value);
  await initializeSignalR();
  await joinElection(electionGuid.value);

  nextTick(() => {
    searchInputRef.value?.focus();
  });
});

onUnmounted(async () => {
  await leaveElection(electionGuid.value);
});

// Watch search query and update selection
watch(searchQuery, () => {
  selectedIndex.value = 0;
  updateSelectedVoter();
  showRegistrationButtons.value = false;
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
    showErrorMessage(err.message || "Failed to load data");
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
  nextTick(() => {
    if (voterTableRef.value && selectedIndex.value >= 0) {
      const tableWrapper = voterTableRef.value.$el.querySelector(
        ".el-table__body-wrapper",
      );
      if (tableWrapper) {
        const rows = tableWrapper.querySelectorAll(".el-table__row");
        const selectedRow = rows[selectedIndex.value];
        if (selectedRow) {
          selectedRow.scrollIntoView({ behavior: "smooth", block: "nearest" });
        }
      }
    }
  });
}

function handleSearchKeydown(event: KeyboardEvent) {
  // console.log('handleSearchKeydown called with key:', event.key);
  // console.log('Key pressed:', event.key, 'showRegistrationButtons:', showRegistrationButtons.value);
  const voterList = allVoters.value;
  // console.log('Voters count:', voterList.length);

  if (showRegistrationButtons.value) {
    const buttons = allButtons.value;
    if (event.key === "ArrowLeft") {
      event.preventDefault();
      selectedButtonIndex.value = Math.max(0, selectedButtonIndex.value - 1);
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      selectedButtonIndex.value = Math.min(
        buttons.length - 1,
        selectedButtonIndex.value + 1,
      );
    } else if (event.key === "Enter") {
      event.preventDefault();
      const selectedButton = buttons[selectedButtonIndex.value];
      if (selectedButton) {
        handleButtonClick(selectedButton);
      }
    } else if (event.key === "Escape") {
      event.preventDefault();
      showRegistrationButtons.value = false;
      selectedButtonIndex.value = 0;
    } else if (event.key >= "1" && event.key <= "9") {
      event.preventDefault();
      const index = parseInt(event.key) - 1;
      if (index >= 0 && index < buttons.length) {
        const button = buttons[index];
        if (!button) {
          showErrorMessage(`Invalid button selected: ${index}`);
          return;
        }
        handleButtonClick(button);
      }
    }
  } else {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      console.log(
        "ArrowDown pressed, current index:",
        selectedIndex.value,
        "voters length:",
        voterList.length,
      );
      selectedIndex.value = Math.min(
        voterList.length - 1,
        selectedIndex.value + 1,
      );
      console.log("New index:", selectedIndex.value);
      updateSelectedVoter();
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      console.log("ArrowUp pressed, current index:", selectedIndex.value);
      selectedIndex.value = Math.max(0, selectedIndex.value - 1);
      console.log("New index:", selectedIndex.value);
      updateSelectedVoter();
    } else if (event.key === "Enter") {
      event.preventDefault();
      console.log("Enter pressed, selectedVoter:", selectedVoter.value);
      if (selectedVoter.value) {
        showRegistrationButtons.value = true;
        selectedButtonIndex.value = 0;
      }
    }
  }
}

function handleRowClick(row: FrontDeskVoterDto) {
  selectedVoter.value = row;
  selectedIndex.value = allVoters.value.findIndex(
    (v) => v.personGuid === row.personGuid,
  );
  showRegistrationButtons.value = true;
  selectedButtonIndex.value = 0;
}

async function confirmCheckIn(votingMethod: string) {
  if (!selectedVoter.value) {
    return;
  }

  try {
    await checkInVoter(electionGuid.value, {
      personGuid: selectedVoter.value.personGuid,
      votingMethod,
      tellerName: undefined,
      votingLocationGuid: undefined,
    });

    showSuccessMessage(
      `${selectedVoter.value.fullName} checked in successfully`,
    );
    showRegistrationButtons.value = false;

    const voterList = allVoters.value;
    if (selectedIndex.value >= voterList.length) {
      selectedIndex.value = Math.max(0, voterList.length - 1);
    }
    updateSelectedVoter();

    nextTick(() => {
      searchInputRef.value?.focus();
    });
  } catch (err: any) {
    showErrorMessage(err.message || "Failed to check in voter");
  }
}

async function handleButtonClick(button: any) {
  if (!selectedVoter.value) {
    return;
  }

  if (button.isVotingMethod) {
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
  if (!selectedVoter.value) {
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
        `Remove flag "${flag}" from ${selectedVoter.value.fullName}?`,
        "Confirm Remove Flag",
        {
          confirmButtonText: "Remove",
          cancelButtonText: "Cancel",
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

    showSuccessMessage("Flags updated successfully");
  } catch (err: any) {
    showErrorMessage(err.message || "Failed to update flags");
  }
}

async function handleUnregister(voter: FrontDeskVoterDto) {
  try {
    await ElMessageBox.confirm(
      `Are you sure you want to unregister ${voter.fullName}? This will clear their check-in status.`,
      "Confirm Unregister",
      {
        confirmButtonText: "Unregister",
        cancelButtonText: "Cancel",
        type: "warning",
      },
    );

    await unregisterVoter(electionGuid.value, {
      personGuid: voter.personGuid,
      reason: "Unregistered by front desk",
    });

    showSuccessMessage("Voter unregistered successfully");
  } catch (err: any) {
    if (err !== "cancel") {
      showErrorMessage(err.message || "Failed to unregister voter");
    }
  }
}

function showHistory(voter: FrontDeskVoterDto) {
  historyVoter.value = voter;
  showHistoryDialog.value = true;
}

function formatTime(time?: string): string {
  if (!time) {
    return "";
  }
  const date = new Date(time);
  return date.toLocaleString();
}

function formatTimeShort(time?: string): string {
  if (!time) {
    return "";
  }
  const date = new Date(time);
  return date.toLocaleTimeString();
}

// function getProgressColor(percentage: number): string {
//   if (percentage < 30) {
//     return "#f56c6c";
//   }
//   if (percentage < 70) {
//     return "#e6a23c";
//   }
//   return "#67c23a";
// }

function getRowClassName({ rowIndex }: { rowIndex: number }) {
  if (rowIndex === selectedIndex.value) {
    return "selected-row";
  }
  return "";
}

function formatTimeline(entry: any): string {
  const items = [];
  if (entry.action === "CheckedIn") {
    items.push(
      `Checked in - ${entry.votingMethod === "I" ? "In Person" : entry.votingMethod === "M" ? "Mail" : entry.votingMethod === "O" ? "Online" : entry.votingMethod === "C" ? "Call-In" : entry.votingMethod}`,
    );
  }
  if (entry.tellerName) {
    items.push(`Teller: ${entry.tellerName}`);
  }
  if (entry.locationName) {
    items.push(`Location: ${entry.locationName}`);
  }
  if (entry.envNum) {
    items.push(`Envelope: #${entry.envNum}`);
  }
  if (entry.performedBy) {
    items.push(entry.performedBy);
  }
  return items.join(", ");
}

function goBack() {
  router.push(`/elections/${electionGuid.value}`);
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
  showAllRegistered.value = false;
}
</script>
<template>
  <div class="front-desk-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header
            content="Front Desk - Voter Check-In"
            @back="goBack"
          />
          <div v-if="stats" class="header-stats">
            <el-statistic
              :value="stats.checkedIn"
              title="Checked In"
              align="center"
            >
              <template #prefix>
                <el-icon>
                  <UserFilled />
                </el-icon>
              </template>
            </el-statistic>
            <el-statistic
              :value="stats.notYetCheckedIn"
              title="Not Checked In"
              align="center"
            >
              <template #prefix>
                <el-icon>
                  <User />
                </el-icon>
              </template>
            </el-statistic>
          </div>
        </div>
      </template>

      <el-row :gutter="20">
        <el-col :span="24">
          <el-card shadow="never">
            <template #header>
              <div class="section-header">
                <h3>Quick Check-In</h3>
                <el-input
                  ref="searchInputRef"
                  v-model="searchQuery"
                  placeholder="Type name to search (↑↓ arrows, Enter to select)"
                  style="width: 450px"
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
            </template>

            <!-- Filters -->
            <div class="filters-section">
              <div class="filter-group">
                <el-button
                  :type="showAllRegistered ? 'primary' : 'default'"
                  size="small"
                  @click="showAllRegistered = !showAllRegistered"
                >
                  Show All Registered ({{ checkedInVoters.length }})
                </el-button>

                <el-divider direction="vertical" />

                <span class="filter-label">Voting Methods:</span>
                <el-button
                  v-for="method in registrationTypes"
                  :key="method.value"
                  :type="
                    selectedMethodFilters.includes(method.value)
                      ? 'primary'
                      : 'default'
                  "
                  size="small"
                  @click="toggleMethodFilter(method.value)"
                >
                  {{ method.label }} ({{ methodCounts[method.value] || 0 }})
                </el-button>

                <template v-if="electionFlags.length > 0">
                  <el-divider direction="vertical" />

                  <span class="filter-label">Flags:</span>
                  <el-button
                    v-for="flag in electionFlags"
                    :key="flag"
                    :type="
                      selectedFlagFilters.includes(flag) ? 'primary' : 'default'
                    "
                    size="small"
                    @click="toggleFlagFilter(flag)"
                  >
                    {{ flag }} ({{ flagCounts[flag] || 0 }})
                  </el-button>
                </template>

                <el-button
                  v-if="
                    selectedMethodFilters.length > 0 ||
                    selectedFlagFilters.length > 0 ||
                    showAllRegistered
                  "
                  type="info"
                  size="small"
                  @click="clearFilters"
                >
                  Clear Filters
                </el-button>
              </div>

              <!-- Flag Legend -->
              <div v-if="electionFlags.length > 0" class="flag-legend">
                <span class="legend-label">Flag Abbreviations:</span>
                <el-tag
                  v-for="flag in electionFlags"
                  :key="flag"
                  size="small"
                  class="legend-tag"
                >
                  {{ getFlagAbbr(flag) }} = {{ flag }}
                </el-tag>
              </div>
            </div>

            <!-- Registration type selection (shown after pressing Enter) -->
            <div
              v-if="showRegistrationButtons && selectedVoter"
              class="registration-buttons"
            >
              <div class="selected-voter-info">
                <strong
                  >{{ selectedVoter.isCheckedIn ? "Update: " : "Check in: "
                  }}{{ selectedVoter.fullName }}</strong
                >
                <span v-if="selectedVoter.bahaiId" class="voter-detail"
                  >ID: {{ selectedVoter.bahaiId }}</span
                >
                <span v-if="selectedVoter.area" class="voter-detail"
                  >Area: {{ selectedVoter.area }}</span
                >
              </div>

              <!-- Voting Methods -->
              <div v-if="!selectedVoter.isCheckedIn" class="button-section">
                <h4>Voting Method</h4>
                <div class="button-group">
                  <el-button
                    v-for="(type, index) in registrationTypes"
                    :key="type.value"
                    :type="
                      index === selectedButtonIndex ? 'primary' : 'default'
                    "
                    size="large"
                    :class="{
                      'selected-button': index === selectedButtonIndex,
                    }"
                    @click="handleButtonClick(type)"
                  >
                    {{ type.label }} <kbd>{{ type.key }}</kbd>
                  </el-button>
                </div>
              </div>

              <!-- Flags -->
              <div v-if="electionFlags.length > 0" class="button-section">
                <h4>Flags</h4>
                <div class="button-group">
                  <el-button
                    v-for="(flag, index) in electionFlags"
                    :key="flag"
                    :type="
                      4 + index === selectedButtonIndex
                        ? 'primary'
                        : hasFlag(selectedVoter, flag)
                          ? 'success'
                          : 'default'
                    "
                    size="large"
                    :class="{
                      'selected-button': 4 + index === selectedButtonIndex,
                    }"
                    @click="toggleFlag(flag)"
                  >
                    {{ flag }} <kbd>{{ 5 + index }}</kbd>
                    <el-icon
                      v-if="hasFlag(selectedVoter, flag)"
                      style="margin-left: 5px"
                    >
                      <Check />
                    </el-icon>
                  </el-button>
                </div>
              </div>

              <div class="instruction-text">
                Use ← → arrows or number keys, press Enter to confirm, Esc to
                cancel
              </div>
            </div>

            <!-- All voters list (merged not-checked-in and checked-in) -->
            <div v-else>
              <el-table
                ref="voterTableRef"
                :data="allVoters"
                :loading="loading"
                style="width: 100%"
                max-height="600px"
                :row-class-name="getRowClassName"
                @row-click="handleRowClick"
              >
                <el-table-column
                  prop="fullName"
                  label="Name"
                  sortable
                  width="350"
                />
                <el-table-column prop="bahaiId" label="Bahá'í ID" width="120" />
                <el-table-column prop="area" label="Area" width="100" />
                <el-table-column prop="envNum" label="Env #" width="80" />
                <el-table-column label="Method" width="100">
                  <template #default="{ row }">
                    <el-tag
                      v-if="row.votingMethod === 'I'"
                      type="success"
                      size="small"
                      >In Person</el-tag
                    >
                    <el-tag
                      v-else-if="row.votingMethod === 'M'"
                      type="info"
                      size="small"
                      >Mail</el-tag
                    >
                    <el-tag
                      v-else-if="row.votingMethod === 'O'"
                      type="primary"
                      size="small"
                      >Online</el-tag
                    >
                    <el-tag
                      v-else-if="row.votingMethod === 'C'"
                      type="warning"
                      size="small"
                      >Call-In</el-tag
                    >
                    <span v-else>-</span>
                  </template>
                </el-table-column>
                <el-table-column
                  v-if="electionFlags.length > 0"
                  label="Flags"
                  width="100"
                >
                  <template #default="{ row }">
                    <template v-if="row.flags">
                      <el-tag
                        v-for="flag in electionFlags.filter((f) =>
                          hasFlag(row, f),
                        )"
                        :key="flag"
                        size="small"
                        type="success"
                        class="flag-tag"
                      >
                        {{ getFlagAbbr(flag) }}
                      </el-tag>
                    </template>
                    <span v-else>-</span>
                  </template>
                </el-table-column>
                <el-table-column label="Time" width="100">
                  <template #default="{ row }">
                    <span v-if="row.registrationTime">{{
                      formatTimeShort(row.registrationTime)
                    }}</span>
                    <span v-else>-</span>
                  </template>
                </el-table-column>
                <el-table-column label="Actions" width="120" fixed="right">
                  <template #default="{ row }">
                    <template v-if="row.isCheckedIn">
                      <el-button
                        type="default"
                        size="small"
                        :icon="Clock"
                        circle
                        title="View History"
                        @click.stop="showHistory(row)"
                      />
                      <el-button
                        type="warning"
                        size="small"
                        :icon="Warning"
                        circle
                        title="Unregister"
                        @click.stop="handleUnregister(row)"
                      />
                    </template>
                  </template>
                </el-table-column>
              </el-table>
              <div class="keyboard-hint">
                {{ notCheckedInVoters.length }} not checked in,
                {{ checkedInVoters.length }} checked in. Use keyboard to
                navigate and check in.
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- History dialog -->
    <el-dialog
      v-model="showHistoryDialog"
      title="Registration History"
      width="760px"
      class="history-detail"
    >
      <div v-if="historyVoter">
        <h4>
          {{ historyVoter.fullName }}
          <span v-if="historyVoter.bahaiId"
            >Bahá'í ID: {{ historyVoter.bahaiId }}</span
          >
        </h4>
        <el-divider />

        <div
          v-if="
            historyVoter.registrationHistory &&
            historyVoter.registrationHistory.length > 0
          "
        >
          <el-timeline>
            <el-timeline-item
              v-for="(entry, index) in historyVoter.registrationHistory"
              :key="index"
              :timestamp="formatTimeline(entry)"
            >
              {{ formatTime(entry.timestamp) }}
            </el-timeline-item>
          </el-timeline>
        </div>
        <div v-else>
          <el-empty description="No history available" />
        </div>
      </div>

      <template #footer>
        <el-button @click="showHistoryDialog = false">Close</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.front-desk-page {
  padding: 20px;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .header-stats {
    display: flex;
    align-items: center;
    gap: 20px;
  }

  .filters-section {
    margin-bottom: 20px;
    padding: 15px;
    background: var(--el-fill-color-light);
    border-radius: 8px;

    .filter-group {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 10px;
      margin-bottom: 10px;
    }

    .filter-label {
      font-weight: bold;
      color: var(--el-text-color-regular);
      margin: 0 5px;
    }

    .flag-legend {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 10px;
      padding-top: 10px;
      border-top: 1px solid var(--el-border-color);

      .legend-label {
        font-weight: bold;
        color: var(--el-text-color-secondary);
        font-size: 12px;
      }

      .legend-tag {
        margin: 0;
      }
    }
  }

  .flag-tag {
    margin-right: 4px;
  }

  .section-header {
    display: flex;
    align-items: center;
    gap: 3em;
  }

  .section-header h3 {
    margin: 0;
  }

  .registration-buttons {
    padding: 20px;
    background: var(--el-color-primary-light-9);
    border-radius: 8px;
    margin-bottom: 20px;
  }

  .button-section {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 10px 0;
      font-size: 14px;
      color: var(--el-text-color-regular);
    }
  }

  .selected-voter-info {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 20px;
    font-size: 18px;
  }

  .voter-detail {
    color: var(--el-text-color-secondary);
    font-size: 14px;
  }

  .button-group {
    display: flex;
    gap: 10px;
    margin-bottom: 10px;
  }

  .button-group .el-button {
    flex: 1;
    position: relative;
  }

  .button-group kbd {
    position: absolute;
    top: 5px;
    right: 5px;
    font-size: 10px;
    padding: 2px 4px;
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
  }

  .selected-button {
    transform: scale(1.05);
    box-shadow: 0 0 10px var(--el-color-primary);
  }

  .instruction-text {
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    margin-top: 10px;
  }

  .keyboard-hint {
    margin-top: 10px;
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

  .selected-row {
    background-color: var(--el-color-primary-light-9) !important;
  }

  .performed-by {
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-style: italic;
  }

  .history-detail {
    h4 {
      display: flex;
      align-items: center;
      gap: 20px;
      justify-content: space-between;
    }
  }
}
</style>
