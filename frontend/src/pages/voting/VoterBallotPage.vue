<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import {
  ElCard,
  ElForm,
  ElFormItem,
  ElAutocomplete,
  ElInput,
  ElButton,
  ElAlert,
  ElEmpty,
  ElTag,
  ElDivider,
} from "element-plus";
import { Delete } from "@element-plus/icons-vue";
import { useOnlineVotingStore } from "../../stores/onlineVotingStore";
import { useNotifications } from "../../composables/useNotifications";
import { useI18n } from "vue-i18n";
import type { OnlineCandidate, OnlineVote } from "../../types";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccess, showError } = useNotifications();

const electionGuid = ref(route.params.electionId as string);
const loading = ref(false);
const submitting = ref(false);

interface VoteSlot {
  position: number;
  candidate: OnlineCandidate | null;
  freeText: string;
  searchText: string;
}

const votes = ref<VoteSlot[]>([]);

const selectionMode = computed(
  () => onlineVotingStore.electionInfo?.onlineSelectionProcess ?? "A",
);

const isModeList = computed(() => selectionMode.value === "A");
const isModeRandom = computed(() => selectionMode.value === "B");
const isModeBoth = computed(() => selectionMode.value === "C");
const showCandidateList = computed(
  () => isModeList.value || isModeBoth.value,
);

onMounted(async () => {
  if (!onlineVotingStore.voterId) {
    showError(t("voting.ballot.authRequired"));
    router.push({
      name: "voter-auth",
      query: { election: electionGuid.value },
    });
    return;
  }

  await loadElectionData();
});

async function loadElectionData() {
  try {
    loading.value = true;

    const [electionInfo, voteStatus] = await Promise.all([
      onlineVotingStore.loadElectionInfo(electionGuid.value),
      onlineVotingStore.checkVoteStatus(
        electionGuid.value,
        onlineVotingStore.voterId!,
      ),
    ]);

    if (voteStatus.hasVoted) {
      showError(voteStatus.message || t("voting.ballot.alreadyVoted"));
      router.push({ name: "voter-confirmation" });
      return;
    }

    if (!electionInfo.isOpen) {
      showError(t("voting.ballot.notOpen"));
      return;
    }

    if (showCandidateList.value) {
      await onlineVotingStore.loadCandidates(electionGuid.value);
    }

    const numToElect = electionInfo.numberToElect || 9;
    votes.value = Array.from({ length: numToElect }, (_, i) => ({
      position: i + 1,
      candidate: null,
      freeText: "",
      searchText: "",
    }));
  } catch (error) {
    console.error("Error loading election data:", error);
  } finally {
    loading.value = false;
  }
}

const candidateOptions = computed(() =>
  onlineVotingStore.candidates.map((c) => ({
    value: c.fullName,
    candidate: c,
  })),
);

function handleCandidateSelect(position: number, item: { value: string; candidate: OnlineCandidate }) {
  const slot = votes.value.find((v) => v.position === position);
  if (!slot) return;
  slot.candidate = item.candidate;
  slot.searchText = item.value;
  slot.freeText = "";
}

function handleSearchInput(position: number, value: string) {
  const slot = votes.value.find((v) => v.position === position);
  if (!slot) return;
  if (slot.candidate && slot.candidate.fullName !== value) {
    slot.candidate = null;
  }
  slot.searchText = value;
}

function clearVote(position: number) {
  const slot = votes.value.find((v) => v.position === position);
  if (slot) {
    slot.candidate = null;
    slot.freeText = "";
    slot.searchText = "";
  }
}

const duplicateVotes = computed(() => {
  const guids = votes.value
    .filter((v) => v.candidate !== null)
    .map((v) => v.candidate!.personGuid);
  return guids.some((g, i) => guids.indexOf(g) !== i);
});

const hasAnyVote = computed(() =>
  votes.value.some(
    (v) => v.candidate !== null || v.freeText.trim().length > 0,
  ),
);

const canSubmit = computed(() => hasAnyVote.value && !duplicateVotes.value);

function getEffectiveName(slot: VoteSlot): string {
  if (slot.candidate) return slot.candidate.fullName;
  if (isModeRandom.value) return slot.freeText;
  if (isModeBoth.value) return slot.searchText || slot.freeText;
  return "";
}

