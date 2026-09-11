<script setup lang="ts">
import { Delete } from "@element-plus/icons-vue";
import {
  ElAlert,
  ElAutocomplete,
  ElButton,
  ElCard,
  ElCheckbox,
  ElDivider,
  ElEmpty,
  ElForm,
  ElFormItem,
  ElInput,
  ElTag,
} from "element-plus";
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import { resolveUserFacingApiError } from "../../utils/errorHandler";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useNotifications } from "../../composables/useNotifications";
import {
  autosaveAsDraft,
  buildOnlineVotes,
  createEmptyVoteSlots,
  isSubmittedOnlineVoteStatus,
  useVoterBallotHelpers,
  type VoteSlot,
} from "../../composables/useVoterBallot";
import { useOnlineVotingStore } from "../../stores/onlineVotingStore";
import type { OnlinePerson } from "../../types";
import { debounce } from "../../utils/debounce";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.electionId as string);
const loading = ref(false);
const submitting = ref(false);
const autosaveReady = ref(false);
/** Once Submitted, later autosaves keep isDraft false (never demote). */
const alreadySubmitted = ref(false);

const votes = ref<VoteSlot[]>([]);

const selectionMode = computed(
  () => onlineVotingStore.electionInfo?.onlineSelectionProcess ?? "A",
);

const isModeList = computed(() => selectionMode.value === "A");
const isModeRandom = computed(() => selectionMode.value === "B");
const isModeBoth = computed(() => selectionMode.value === "C");
const canChangeVote = computed(
  () => onlineVotingStore.voteStatus?.canChangeVote !== false,
);
const showVotablePeopleList = computed(
  () => isModeList.value || isModeBoth.value,
);

const {
  poolForm,
  notifyWhenProcessed,
  isEditing,
  duplicateVotes,
  duplicatePositions,
  canSubmit,
  hasAnyVote,
  takePoolFormEntry,
  addEntryToNextEmptyVote,
  poolAsVotablePeople,
  applyPriorVotes,
  poolEntries,
} = useVoterBallotHelpers(() => selectionMode.value);

const duplicateVotePositions = computed(() =>
  duplicatePositions(votes.value),
);

const allVotablePersonOptions = computed(() => {
  const official = onlineVotingStore.votablePeople.map((p) => ({
    value: p.fullName,
    person: p,
  }));
  const pool = poolAsVotablePeople().map((p) => ({
    value: p.fullName,
    person: p,
  }));
  return [...official, ...pool];
});

onMounted(async () => {
  const restored = await onlineVotingStore.restoreSession();
  if (!restored) {
    showErrorMessage(t("voting.ballot.authRequired"));
    router.push({
      name: "voter-auth",
      query: { election: electionGuid.value },
    });
    return;
  }

  await onlineVotingStore.ensureVoterHubsConnected();
  await onlineVotingStore.joinElectionBallotPresence(electionGuid.value);
  await loadElectionData();
});

onUnmounted(async () => {
  await onlineVotingStore.leaveElectionBallotPresence();
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

    if (!electionInfo.isOpen) {
      showErrorMessage(t("voting.ballot.notOpen"));
      return;
    }

    if (showVotablePeopleList.value) {
      await onlineVotingStore.loadVotablePeople(electionGuid.value);
    }

    alreadySubmitted.value = isSubmittedOnlineVoteStatus(voteStatus);

    const numToElect = electionInfo.numberToElect || 9;
    votes.value = createEmptyVoteSlots(numToElect);

    if (voteStatus.hasVoted || (voteStatus.priorVotes?.length ?? 0) > 0) {
      applyPriorVotes(votes.value, voteStatus, onlineVotingStore.votablePeople);
    }
  } catch (error) {
    console.error("Error loading election data:", error);
  } finally {
    loading.value = false;
    autosaveReady.value = true;
  }
}

function buildSubmitPayload(isDraft: boolean) {
  const onlineVotes = buildOnlineVotes(votes.value, selectionMode.value).map(
    (v) => ({
      ...v,
      personGuid:
        v.personGuid && !String(v.personGuid).startsWith("pool-")
          ? v.personGuid
          : undefined,
    }),
  );

  return {
    electionGuid: electionGuid.value,
    voterId: onlineVotingStore.voterId!,
    votes: onlineVotes,
    listPool: poolEntries.value,
    notifyWhenProcessed: notifyWhenProcessed.value,
    isDraft,
  };
}

const runAutosave = debounce(async () => {
  if (!autosaveReady.value || !canChangeVote.value || submitting.value) {
    return;
  }
  if (!hasAnyVote(votes.value)) {
    return;
  }
  if (!onlineVotingStore.voterId) {
    return;
  }

  try {
    await onlineVotingStore.submitBallot(
      electionGuid.value,
      buildSubmitPayload(autosaveAsDraft(alreadySubmitted.value)),
      { silent: true },
    );
    isEditing.value = true;
  } catch (error) {
    console.error("Silent ballot autosave failed:", error);
  }
}, 800);

