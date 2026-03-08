<template>
  <div class="presentation-view">
    <div class="presentation-header">
      <h1 class="election-title">
        {{
          presentationData?.electionName || $t("presentation.electionResults")
        }}
      </h1>
      <div v-if="presentationData" class="election-meta">
        <span v-if="presentationData.electionDate" class="election-date">
          {{ formatDate(presentationData.electionDate) }}
        </span>
        <span class="election-stats">
          {{
            $t("presentation.positionsToFill", {
              count: presentationData.numToElect,
            })
          }}
          |
          {{
            $t("presentation.totalBallots", {
              count: presentationData.totalBallots,
            })
          }}
          |
          {{
            $t("presentation.totalVotes", {
              count: presentationData.totalVotes,
            })
          }}
        </span>
      </div>
    </div>

    <div v-if="loading" class="loading-container">
      <el-icon class="loading-icon" size="48"><Loading /></el-icon>
      <div class="loading-text">{{ $t("common.loading") }}</div>
    </div>

    <div v-else-if="presentationData" class="presentation-content">
      <!-- Status Banner -->
      <div
        class="status-banner"
        :class="getStatusClass(presentationData.status)"
      >
        <el-icon size="32" :class="getStatusIcon(presentationData.status)">
          <component :is="getStatusIcon(presentationData.status)" />
        </el-icon>
        <span class="status-text">{{
          getStatusText(presentationData.status)
        }}</span>
      </div>

      <!-- Elected Candidates -->
      <div class="results-section">
        <h2 class="section-title elected-title">
          {{ $t("presentation.electedCandidates") }}
        </h2>
        <div class="candidates-grid">
          <div
            v-for="candidate in presentationData.electedCandidates"
            :key="candidate.rank"
            class="candidate-card elected-card"
            :class="{ 'tied-candidate': candidate.isTied }"
          >
            <div class="candidate-rank">{{ candidate.rank }}</div>
            <div class="candidate-info">
              <div class="candidate-name">{{ candidate.fullName }}</div>
              <div class="candidate-votes">
                {{ $t("presentation.votes", { count: candidate.voteCount }) }}
              </div>
            </div>
            <div class="candidate-status">
              <el-icon v-if="candidate.isWinner" size="24" class="icon-success"
                ><Check
              /></el-icon>
              <el-icon v-if="candidate.isTied" size="24" class="icon-warning"
                ><Warning
              /></el-icon>
            </div>
          </div>
        </div>
      </div>

      <!-- Extra Candidates -->
      <div
        v-if="presentationData.extraCandidates.length > 0"
        class="results-section"
      >
        <h2 class="section-title extra-title">
          {{ $t("presentation.extraCandidates") }}
        </h2>
        <div class="candidates-grid">
          <div
            v-for="candidate in presentationData.extraCandidates"
            :key="candidate.rank"
            class="candidate-card extra-card"
            :class="{ 'tied-candidate': candidate.isTied }"
          >
            <div class="candidate-rank">{{ candidate.rank }}</div>
            <div class="candidate-info">
              <div class="candidate-name">{{ candidate.fullName }}</div>
              <div class="candidate-votes">
                {{ $t("presentation.votes", { count: candidate.voteCount }) }}
              </div>
            </div>
            <div class="candidate-status">
              <el-icon v-if="candidate.isTied" size="24" class="icon-warning"
                ><Warning
              /></el-icon>
            </div>
          </div>
        </div>
      </div>

      <!-- Ties Section -->
      <div
        v-if="presentationData.hasTies && presentationData.ties.length > 0"
        class="ties-section"
      >
        <h2 class="section-title ties-title">{{ $t("presentation.ties") }}</h2>
        <div class="ties-grid">
          <div
            v-for="tie in presentationData.ties"
            :key="tie.tieBreakGroup"
            class="tie-card"
            :class="{ 'tie-break-required': tie.tieBreakRequired }"
          >
            <div class="tie-header">
              <span class="tie-group">{{
                $t("presentation.tieGroup", { group: tie.tieBreakGroup })
              }}</span>
              <span class="tie-section">{{
                getSectionLabel(tie.section)
              }}</span>
            </div>
            <div class="tie-candidates">
              <div
                v-for="name in tie.candidateNames"
                :key="name"
                class="tie-candidate-name"
              >
                {{ name }}
              </div>
            </div>
            <div v-if="tie.tieBreakRequired" class="tie-status">
              <el-icon size="20" class="icon-error"><Warning /></el-icon>
              <span>{{ $t("presentation.tieBreakRequired") }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="no-data">
      <el-empty :description="$t('presentation.noData')" />
    </div>

    <!-- Footer -->
    <div class="presentation-footer">
      <div class="footer-content">
        <span class="footer-text">{{
          $t("presentation.generatedAt", {
            time: formatDateTime(new Date().toISOString()),
          })
        }}</span>
        <span class="footer-brand">TallyJ4 Election System</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useNotifications } from "@/composables/useNotifications";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { Loading, Check, Warning, Clock } from "@element-plus/icons-vue";
