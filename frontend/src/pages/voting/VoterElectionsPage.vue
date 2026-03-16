<script setup lang="ts">
import { onMounted, computed } from "vue";
import { useRouter } from "vue-router";
import {
  ElCard,
  ElButton,
  ElEmpty,
  ElTag,
  ElAlert,
} from "element-plus";
import { useOnlineVotingStore } from "../../stores/onlineVotingStore";
import { useNotifications } from "../../composables/useNotifications";
import { useI18n } from "vue-i18n";

const router = useRouter();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showError } = useNotifications();

onMounted(async () => {
  if (!onlineVotingStore.voterId) {
    router.push({ name: "voter-auth" });
    return;
  }

  try {
    await onlineVotingStore.loadAvailableElections(onlineVotingStore.voterId);
  } catch {
    showError(t("voting.elections.loadError"));
  }
});

const openElections = computed(() =>
  onlineVotingStore.availableElections.filter((e) => e.isOpen),
);

const otherElections = computed(() =>
  onlineVotingStore.availableElections.filter((e) => !e.isOpen),
);

function selectElection(electionGuid: string) {
  router.push(`/vote/${electionGuid}`);
}

function formatDate(dateStr: string | undefined): string {
  if (!dateStr) return "";
  return new Date(dateStr).toLocaleString();
}

function formatDateOnly(dateStr: string | undefined): string {
  if (!dateStr) return "";
  return new Date(dateStr).toLocaleDateString();
}

function closedAgo(closeDate: string | undefined): string {
  if (!closeDate) return t("voting.elections.status.closed");
  const diff = Date.now() - new Date(closeDate).getTime();
  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  if (days < 1) return t("voting.elections.status.closedToday");
  if (days === 1) return t("voting.elections.status.closedYesterday");
  if (days < 30) return t("voting.elections.status.closedDaysAgo", { days });
  const months = Math.floor(days / 30);
  if (months < 12) return t("voting.elections.status.closedMonthsAgo", { months });
  const years = Math.floor(months / 12);
  return t("voting.elections.status.closedYearsAgo", { years });
}

function timeUntilClose(closeDate: string | undefined, isEstimate: boolean): string {
  if (!closeDate) return t("voting.elections.status.open");
  const diff = new Date(closeDate).getTime() - Date.now();
  const minutes = Math.floor(diff / (1000 * 60));
  if (minutes < 1) return t("voting.elections.status.closingSoon");
  if (minutes < 60) return t("voting.elections.status.closingInMinutes", { minutes });
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    const remainingMinutes = minutes % 60;
    const atTime = new Date(closeDate).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    const suffix = isEstimate ? t("voting.elections.status.estimatedSuffix") : "";
    if (remainingMinutes > 0) {
      return t("voting.elections.status.closingAtWithMinutes", { hours, minutes: remainingMinutes, time: atTime }) + suffix;
    }
    return t("voting.elections.status.closingAt", { time: atTime }) + suffix;
  }
  return t("voting.elections.status.open");
}

function handleLogout() {
  onlineVotingStore.logout();
  router.push("/");
}
</script>