function handlePersonSelect(
  position: number,
  item: { value: string; person: OnlinePerson },
) {
  const slot = votes.value.find((v) => v.position === position);
  if (!slot) {
    return;
  }
  slot.person = item.person;
  slot.searchText = item.value;
  slot.freeText = "";
  runAutosave();
}

function handleSearchInput(position: number, value: string) {
  const slot = votes.value.find((v) => v.position === position);
  if (!slot) {
    return;
  }
  if (slot.person && slot.person.fullName !== value) {
    slot.person = null;
  }
  slot.searchText = value;
  runAutosave();
}

function clearVote(position: number) {
  const slot = votes.value.find((v) => v.position === position);
  if (slot) {
    slot.person = null;
    slot.freeText = "";
    slot.searchText = "";
    runAutosave();
  }
}

function handleAddToPool() {
  const entry = takePoolFormEntry();
  if (!entry) {
    showErrorMessage(t("voting.ballot.poolNameRequired"));
    return;
  }
  const position = addEntryToNextEmptyVote(votes.value, entry);
  if (position == null) {
    showErrorMessage(t("voting.ballot.ballotFull"));
    return;
  }
  runAutosave();
}

async function handleSubmit() {
  if (!canChangeVote.value) {
    showErrorMessage(t("voting.submit.alreadyProcessed"));
    return;
  }
  if (duplicateVotes(votes.value)) {
    showErrorMessage(t("voting.ballot.duplicateError"));
    return;
  }

  try {
    submitting.value = true;

    await onlineVotingStore.submitBallot(
      electionGuid.value,
      buildSubmitPayload(false),
    );
    alreadySubmitted.value = true;

    showSuccessMessage(
      isEditing.value
        ? t("voting.ballot.resubmitSuccess")
        : t("voting.ballot.submitSuccess"),
    );
    router.push({ name: "voter-confirmation" });
  } catch (error) {
    console.error("Error submitting ballot:", error);
    showErrorMessage(
      resolveUserFacingApiError(error, t("voting.ballot.submitError")),
    );
  } finally {
    submitting.value = false;
  }
}

watch(
  () =>
    votes.value.map((v) => ({
      personGuid: v.person?.personGuid,
      searchText: v.searchText,
      freeText: v.freeText,
    })),
  () => {
    runAutosave();
  },
  { deep: true },
);

