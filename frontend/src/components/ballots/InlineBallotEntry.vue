<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from "vue";
import { useI18n } from "vue-i18n";
import { useNotifications } from "@/composables/useNotifications";
import { usePeopleStore } from "@/stores/peopleStore";
import { usePersonSearch } from "@/composables/usePersonSearch";
import type { BallotDto } from "@/types/Ballot";
import type { VoteDto } from "@/types/Vote";
import type { SearchablePersonDto } from "@/types/Person";
import { Delete, WarningFilled } from "@element-plus/icons-vue";

const props = defineProps<{
  electionGuid: string;
  ballot: BallotDto;
  maxVotes: number;
}>();

const emit = defineEmits<{
  "vote-added": [vote: VoteDto];
  "vote-removed": [positionOnBallot: number];
  "ballot-saved": [];
}>();

const { t } = useI18n();
const peopleStore = usePeopleStore();
const { showWarningMessage, showErrorMessage } = useNotifications();

const votes = ref<(VoteDto | null)[]>([]);
const cacheLoading = ref(false);
const cacheError = ref(false);

const searchQuery = ref("");
const searchInputRef = ref();
const selectedSearchIndex = ref(0);
const searchResultsListRef = ref<HTMLElement | null>(null);

const candidatesRef = computed(() => peopleStore.candidateCache);
const { searchResults } = usePersonSearch(searchQuery, candidatesRef, {
  maxResults: 20,
});

watch(
  () => props.ballot.votes,
  (newVotes) => {
    const voteArray: (VoteDto | null)[] = [];
    for (let i = 1; i <= props.maxVotes; i++) {
      const existingVote = newVotes.find((v) => v.positionOnBallot === i);
      voteArray.push(existingVote || null);
    }
    votes.value = voteArray;
  },
  { immediate: true },
);

const duplicatePersonGuids = computed(() => {
  const personGuids = votes.value
    .filter((v): v is VoteDto => v !== null && !!v.personGuid)
    .map((v) => v.personGuid!);

  const duplicates: string[] = [];
  const seen = new Set<string>();

  for (const guid of personGuids) {
    if (seen.has(guid)) {
      duplicates.push(guid);
    } else {
      seen.add(guid);
    }
  }
  return duplicates;
});

function findNextEmptyPosition(): number {
  for (let i = 1; i <= props.maxVotes; i++) {
    if (!votes.value[i - 1]) return i;
  }
  return -1;
}

async function handlePersonSelected(person: SearchablePersonDto) {
  const emptyPos = findNextEmptyPosition();
  if (emptyPos === -1) {
    showWarningMessage(t("ballots.ballotFull", "Ballot is full"));
    return;
  }

  const isSpoiled = person.canReceiveVotes === false;
  const statusCode = isSpoiled ? person.ineligibleReasonCode || "X01" : "ok";

  const vote: VoteDto = {
    rowId: 0,
    ballotGuid: props.ballot.ballotGuid,
    positionOnBallot: emptyPos,
    personGuid: person.personGuid,
    personFullName: person.fullName,
    statusCode,
  };

  emit("vote-added", vote);

  searchQuery.value = "";
  selectedSearchIndex.value = 0;
  
  await nextTick();
  searchInputRef.value?.focus();
}

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === "ArrowDown") {
    e.preventDefault();
    if (selectedSearchIndex.value < searchResults.value.length - 1) {
      selectedSearchIndex.value++;
      scrollToSelected();
    }
  } else if (e.key === "ArrowUp") {
    e.preventDefault();
    if (selectedSearchIndex.value > 0) {
      selectedSearchIndex.value--;
      scrollToSelected();
    }
  } else if (e.key === "Enter") {
    e.preventDefault();
    if (searchResults.value.length > 0 && selectedSearchIndex.value >= 0 && selectedSearchIndex.value < searchResults.value.length) {
      handlePersonSelected(searchResults.value[selectedSearchIndex.value]);
    }
  } else if (e.key === "Escape") {
    e.preventDefault();
    searchQuery.value = "";
    selectedSearchIndex.value = 0;
  }
}

function scrollToSelected() {
  nextTick(() => {
    const list = searchResultsListRef.value;
    if (list) {
      const selected = list.querySelector('.is-selected') as HTMLElement;
      if (selected) {
        selected.scrollIntoView({ block: 'nearest' });
      }
    }
  });
}

watch(searchResults, () => {
  selectedSearchIndex.value = 0;
});