async function handleSubmit() {
  if (duplicateVotes.value) {
    showError(t("voting.ballot.duplicateError"));
    return;
  }

  try {
    submitting.value = true;

    const onlineVotes: OnlineVote[] = votes.value
      .filter((v) => v.candidate !== null || (isModeRandom.value && v.freeText.trim()) || (isModeBoth.value && (v.searchText.trim() || v.freeText.trim())))
      .map((v) => ({
        personGuid: v.candidate?.personGuid,
        voteName: getEffectiveName(v) || undefined,
        positionOnBallot: v.position,
      }));

    await onlineVotingStore.submitBallot(electionGuid.value, {
      electionGuid: electionGuid.value,
      voterId: onlineVotingStore.voterId!,
      votes: onlineVotes,
    });

    showSuccess(t("voting.ballot.submitSuccess"));
    router.push({ name: "voter-confirmation" });
  } catch (error) {
    console.error("Error submitting ballot:", error);
  } finally {
    submitting.value = false;
  }
}

function backToElections() {
  router.push({ name: "voter-elections" });
}
</script>

<template>
  <div class="voter-ballot-page">
    <div class="ballot-container">
      <ElCard v-if="loading">
        <div class="loading-text">{{ $t("voting.ballot.loading") }}</div>
      </ElCard>

      <ElCard v-else-if="onlineVotingStore.electionInfo" class="ballot-card">
        <template #header>
          <div class="ballot-card-header">
            <div class="header-left">
              <ElButton text size="small" @click="backToElections">
                ← {{ $t("voting.ballot.backToElections") }}
              </ElButton>
              <h2>{{ onlineVotingStore.electionInfo.name }}</h2>
              <p v-if="onlineVotingStore.electionInfo.dateOfElection">
                {{
                  new Date(
                    onlineVotingStore.electionInfo.dateOfElection,
                  ).toLocaleDateString()
                }}
              </p>
              <p v-if="onlineVotingStore.electionInfo.convenor">
                {{ onlineVotingStore.electionInfo.convenor }}
              </p>
            </div>
            <div class="header-right">
              <ElTag type="success">{{ $t("voting.ballot.openNow") }}</ElTag>
            </div>
          </div>
        </template>

        <ElAlert
          v-if="duplicateVotes"
          type="error"
          :closable="false"
          class="ballot-alert"
        >
          {{ $t("voting.ballot.duplicateError") }}
        </ElAlert>

        <div class="ballot-body">
          <div class="left-panel" v-if="isModeBoth">
            <h3>{{ $t("voting.ballot.addToPool") }}</h3>
            <p class="panel-description">{{ $t("voting.ballot.addToPoolDescription") }}</p>
            <ElForm>
              <ElFormItem :label="$t('voting.ballot.firstName')">
                <ElInput :placeholder="$t('voting.ballot.firstNamePlaceholder')" />
              </ElFormItem>
              <ElFormItem :label="$t('voting.ballot.lastName')">
                <ElInput :placeholder="$t('voting.ballot.lastNamePlaceholder')" />
              </ElFormItem>
              <ElFormItem :label="$t('voting.ballot.extraInfo')">
                <ElInput
                  type="textarea"
                  :placeholder="$t('voting.ballot.extraInfoPlaceholder')"
                  :rows="2"
                />
              </ElFormItem>
              <ElButton type="default">{{ $t("voting.ballot.addPersonToPool") }}</ElButton>
            </ElForm>

            <ElDivider />

            <div class="guidelines">
              <p class="guideline-quote">{{ $t("voting.ballot.guideline") }}</p>
            </div>
          </div>

          <div :class="isModeBoth ? 'right-panel' : 'full-panel'">
            <h3>
              {{
                $t("voting.ballot.votingFor", {
                  count: onlineVotingStore.electionInfo.numberToElect ?? 9,
                })
              }}
            </h3>

            <ElAlert
              v-if="isModeRandom"
              type="info"
              :closable="false"
              class="mode-hint"
            >
              {{ $t("voting.ballot.randomModeHint") }}
            </ElAlert>

            <ElAlert
              v-if="isModeBoth"
              type="info"
              :closable="false"
              class="mode-hint"
            >
              {{ $t("voting.ballot.bothModeHint") }}
            </ElAlert>

            <p class="ballot-note">{{ $t("voting.ballot.orderNote") }}</p>

            <div class="my-ballot-label">{{ $t("voting.ballot.myBallot") }}</div>

            <ElForm @submit.prevent="handleSubmit">
              <div
                v-for="vote in votes"
                :key="vote.position"
                class="vote-item"
                :class="{ 'vote-filled': vote.candidate || (isModeRandom && vote.freeText) || (isModeBoth && (vote.searchText || vote.freeText)) }"
              >
                <span class="vote-number">{{ vote.position }}.</span>

                <ElAutocomplete
                  v-if="showCandidateList"
                  v-model="vote.searchText"
                  :fetch-suggestions="
                    (queryString: string, cb: Function) => {
                      const results = queryString
                        ? candidateOptions.filter((opt) =>
                            opt.value
                              .toLowerCase()
                              .includes(queryString.toLowerCase()),
                          )
                        : candidateOptions;
                      cb(results);
                    }
                  "
                  :placeholder="$t('voting.ballot.searchPlaceholder')"
                  clearable
                  class="vote-input"
                  size="default"
                  @select="(item: any) => handleCandidateSelect(vote.position, item)"
                  @input="(val: string) => handleSearchInput(vote.position, val)"
                />

                <ElInput
                  v-else-if="isModeRandom"
                  v-model="vote.freeText"
                  :placeholder="$t('voting.ballot.namePlaceholder')"
                  class="vote-input"
                  size="default"
                />

                <span v-if="vote.candidate" class="candidate-tag">
                  {{ vote.candidate.fullName }}
                  <ElButton
                    text
                    size="small"
                    :icon="Delete"
                    @click="clearVote(vote.position)"
                    class="clear-btn"
                  />
                </span>
                <span v-else-if="isModeRandom && vote.freeText" class="random-tag">
                  {{ vote.freeText }}
                  <ElButton
                    text
                    size="small"
                    :icon="Delete"
                    @click="clearVote(vote.position)"
                    class="clear-btn"
                  />
                </span>
              </div>

              <ElAlert type="warning" :closable="false" class="ballot-warning">
                {{ $t("voting.ballot.onceWarning") }}
              </ElAlert>

              <div class="submit-actions">
                <ElButton
                  type="primary"
                  native-type="submit"
                  :loading="submitting"
                  :disabled="!canSubmit"
                  size="large"
                  class="submit-btn"
                >
                  {{ $t("voting.ballot.submit") }}
                </ElButton>
              </div>
            </ElForm>
          </div>
        </div>
      </ElCard>

      <ElCard v-else>
        <ElEmpty :description="$t('voting.ballot.electionNotFound')" />
      </ElCard>
    </div>
  </div>