import { useResultStore } from "../../stores/resultStore";
import type { PresentationDto } from "../../types";

const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();
const { showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const electionGuid = route.params.id as string;
const presentationData = ref<PresentationDto | null>(null);
const loading = ref(false);

onMounted(async () => {
  await loadPresentationData();
});

async function loadPresentationData() {
  try {
    loading.value = true;
    const data = await resultStore.fetchPresentationData(electionGuid);
    presentationData.value = data;
  } catch (error) {
    handleApiError(error);
  } finally {
    loading.value = false;
  }
}

function formatDate(date: string) {
  if (!date) return "";
  return new Date(date).toLocaleDateString();
}

function formatDateTime(date: string) {
  if (!date) return "";
  return new Date(date).toLocaleString();
}

function getStatusClass(status: string) {
  const classMap: Record<string, string> = {
    Complete: "status-complete",
    InProgress: "status-progress",
    Paused: "status-paused",
    Error: "status-error",
  };
  return classMap[status] || "status-default";
}

function getStatusIcon(status: string) {
  const iconMap: Record<string, any> = {
    Complete: Check,
    InProgress: Clock,
    Paused: Warning,
    Error: Warning,
  };
  return iconMap[status] || Clock;
}

function getStatusText(status: string) {
  const textMap: Record<string, string> = {
    Complete: t("presentation.statusComplete"),
    InProgress: t("presentation.statusInProgress"),
    Paused: t("presentation.statusPaused"),
    Error: t("presentation.statusError"),
  };
  return textMap[status] || status;
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    E: t("presentation.elected"),
    X: t("presentation.extra"),
    O: t("presentation.other"),
  };
  return labelMap[section] || section;
}
</script>

<style lang="less">
.presentation-view {
  min-height: 100vh;
  background: var(--color-bg-primary);
  color: var(--color-text-primary);
  font-family: var(--font-family-primary);
  padding: 20px;
  display: flex;
  flex-direction: column;
}

.presentation-header {
  text-align: center;
  margin-bottom: 30px;
  padding: 20px;
  background: linear-gradient(
    135deg,
    var(--color-primary-500) 0%,
    var(--color-primary-700) 100%
  );
  border-radius: 10px;
  color: var(--color-text-inverse);
}

.election-title {
  font-size: 3rem;
  font-weight: bold;
  margin: 0 0 10px 0;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.3);
}

.election-meta {
  font-size: 1.2rem;
  opacity: 0.9;
}

.election-stats {
  display: block;
  margin-top: 5px;
  font-size: 1rem;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  color: var(--color-text-primary);
}

