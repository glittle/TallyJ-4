<template>
  <div :class="['public-display-page', displayOptions.theme]">
    <div v-if="loading && !displayData" class="loading-container">
      <el-icon class="loading-icon" :size="100">
        <Loading />
      </el-icon>
      <p class="loading-text">Loading election results...</p>
    </div>

    <div v-else-if="error" class="error-container">
      <el-icon class="error-icon" :size="100">
        <WarningFilled />
      </el-icon>
      <p class="error-text">{{ error }}</p>
    </div>

    <div v-else-if="displayData" class="display-container">
      <header class="display-header">
        <h1 class="election-name">{{ displayData.electionName }}</h1>
        <div class="election-meta">
          <span v-if="displayData.dateOfElection" class="election-date">
            {{ formatDate(displayData.dateOfElection) }}
          </span>
          <span v-if="displayData.convenor" class="convenor">
            Convenor: {{ displayData.convenor }}
          </span>
        </div>
        <div class="status-badge" :class="statusClass">
          {{ displayData.tallyStatus }}
        </div>
      </header>

      <main class="display-content">
        <section v-if="displayData.electedCandidates.length > 0" class="results-section">
          <h2 class="section-title">
            ELECTED ({{ displayData.numberToElect }}
            {{ displayData.numberToElect === 1 ? "position" : "positions" }})
          </h2>
          <div class="candidates-list">
            <div
              v-for="candidate in displayData.electedCandidates"
              :key="candidate.rank"
              class="candidate-row"
              :class="{ tied: candidate.isTied }"
            >
              <span class="rank">{{ candidate.rank }}.</span>
              <span class="name">{{ candidate.fullName }}</span>
              <span v-if="displayOptions.showVoteCounts" class="votes">
                {{ candidate.voteCount }}
                {{ candidate.voteCount === 1 ? "vote" : "votes" }}
              </span>
              <el-tag
                v-if="candidate.tieBreakRequired"
                type="warning"
                size="large"
                class="tie-tag"
              >
                TIE BREAK REQUIRED
              </el-tag>
            </div>
          </div>
        </section>

        <section
          v-if="
            displayOptions.showAdditionalNames &&
            displayData.additionalCandidates.length > 0
          "
          class="results-section additional"
        >
          <h2 class="section-title">
            ADDITIONAL NAMES ({{ displayData.numberExtra }}
            {{ displayData.numberExtra === 1 ? "position" : "positions" }})
          </h2>
          <div class="candidates-list">
            <div
              v-for="candidate in displayData.additionalCandidates"
              :key="candidate.rank"
              class="candidate-row"
              :class="{ tied: candidate.isTied }"
            >
              <span class="rank">{{ candidate.rank }}.</span>
              <span class="name">{{ candidate.fullName }}</span>
              <span v-if="displayOptions.showVoteCounts" class="votes">
                {{ candidate.voteCount }}
                {{ candidate.voteCount === 1 ? "vote" : "votes" }}
              </span>
            </div>
          </div>
        </section>

        <section v-if="displayOptions.showStatistics" class="statistics-section">
          <div class="stats-grid">
            <div class="stat-item">
              <div class="stat-label">Total Ballots</div>
              <div class="stat-value">{{ displayData.statistics.totalBallots }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Valid Ballots</div>
              <div class="stat-value">{{ displayData.statistics.validBallots }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Spoiled Ballots</div>
              <div class="stat-value">{{ displayData.statistics.spoiledBallots }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Total Votes</div>
              <div class="stat-value">{{ displayData.statistics.totalVotes }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Turnout</div>
              <div class="stat-value">
                {{ displayData.statistics.turnoutPercentage.toFixed(1) }}%
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer class="display-footer">
        <div class="auto-refresh">
          <el-icon v-if="displayOptions.autoRefresh" :size="20">
            <Refresh />
          </el-icon>
          Auto-refresh: {{ displayOptions.autoRefresh ? "ON" : "OFF" }}
        </div>
        <div class="last-updated">
          Last updated: {{ formatTime(displayData.lastUpdated) }}
        </div>
      </footer>

      <div class="controls-overlay" :class="{ visible: showControls }">
        <el-button-group>
          <el-button @click="toggleTheme">
            <el-icon><Sunny /></el-icon>
            Toggle Theme
          </el-button>
          <el-button @click="toggleVoteCounts">
            <el-icon><View /></el-icon>
            {{ displayOptions.showVoteCounts ? "Hide" : "Show" }} Votes
          </el-button>
          <el-button @click="toggleStatistics">
            <el-icon><DataAnalysis /></el-icon>
            {{ displayOptions.showStatistics ? "Hide" : "Show" }} Stats
          </el-button>
          <el-button @click="toggleAutoRefresh">
            <el-icon><Refresh /></el-icon>
            {{ displayOptions.autoRefresh ? "Disable" : "Enable" }} Auto-refresh
          </el-button>
          <el-button @click="refreshNow">
            <el-icon><RefreshRight /></el-icon>
            Refresh Now
          </el-button>
        </el-button-group>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRoute } from "vue-router";
import { usePublicStore } from "../stores/publicStore";
import { storeToRefs } from "pinia";
import { ElMessage } from "element-plus";
import {
  Loading,
  WarningFilled,
  Refresh,
  Sunny,
  View,
  DataAnalysis,
  RefreshRight,
} from "@element-plus/icons-vue";
import { signalrService } from "../services/signalrService";
import type { PublicDisplayDto } from "../types";

const route = useRoute();
const publicStore = usePublicStore();
const { displayData, loading, error, displayOptions } = storeToRefs(publicStore);

const showControls = ref(false);
let refreshTimer: number | null = null;

const electionGuid = computed(() => route.params.electionGuid as string);

const statusClass = computed(() => {
  if (!displayData.value) return "";
  const status = displayData.value.tallyStatus.toLowerCase();
  if (status.includes("finalized") || status.includes("complete"))
    return "status-finalized";
  if (status.includes("progress") || status.includes("processing"))
    return "status-in-progress";
  return "status-pending";
});

function formatDate(dateString: string | null): string {
  if (!dateString) return "";
  const date = new Date(dateString);
  return date.toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

function formatTime(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleTimeString("en-US", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function toggleTheme() {
  publicStore.toggleTheme();
}

function toggleVoteCounts() {
  publicStore.setDisplayOptions({ showVoteCounts: !displayOptions.value.showVoteCounts });
}

function toggleStatistics() {
  publicStore.setDisplayOptions({ showStatistics: !displayOptions.value.showStatistics });
}

function toggleAutoRefresh() {
  publicStore.setDisplayOptions({ autoRefresh: !displayOptions.value.autoRefresh });
  if (displayOptions.value.autoRefresh) {
    startAutoRefresh();
  } else {
    stopAutoRefresh();
  }
}

async function refreshNow() {
  try {
    await publicStore.fetchPublicDisplay(electionGuid.value);
    ElMessage.success("Display refreshed");
  } catch (e: any) {
    ElMessage.error("Failed to refresh display");
  }
}

function startAutoRefresh() {
  if (refreshTimer !== null) {
    stopAutoRefresh();
  }
  refreshTimer = window.setInterval(async () => {
    try {
      await publicStore.fetchPublicDisplay(electionGuid.value);
    } catch (e) {
      console.error("Auto-refresh failed:", e);
    }
  }, displayOptions.value.refreshInterval);
}

function stopAutoRefresh() {
  if (refreshTimer !== null) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
}

function handleKeyPress(event: KeyboardEvent) {
  if (event.key === "c" || event.key === "C") {
    showControls.value = !showControls.value;
  }
  if (event.key === "f" || event.key === "F") {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
    } else {
      document.exitFullscreen();
    }
  }
}

async function setupSignalR() {
  try {
    const connection = await signalrService.connectToPublicHub();
    await signalrService.joinPublicDisplay(electionGuid.value);

    connection.on("ResultsUpdated", (data: PublicDisplayDto) => {
      console.log("Results updated via SignalR:", data);
      publicStore.updateDisplayData(data);
    });

    console.log("SignalR connected to public display");
  } catch (e) {
    console.error("Failed to setup SignalR:", e);
  }
}

async function cleanupSignalR() {
  try {
    await signalrService.leavePublicDisplay(electionGuid.value);
    await signalrService.disconnect("/hubs/public");
  } catch (e) {
    console.error("Failed to cleanup SignalR:", e);
  }
}

onMounted(async () => {
  try {
    await publicStore.fetchPublicDisplay(electionGuid.value);
    await setupSignalR();
    if (displayOptions.value.autoRefresh) {
      startAutoRefresh();
    }
    document.addEventListener("keypress", handleKeyPress);
  } catch (e: any) {
    ElMessage.error(e.message || "Failed to load public display");
  }
});

onUnmounted(async () => {
  stopAutoRefresh();
  await cleanupSignalR();
  publicStore.reset();
  document.removeEventListener("keypress", handleKeyPress);
});
</script>

<style scoped>
.public-display-page {
  min-height: 100vh;
  width: 100vw;
  overflow: auto;
  padding: 2rem;
  background-color: var(--color-bg-primary);
  color: var(--color-text-primary);
  transition: background-color 0.3s, color 0.3s;
}

.public-display-page.light {
  background-color: var(--color-bg-primary);
  color: var(--color-text-primary);
}

.public-display-page.dark {
  background-color: var(--color-gray-900);
  color: var(--color-gray-50);
}

.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 80vh;
  gap: 2rem;
}

.loading-icon,
.error-icon {
  animation: pulse 2s infinite;
  color: var(--color-text-primary);
}

@keyframes pulse {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.loading-text,
.error-text {
  font-size: 2rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.display-container {
  max-width: 1400px;
  margin: 0 auto;
}

.display-header {
  text-align: center;
  margin-bottom: 3rem;
  padding-bottom: 2rem;
  border-bottom: 3px solid var(--el-border-color);
}

.election-name {
  font-size: 3.5rem;
  font-weight: bold;
  margin: 0 0 1rem 0;
  line-height: 1.2;
  color: var(--color-text-primary);
}

.election-meta {
  display: flex;
  gap: 2rem;
  justify-content: center;
  align-items: center;
  font-size: 1.5rem;
  margin-bottom: 1rem;
  opacity: 0.8;
  color: var(--color-text-secondary);
}

.status-badge {
  display: inline-block;
  padding: 0.75rem 2rem;
  border-radius: 50px;
  font-size: 1.5rem;
  font-weight: 600;
  text-transform: uppercase;
}

.status-finalized {
  background-color: var(--color-success-500);
  color: var(--color-text-inverse);
}

.status-in-progress {
  background-color: var(--color-warning-500);
  color: var(--color-text-inverse);
}

.status-pending {
  background-color: var(--color-gray-500);
  color: var(--color-text-inverse);
}

.results-section {
  margin-bottom: 3rem;
}

.section-title {
  font-size: 2.5rem;
  font-weight: bold;
  margin-bottom: 1.5rem;
  text-transform: uppercase;
  letter-spacing: 2px;
  color: var(--color-text-primary);
}

.candidates-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.candidate-row {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 1.5rem 2rem;
  border-radius: 12px;
  font-size: 2rem;
  background-color: var(--color-bg-secondary);
  transition: background-color 0.2s;
}

.light .candidate-row {
  background-color: var(--color-bg-secondary);
}

.dark .candidate-row {
  background-color: var(--color-gray-800);
}

.candidate-row.tied {
  border-left: 6px solid var(--color-warning-500);
}

.rank {
  font-weight: bold;
  min-width: 60px;
  opacity: 0.7;
  color: var(--color-text-secondary);
}

.name {
  flex: 1;
  font-weight: 600;
  color: var(--color-text-primary);
}

.votes {
  font-size: 1.75rem;
  opacity: 0.8;
  min-width: 150px;
  text-align: right;
  color: var(--color-text-secondary);
}

.tie-tag {
  font-size: 1rem;
  padding: 0.5rem 1rem;
}

.statistics-section {
  margin-top: 4rem;
  padding-top: 2rem;
  border-top: 2px solid var(--el-border-color);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 2rem;
}

.stat-item {
  text-align: center;
  padding: 1.5rem;
  border-radius: 12px;
  background-color: var(--color-bg-secondary);
}

.light .stat-item {
  background-color: var(--color-bg-secondary);
}

.dark .stat-item {
  background-color: var(--color-gray-800);
}

.stat-label {
  font-size: 1.25rem;
  margin-bottom: 0.5rem;
  opacity: 0.7;
  color: var(--color-text-secondary);
}

.stat-value {
  font-size: 2.5rem;
  font-weight: bold;
  color: var(--color-text-primary);
}

.display-footer {
  margin-top: 4rem;
  padding-top: 2rem;
  border-top: 2px solid var(--el-border-color);
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 1.25rem;
  opacity: 0.7;
  color: var(--color-text-secondary);
}

.auto-refresh {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.controls-overlay {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  padding: 1rem;
  border-radius: 12px;
  background-color: var(--color-bg-overlay);
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.3s;
}

.controls-overlay.visible {
  opacity: 1;
  pointer-events: auto;
}

.additional .section-title {
  opacity: 0.8;
}
</style>