</template>

<style lang="less">
.voter-ballot-page {
  min-height: calc(100vh - 100px);
  padding: 20px;

  .ballot-container {
    max-width: 900px;
    margin: 0 auto;

    .loading-text {
      text-align: center;
      padding: 40px;
    }

    .ballot-card {
      .ballot-card-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;

        .header-left {
          h2 {
            margin: 4px 0;
            color: var(--el-color-primary);
          }

          p {
            margin: 2px 0;
            color: var(--el-text-color-secondary);
            font-size: 13px;
          }
        }
      }

      .ballot-alert {
        margin-bottom: 16px;
      }

      .ballot-body {
        display: flex;
        gap: 24px;

        .left-panel {
          flex: 0 0 300px;
          border-right: 1px solid var(--el-border-color-lighter);
          padding-right: 24px;

          h3 {
            margin: 0 0 8px 0;
          }

          .panel-description {
            font-size: 13px;
            color: var(--el-text-color-secondary);
            margin-bottom: 16px;
          }

          .guidelines {
            .guideline-quote {
              font-size: 12px;
              font-style: italic;
              color: var(--el-text-color-secondary);
              line-height: 1.6;
            }
          }
        }

        .right-panel,
        .full-panel {
          flex: 1;

          h3 {
            margin: 0 0 8px 0;
          }

          .mode-hint {
            margin-bottom: 12px;
          }

          .ballot-note {
            font-size: 12px;
            color: var(--el-text-color-secondary);
            margin-bottom: 8px;
            text-align: right;
          }

          .my-ballot-label {
            font-weight: 600;
            font-size: 14px;
            margin-bottom: 8px;
            border-bottom: 1px solid var(--el-border-color-lighter);
            padding-bottom: 4px;
          }
        }
      }

      .vote-item {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
        padding: 6px 8px;
        border: 1px solid var(--el-border-color-lighter);
        border-radius: 4px;
        min-height: 40px;

        &.vote-filled {
          background-color: var(--el-color-success-light-9);
          border-color: var(--el-color-success-light-5);
        }

        .vote-number {
          font-size: 13px;
          color: var(--el-text-color-secondary);
          min-width: 20px;
        }

        .vote-input {
          flex: 1;
        }

        .candidate-tag,
        .random-tag {
          flex: 1;
          display: flex;
          align-items: center;
          justify-content: space-between;
          font-size: 14px;
          font-weight: 500;

          .clear-btn {
            padding: 0;
            min-height: unset;
          }
        }
      }

      .ballot-warning {
        margin: 16px 0;
      }

      .submit-actions {
        margin-top: 16px;

        .submit-btn {
          width: 100%;
        }
      }
    }
  }
}
</style>