.loading-icon {
  margin-bottom: 20px;
  animation: spin 2s linear infinite;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

.loading-text {
  font-size: 1.5rem;
}

.presentation-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.status-banner {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 15px;
  padding: 20px;
  border-radius: 10px;
  font-size: 1.5rem;
  font-weight: bold;
  text-transform: uppercase;
  color: var(--color-text-inverse);
}

.status-complete {
  background: linear-gradient(
    135deg,
    var(--color-success-500) 0%,
    var(--color-success-600) 100%
  );
}

.status-progress {
  background: linear-gradient(
    135deg,
    var(--color-warning-500) 0%,
    var(--color-warning-600) 100%
  );
}

.status-paused {
  background: linear-gradient(
    135deg,
    var(--color-gray-500) 0%,
    var(--color-gray-600) 100%
  );
}

.status-error {
  background: linear-gradient(
    135deg,
    var(--color-error-500) 0%,
    var(--color-error-600) 100%
  );
}

.status-default {
  background: linear-gradient(
    135deg,
    var(--color-primary-500) 0%,
    var(--color-primary-600) 100%
  );
}

.results-section {
  margin-bottom: 30px;
}

.section-title {
  font-size: 2.5rem;
  font-weight: bold;
  margin-bottom: 20px;
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 2px;
}

.elected-title {
  color: var(--color-success-500);
}

.extra-title {
  color: var(--color-warning-500);
}

.ties-title {
  color: var(--color-error-500);
}

.candidates-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
}

.candidate-card {
  background: var(--color-bg-secondary);
  border: 2px solid var(--el-border-color);
  border-radius: 10px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 15px;
  transition: transform 0.3s ease;
}

.candidate-card:hover {
  transform: translateY(-5px);
}

.elected-card {
  border-color: var(--color-success-500);
  background: var(--color-success-50);
}

.dark .elected-card {
  background: rgba(34, 197, 94, 0.1);
}

.extra-card {
  border-color: var(--color-warning-500);
  background: var(--color-warning-50);
}

.dark .extra-card {
  background: rgba(245, 158, 11, 0.1);
}

.tied-candidate {
  border-color: var(--color-error-500) !important;
  background: var(--color-error-50) !important;
}

.dark .tied-candidate {
  background: rgba(239, 68, 68, 0.1) !important;
}

.candidate-rank {
  font-size: 2rem;
  font-weight: bold;
  color: var(--color-primary-500);
  min-width: 50px;
  text-align: center;
}

.candidate-info {
  flex: 1;
}

.candidate-name {
  font-size: 1.5rem;
  font-weight: bold;
  margin-bottom: 5px;
  color: var(--color-text-primary);
}

.candidate-votes {
  font-size: 1.2rem;
  opacity: 0.8;
  color: var(--color-text-secondary);
}

.candidate-status {
  font-size: 1.5rem;

  .icon-success {
    color: var(--color-success-500);
  }

  .icon-warning {
    color: var(--color-warning-500);
  }

  .icon-error {
    color: var(--color-error-500);
  }
}

.ties-section {
  margin-top: 30px;
}

.ties-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 20px;
}

.tie-card {
  background: var(--color-error-50);
  border: 2px solid var(--color-error-500);
  border-radius: 10px;
  padding: 20px;
}

.dark .tie-card {
  background: rgba(239, 68, 68, 0.1);
}

.tie-break-required {
  border-color: var(--color-error-600);
  background: var(--color-error-50);
}

.dark .tie-break-required {
  background: rgba(239, 68, 68, 0.2);
}

.tie-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
  font-size: 1.3rem;
  font-weight: bold;
  color: var(--color-text-primary);
}

.tie-candidates {
  margin-bottom: 10px;
}

.tie-candidate-name {
  font-size: 1.2rem;
  margin-bottom: 5px;
  padding: 5px 0;
  border-bottom: 1px solid var(--el-border-color-light);
  color: var(--color-text-primary);
}

.tie-candidate-name:last-child {
  border-bottom: none;
}

.tie-status {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  color: var(--color-error-600);
  font-weight: bold;
}

.presentation-footer {
  margin-top: auto;
  padding: 20px;
  border-top: 1px solid var(--el-border-color);
  text-align: center;
  color: var(--color-text-secondary);
}

.footer-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.9rem;
  opacity: 0.7;
}

.footer-brand {
  font-weight: bold;
}

.no-data {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-primary);
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .election-title {
    font-size: 2rem;
  }

  .section-title {
    font-size: 2rem;
  }

  .candidates-grid {
    grid-template-columns: 1fr;
  }

  .ties-grid {
    grid-template-columns: 1fr;
  }

  .footer-content {
    flex-direction: column;
    gap: 10px;
  }
}
</style>
