<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNotifications } from '@/composables/useNotifications';
import { usePeopleStore } from '@/stores/peopleStore';
import VoteEntryRow from './VoteEntryRow.vue';
import type { BallotDto } from '@/types/Ballot';
import type { VoteDto, CreateVoteDto } from '@/types/Vote';

const props = defineProps<{
  electionGuid: string;
  ballot: BallotDto;
  maxVotes: number;
}>();

const emit = defineEmits<{
  'vote-added': [vote: VoteDto];
  'vote-removed': [positionOnBallot: number];
  'ballot-saved': [];
}>();

const { t } = useI18n();
const peopleStore = usePeopleStore();
const { showWarningMessage, showErrorMessage } = useNotifications();

const votes = ref<(VoteDto | null)[]>([]);
const voteRowRefs = ref<InstanceType<typeof VoteEntryRow>[]>([]);
const cacheLoading = ref(false);
const cacheError = ref(false);

const votesEntered = computed(() => {
  return votes.value.filter(v => v !== null && v.personGuid).length;
});

const duplicatePersonGuids = computed(() => {
  const personGuids = votes.value
    .filter((v): v is VoteDto => v !== null && !!v.personGuid)
    .map(v => v.personGuid!);

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

watch(() => props.ballot.votes, (newVotes) => {
  initializeVotes(newVotes);
}, { immediate: true });

function initializeVotes(ballotVotes: VoteDto[]) {
  const voteArray: (VoteDto | null)[] = [];

  for (let i = 1; i <= props.maxVotes; i++) {
    const existingVote = ballotVotes.find(v => v.positionOnBallot === i);
    voteArray.push(existingVote || null);
  }

  votes.value = voteArray;
}

async function handleVoteSelected(vote: VoteDto, index: number) {
  vote.ballotGuid = props.ballot.ballotGuid; // ensure ballotGuid is set

  votes.value[index] = vote;

  const duplicates = duplicatePersonGuids.value;
  if (duplicates.includes(vote.personGuid!)) {
    showWarningMessage(t('ballots.duplicateWarning'));
  }

  emit('vote-added', vote);

  await nextTick();

  if (index < props.maxVotes - 1) {
    const nextRow = voteRowRefs.value[index + 1];
    if (nextRow) {
      nextRow.focusInput();
    }
  }
}

function handleVoteCleared(positionOnBallot: number) {
  const index = positionOnBallot - 1;
  votes.value[index] = null;
  emit('vote-removed', positionOnBallot);
}

function handleClearAll() {
  for (let i = 0; i < votes.value.length; i++) {
    const vote = votes.value[i];
    if (vote) {
      emit('vote-removed', i + 1);
    }
    votes.value[i] = null;
  }

  nextTick(() => {
    const firstRow = voteRowRefs.value[0];
    if (firstRow) {
      firstRow.focusInput();
    }
  });
}

onMounted(async () => {
  cacheLoading.value = true;
  cacheError.value = false;

  try {
    await peopleStore.initializeCandidateCache(props.electionGuid);
  } catch (e) {
    console.error('Failed to initialize candidate cache:', e);
    cacheError.value = true;
    showErrorMessage(t('ballots.cacheLoadError'));
  } finally {
    cacheLoading.value = false;
  }
});
</script>

<template>
  <div class="inline-ballot-entry">
    <output v-if="cacheLoading" class="inline-ballot-entry__loading" :aria-label="$t('ballots.cacheLoading')">
      <el-skeleton :rows="maxVotes" animated />
    </output>

    <div v-else-if="cacheError" class="inline-ballot-entry__error" role="alert">
      <el-alert type="error" :title="$t('ballots.cacheLoadError')" :closable="false" />
    </div>

    <div v-else class="inline-ballot-entry__content">
      <div class="inline-ballot-entry__header">
        <div class="inline-ballot-entry__status">
          <span class="inline-ballot-entry__status-text">
            {{ $t('ballots.votesEntered', { count: votesEntered, max: maxVotes }) }}
          </span>
        </div>

        <div class="inline-ballot-entry__actions">
          <el-button size="small" @click="handleClearAll" :disabled="votesEntered === 0">
            {{ $t('ballots.clearAll') }}
          </el-button>
        </div>
      </div>

      <div class="inline-ballot-entry__rows">
        <VoteEntryRow v-for="(vote, index) in votes" :key="index"
          :ref="el => { if (el) voteRowRefs[index] = el as InstanceType<typeof VoteEntryRow> }"
          :position-on-ballot="index + 1" :model-value="vote" :candidates="peopleStore.candidateCache"
          :duplicate-person-guids="duplicatePersonGuids" @vote-selected="(v) => handleVoteSelected(v, index)"
          @vote-cleared="handleVoteCleared" />
      </div>
    </div>
  </div>
</template>

<style lang="less" scoped>
.inline-ballot-entry {
  width: 100%;
  max-width: 800px;
  margin: 0 auto;

  &__loading {
    padding: var(--spacing-4, 16px);
  }

  &__error {
    padding: var(--spacing-4, 16px);
  }

  &__content {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-4, 16px);
  }

  &__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
    background-color: var(--color-gray-50, #fafafa);
    border-radius: var(--radius-md, 8px);
    border: 1px solid var(--color-gray-200, #e5e5e5);

    .dark & {
      background-color: var(--color-gray-900, #1a1a1a);
      border-color: var(--color-gray-700, #4a4a4a);
    }
  }

  &__status {
    display: flex;
    align-items: center;
    gap: var(--spacing-2, 8px);
  }

  &__status-text {
    font-size: var(--font-size-base, 16px);
    font-weight: var(--font-weight-medium, 500);
    color: var(--color-gray-700, #666);

    .dark & {
      color: var(--color-gray-300, #ccc);
    }
  }

  &__actions {
    display: flex;
    gap: var(--spacing-2, 8px);
  }

  &__rows {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-1, 4px);
    padding: var(--spacing-2, 8px);
  }
}
</style>