<template>
  <div class="voter-elections-page">
    <div class="elections-container">
      <ElAlert
        v-if="onlineVotingStore.voterId"
        type="info"
        :closable="false"
        class="welcome-alert"
      >
        {{ $t("voting.elections.welcome", { voterId: onlineVotingStore.voterId }) }}
        <br />
        {{ $t("voting.elections.welcomeHint") }}
      </ElAlert>

      <div v-if="onlineVotingStore.loading" class="loading-state">
        {{ $t("voting.elections.loading") }}
      </div>

      <div v-else-if="onlineVotingStore.availableElections.length === 0">
        <ElEmpty :description="$t('voting.elections.noElections')" />
      </div>

      <div v-else class="elections-list">
        <table class="elections-table">
          <thead>
            <tr>
              <th>{{ $t("voting.elections.col.election") }}</th>
              <th>{{ $t("voting.elections.col.onlineVoting") }}</th>
              <th>{{ $t("voting.elections.col.yourNameAndBallot") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="election in onlineVotingStore.availableElections"
              :key="election.electionGuid"
              :class="{ 'row-open': election.isOpen }"
            >
              <td class="election-info-cell">
                <div class="election-name">{{ election.name }}</div>
                <div v-if="election.dateOfElection" class="election-date">
                  {{ formatDateOnly(election.dateOfElection) }}
                </div>
                <div v-if="election.convenor" class="election-convenor">
                  {{ election.convenor }}
                </div>
              </td>

              <td class="voting-status-cell">
                <div v-if="!election.hasOnlineVoting" class="status-no-voting">
                  {{ $t("voting.elections.status.noOnlineVoting") }}
                </div>
                <div v-else-if="election.isOpen" class="status-open">
                  <div class="open-badge">
                    <ElTag type="success" size="large">
                      {{ $t("voting.elections.status.openNow") }}
                    </ElTag>
                  </div>
                  <div class="close-info">
                    {{ timeUntilClose(election.onlineWhenClose, election.onlineCloseIsEstimate) }}
                  </div>
                  <ElButton
                    v-if="!election.hasVoted"
                    type="primary"
                    size="default"
                    class="vote-button"
                    @click="selectElection(election.electionGuid)"
                  >
                    {{ $t("voting.elections.prepareBallot") }}
                  </ElButton>
                  <div v-else class="already-voted-note">
                    {{ $t("voting.elections.alreadyVoted") }}
                  </div>
                </div>
                <div v-else class="status-closed">
                  {{ election.onlineWhenClose ? closedAgo(election.onlineWhenClose) : $t("voting.elections.status.closed") }}
                </div>
              </td>

              <td class="voter-info-cell">
                <div v-if="election.voterName" class="voter-name">
                  {{ election.voterName }}
                </div>
                <div class="registration-status">
                  {{ $t("voting.elections.registered") }}
                  <span class="voting-method">
                    {{ election.hasVoted ? $t("voting.elections.method.online") : "-" }}
                  </span>
                </div>
                <div v-if="election.ballotStatus" class="ballot-status">
                  <ElTag :type="election.ballotStatus === 'Submitted' ? 'success' : 'info'" size="small">
                    {{ election.ballotStatus }}
                  </ElTag>
                  <span v-if="election.whenBallotStatus" class="ballot-date">
                    {{ formatDate(election.whenBallotStatus) }}
                  </span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="activity-section">
          <p class="footer-note">{{ $t("voting.elections.footerNote") }}</p>
        </div>
      </div>

      <div class="action-bar">
        <ElButton @click="handleLogout">{{ $t("voting.elections.logout") }}</ElButton>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.voter-elections-page {
  min-height: calc(100vh - 60px);
  padding: 20px;

  .elections-container {
    max-width: 900px;
    margin: 0 auto;

    .welcome-alert {
      margin-bottom: 20px;
    }

    .loading-state {
      text-align: center;
      padding: 40px;
      color: var(--el-text-color-secondary);
    }

    .elections-table {
      width: 100%;
      border-collapse: collapse;
      background: var(--el-bg-color);
      border: 1px solid var(--el-border-color);
      border-radius: 4px;
      overflow: hidden;

      th {
        background: var(--el-fill-color-light);
        padding: 12px 16px;
        text-align: center;
        font-weight: 600;
        font-size: 13px;
        color: var(--el-text-color-primary);
        border-bottom: 1px solid var(--el-border-color);
      }

      td {
        padding: 16px;
        border-bottom: 1px solid var(--el-border-color-lighter);
        vertical-align: top;
      }

      tr:last-child td {
        border-bottom: none;
      }

      tr.row-open {
        background-color: #fffbe6;
      }

      .election-info-cell {
        .election-name {
          font-weight: 600;
          font-size: 14px;
          color: var(--el-text-color-primary);
          margin-bottom: 4px;
        }

        .election-date {
          font-size: 12px;
          color: var(--el-text-color-secondary);
        }

        .election-convenor {
          font-size: 12px;
          color: var(--el-text-color-secondary);
        }
      }

      .voting-status-cell {
        text-align: center;

        .status-no-voting {
          color: var(--el-text-color-secondary);
          font-size: 13px;
        }

        .status-open {
          .open-badge {
            margin-bottom: 8px;
          }

          .close-info {
            font-size: 12px;
            color: var(--el-text-color-secondary);
            margin-bottom: 12px;
          }

          .vote-button {
            width: 100%;
          }

          .already-voted-note {
            font-size: 12px;
            color: var(--el-text-color-secondary);
            font-style: italic;
          }
        }

        .status-closed {
          color: var(--el-text-color-secondary);
          font-size: 13px;
        }
      }

      .voter-info-cell {
        text-align: center;

        .voter-name {
          font-weight: 600;
          font-size: 14px;
          color: var(--el-text-color-primary);
          margin-bottom: 4px;
        }

        .registration-status {
          font-size: 12px;
          color: var(--el-text-color-secondary);
          margin-bottom: 4px;

          .voting-method {
            font-style: italic;
          }
        }

        .ballot-status {
          margin-top: 4px;

          .ballot-date {
            font-size: 11px;
            color: var(--el-text-color-secondary);
            display: block;
            margin-top: 2px;
          }
        }
      }
    }

    .activity-section {
      margin-top: 16px;

      .footer-note {
        font-size: 12px;
        color: var(--el-text-color-secondary);
        font-style: italic;
        text-align: center;
      }
    }

    .action-bar {
      margin-top: 24px;
      text-align: center;
    }
  }
}
</style>