watch(notifyWhenProcessed, () => {
  runAutosave();
});

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
              <div class="header-tags">
                <ElTag type="success">{{ $t("voting.ballot.openNow") }}</ElTag>
                <ElTag v-if="isEditing" type="warning">
                  {{ $t("voting.ballot.editing") }}
                </ElTag>
              </div>
              <ElAlert
                v-if="isEditing && canChangeVote"
                type="info"
                :closable="false"
                class="header-status-alert"
              >
                {{ $t("voting.ballot.editHint") }}
              </ElAlert>
              <ElAlert
                v-else-if="!isEditing"
                type="warning"
                :closable="false"
                class="header-status-alert"
              >
                {{ $t("voting.ballot.onceWarning") }}
              </ElAlert>
            </div>
          </div>
        </template>

        <ElAlert
          v-if="onlineVotingStore.loginElsewhereNotice"
          type="warning"
          :closable="true"
          class="ballot-alert"
          @close="onlineVotingStore.dismissLoginElsewhereNotice()"
        >
          {{ $t("voting.elections.loginElsewhere") }}
        </ElAlert>

        <ElAlert
          v-if="isEditing && !canChangeVote"
          type="warning"
          :closable="false"
          class="ballot-alert"
          data-testid="ballot-processed-alert"
        >
          {{ $t("voting.status.alreadyProcessed") }}
        </ElAlert>

        <div class="ballot-body">
          <div v-if="isModeBoth" class="left-panel">
            <h3>{{ $t("voting.ballot.addToPool") }}</h3>
            <p class="panel-description">
              {{ $t("voting.ballot.addToPoolDescription") }}
            </p>
            <ElForm @submit.prevent="handleAddToPool">
              <ElFormItem :label="$t('voting.ballot.firstName')">
                <ElInput
                  v-model="poolForm.firstName"
                  :placeholder="$t('voting.ballot.firstNamePlaceholder')"
                />
              </ElFormItem>
              <ElFormItem :label="$t('voting.ballot.lastName')">
                <ElInput
                  v-model="poolForm.lastName"
                  :placeholder="$t('voting.ballot.lastNamePlaceholder')"
                />
              </ElFormItem>
              <ElFormItem
                :label="$t('voting.ballot.extraInfo')"
                label-position="top"
              >
                <ElInput
                  v-model="poolForm.otherInfo"
                  type="textarea"
                  :placeholder="$t('voting.ballot.extraInfoPlaceholder')"
                  :rows="2"
                />
              </ElFormItem>
              <ElButton type="default" native-type="submit">{{
                $t("voting.ballot.addPersonToPool")
              }}</ElButton>
            </ElForm>

            <div v-if="poolEntries.length > 0" class="pool-list">
              <h4>{{ $t("voting.ballot.yourPool") }}</h4>
              <ul>
                <li v-for="entry in poolEntries" :key="entry.fullName">
                  {{ entry.fullName }}
                </li>
              </ul>
            </div>

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

            <div class="my-ballot-label">
              {{ $t("voting.ballot.myBallot") }}
            </div>

            <ElAlert
              v-if="duplicateVotes(votes)"
              type="error"
              :closable="false"
              class="ballot-alert"
            >
              {{ $t("voting.ballot.duplicateError") }}
            </ElAlert>

            <ElForm @submit.prevent="handleSubmit">
              <div
                v-for="vote in votes"
                :key="vote.position"
                class="vote-item"
                :class="{
                  'vote-filled':
                    vote.person ||
                    (isModeRandom && vote.freeText) ||
                    (isModeBoth && (vote.searchText || vote.freeText)) ||
                    (isModeList && vote.searchText),
                  'vote-duplicate': duplicateVotePositions.has(vote.position),
                }"
              >
                <span class="vote-number">{{ vote.position }}.</span>

                <ElAutocomplete
                  v-if="showVotablePeopleList"
                  v-model="vote.searchText"
                  :fetch-suggestions="
                    (queryString: string, cb: Function) => {
                      const results = queryString
                        ? allVotablePersonOptions.filter((opt) =>
                            opt.value
                              .toLowerCase()
                              .includes(queryString.toLowerCase()),
                          )
                        : allVotablePersonOptions;
                      cb(results);
                    }
                  "
                  :placeholder="$t('voting.ballot.searchPlaceholder')"
                  clearable
                  class="vote-input"
                  size="default"
                  @select="
                    (item: any) => handlePersonSelect(vote.position, item)
                  "
                  @input="
                    (val: string) => handleSearchInput(vote.position, val)
                  "
                />

                <ElInput
                  v-else-if="isModeRandom"
                  v-model="vote.freeText"
                  :placeholder="$t('voting.ballot.namePlaceholder')"
                  class="vote-input"
                  size="default"
                  @update:model-value="runAutosave"
                />

                <span v-if="vote.person" class="person-tag">
                  {{ vote.person.fullName }}
                  <ElButton
                    text
                    size="small"
                    :icon="Delete"
                    class="clear-btn"
                    @click="clearVote(vote.position)"
                  />
                </span>
                <span
                  v-else-if="isModeRandom && vote.freeText"
                  class="random-tag"
                >
                  {{ vote.freeText }}
                  <ElButton
                    text
                    size="small"
                    :icon="Delete"
                    class="clear-btn"
                    @click="clearVote(vote.position)"
                  />
                </span>
              </div>

              <div class="notify-preference">
                <ElCheckbox v-model="notifyWhenProcessed">
                  {{ $t("voting.ballot.notifyWhenProcessed") }}
                </ElCheckbox>
              </div>

              <div class="submit-actions">
                <ElButton
                  type="primary"
                  native-type="submit"
                  :loading="submitting"
                  :disabled="!canChangeVote || !canSubmit(votes)"
                  size="large"
                  class="submit-btn"
                >
                  {{
                    isEditing
                      ? $t("voting.ballot.resubmit")
                      : $t("voting.ballot.submit")
                  }}
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
    max-width: 1190px;
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
        gap: 16px;

        .header-left {
          flex: 1;
          min-width: 0;

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

        .header-right {
          flex: 0 1 340px;
          display: flex;
          flex-direction: column;
          align-items: flex-end;
          gap: 8px;

          .header-tags {
            display: flex;
            flex-wrap: wrap;
            justify-content: flex-end;
            gap: 8px;
          }

          .header-status-alert {
            width: 100%;
            margin: 0;
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

          .pool-list {
            margin-top: 16px;

            h4 {
              margin: 0 0 8px 0;
              font-size: 13px;
            }

            ul {
              margin: 0;
              padding-left: 18px;
              font-size: 13px;
            }
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

          .notify-preference {
            margin: 12px 0;
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

        &.vote-duplicate {
          background-color: var(--el-color-danger-light-9);
          border-color: var(--el-color-danger);
        }

        .vote-number {
          font-size: 13px;
          color: var(--el-text-color-secondary);
          min-width: 20px;
        }

        .vote-input {
          flex: 1;
        }

        .person-tag,
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