function handleVoteRemoved(positionOnBallot: number) {
  emit("vote-removed", positionOnBallot);
  searchInputRef.value?.focus();
}

onMounted(async () => {
  cacheLoading.value = true;
  cacheError.value = false;

  peopleStore.initializeCandidateCache(props.electionGuid)
    .then(() => {
      cacheLoading.value = false;
      nextTick(() => {
        searchInputRef.value?.focus();
      });
    })
    .catch((e) => {
      console.error("Failed to initialize candidate cache:", e);
      cacheError.value = true;
      cacheLoading.value = false;
      showErrorMessage(t("ballots.cacheLoadError"));
    });
});
</script>

<template>
  <div class="inline-ballot-entry">
    <div v-if="cacheError" class="inline-ballot-entry__error">
      <el-alert type="error" :title="$t('ballots.cacheLoadError')" :closable="false" />
    </div>

    <div v-else class="ballot-entry-layout">
      <!-- Left Panel: Search -->
      <div class="search-panel">
        <div class="search-panel-header">
          <h4>{{ $t('ballots.searchPerson', 'Search for a person:') }}</h4>
        </div>
        <div class="search-input-wrapper">
          <el-input
            ref="searchInputRef"
            v-model="searchQuery"
            @keydown="handleKeyDown"
            :placeholder="$t('ballots.searchPlaceholder', 'Type to search...')"
            clearable
            class="search-input"
          />
        </div>
        <div class="search-help">
          <small>{{ $t('ballots.searchHelp', 'Use ↑ ↓ keys to move in the list. Press Enter to add.') }}</small>
        </div>
        
        <div class="search-results" ref="searchResultsListRef">
          <div v-if="searchQuery && searchResults.length === 0" class="no-results">
            {{ $t('ballots.noMatchesFound') }}
          </div>
          <div
            v-for="(person, index) in searchResults"
            :key="person.personGuid"
            class="search-result-item"
            :class="{ 
              'is-selected': index === selectedSearchIndex,
              'is-ineligible': person.canReceiveVotes === false
            }"
            @click="handlePersonSelected(person)"
            @mouseover="selectedSearchIndex = index"
          >
            <div class="person-info">
              <span class="person-name">{{ person.fullName }}</span>
              <span v-if="person.canReceiveVotes === false" class="ineligible-badge" :title="$t('ballots.ineligible')">
                {{ person.ineligibleReasonCode }}
              </span>
            </div>
            <span v-if="person.voteCount > 0" class="vote-count-badge">
              {{ person.voteCount }}
            </span>
          </div>
        </div>
      </div>

      <!-- Right Panel: Votes -->
      <div class="votes-panel">
        <div class="votes-panel-header">
          <h4>{{ $t('ballots.namesOnBallot', 'Names on the ballot') }}</h4>
          <span class="ballot-id">{{ $t('ballots.ballotNum', { code: ballot.ballotCode }) }}</span>
        </div>
        
        <div class="votes-list">
          <div
            v-for="(vote, index) in votes"
            :key="index"
            class="vote-row"
            :class="{ 'has-vote': !!vote, 'is-duplicate': vote && duplicatePersonGuids.includes(vote.personGuid!) }"
          >
            <div class="vote-position">{{ index + 1 }}</div>
            <div class="vote-content">
              <template v-if="vote">
                <span class="vote-name" :class="{ 'is-spoiled': vote.statusCode && vote.statusCode !== 'ok' }">
                  {{ vote.personFullName }}
                </span>
                
                <div class="vote-actions">
                  <span v-if="vote.statusCode && vote.statusCode !== 'ok'" class="status-badge error">
                    {{ vote.statusCode }}
                  </span>
                  <span v-if="duplicatePersonGuids.includes(vote.personGuid!)" class="status-badge warning" :title="$t('ballots.duplicateWarning')">
                    <el-icon><WarningFilled /></el-icon>
                  </span>
                  <el-button
                    type="danger"
                    :icon="Delete"
                    circle
                    plain
                    size="small"
                    @click="handleVoteRemoved(index + 1)"
                    :aria-label="$t('common.delete')"
                  />
                </div>
              </template>
              <template v-else>
                <div class="empty-slot"></div>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.inline-ballot-entry {
  width: 100%;
  
  .ballot-entry-layout {
    display: flex;
    gap: var(--spacing-6, 24px);
    align-items: flex-start;
    
    @media (max-width: 768px) {
      flex-direction: column;
    }
  }

  .search-panel {
    flex: 1;
    min-width: 300px;
    max-width: 400px;
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color);
    border-radius: var(--el-border-radius-base);
    display: flex;
    flex-direction: column;

    .search-panel-header {
      padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
      background: var(--el-fill-color-light);
      border-bottom: 1px solid var(--el-border-color-lighter);
      
      h4 {
        margin: 0;
        font-size: var(--el-font-size-base);
        color: var(--el-text-color-regular);
      }
    }

    .search-input-wrapper {
      padding: var(--spacing-3, 12px);
    }
    
    .search-help {
      padding: 0 var(--spacing-3, 12px) var(--spacing-2, 8px);
      color: var(--el-text-color-secondary);
      font-size: var(--el-font-size-small);
    }

    .search-results {
      flex: 1;
      overflow-y: auto;
      max-height: 400px;
      border-top: 1px solid var(--el-border-color-lighter);
      
      .no-results {
        padding: var(--spacing-4, 16px);
        text-align: center;
        color: var(--el-text-color-secondary);
      }

      .search-result-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: var(--spacing-2, 8px) var(--spacing-3, 12px);
        cursor: pointer;
        border-bottom: 1px solid var(--el-border-color-lighter);
        
        &:last-child {
          border-bottom: none;
        }

        &.is-selected {
          background-color: var(--el-color-primary-light-9);
        }

        &.is-ineligible {
          .person-name {
            color: var(--el-text-color-secondary);
            text-decoration: line-through;
          }
        }

        .person-info {
          display: flex;
          align-items: center;
          gap: var(--spacing-2, 8px);
          overflow: hidden;
          
          .person-name {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }
          
          .ineligible-badge {
            background: var(--el-color-danger-light-9);
            color: var(--el-color-danger);
            font-size: 11px;
            padding: 2px 6px;
            border-radius: 4px;
            border: 1px solid var(--el-color-danger-light-5);
            font-family: monospace;
          }
        }

        .vote-count-badge {
          background: var(--el-color-success-light-9);
          color: var(--el-color-success);
          font-size: 12px;
          padding: 2px 8px;
          border-radius: 12px;
          font-weight: bold;
        }
      }
    }
  }

  .votes-panel {
    flex: 1.5;
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color);
    border-radius: var(--el-border-radius-base);

    .votes-panel-header {
      padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
      background: var(--el-fill-color-light);
      border-bottom: 1px solid var(--el-border-color-lighter);
      display: flex;
      justify-content: space-between;
      align-items: center;
      
      h4 {
        margin: 0;
        font-size: var(--el-font-size-base);
        color: var(--el-text-color-regular);
      }
      
      .ballot-id {
        font-weight: bold;
      }
    }

    .votes-list {
      padding: var(--spacing-2, 8px);
    }

    .vote-row {
      display: flex;
      align-items: center;
      gap: var(--spacing-3, 12px);
      padding: var(--spacing-1, 4px) var(--spacing-2, 8px);
      margin-bottom: var(--spacing-1, 4px);
      border-radius: var(--el-border-radius-base);
      
      &.has-vote {
        background-color: var(--el-color-success-light-9);
        border: 1px solid var(--el-color-success-light-5);
      }

      &.is-duplicate {
        background-color: var(--el-color-warning-light-9);
        border: 1px solid var(--el-color-warning-light-5);
      }

      .vote-position {
        width: 24px;
        text-align: right;
        color: var(--el-text-color-secondary);
        font-size: var(--el-font-size-small);
      }

      .vote-content {
        flex: 1;
        display: flex;
        justify-content: space-between;
        align-items: center;
        min-height: 32px;
        
        .empty-slot {
          flex: 1;
          height: 1px;
          background-color: var(--el-border-color-lighter);
          margin: auto 0;
        }

        .vote-name {
          font-weight: 500;
          
          &.is-spoiled {
            color: var(--el-color-danger);
            text-decoration: line-through;
          }
        }
        
        .vote-actions {
          display: flex;
          align-items: center;
          gap: var(--spacing-2, 8px);

          .status-badge {
            font-size: 11px;
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: bold;
            
            &.error {
              background: var(--el-color-danger-light-9);
              color: var(--el-color-danger);
              border: 1px solid var(--el-color-danger-light-5);
            }
            
            &.warning {
              color: var(--el-color-warning);
            }
          }
        }
      }
    }
  }
}
</style>
