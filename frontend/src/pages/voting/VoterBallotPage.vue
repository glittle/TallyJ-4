<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElCard, ElForm, ElFormItem, ElAutocomplete, ElButton, ElAlert, ElEmpty } from 'element-plus';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { useNotifications } from '../../composables/useNotifications';
import type { OnlineCandidate, OnlineVote } from '../../types';

const router = useRouter();
const route = useRoute();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccess, showError } = useNotifications();

const electionGuid = ref(route.params.electionId as string);
const loading = ref(false);
const submitting = ref(false);
const votes = ref<Array<{ position: number; candidate: OnlineCandidate | null; searchText: string }>>([]);

onMounted(async () => {
  if (!onlineVotingStore.voterId) {
    showError('Please authenticate first');
    router.push({ name: 'voter-auth', query: { election: electionGuid.value } });
    return;
  }

  await loadElectionData();
});

async function loadElectionData() {
  try {
    loading.value = true;
    
    const [electionInfo, voteStatus] = await Promise.all([
      onlineVotingStore.loadElectionInfo(electionGuid.value),
      onlineVotingStore.checkVoteStatus(electionGuid.value, onlineVotingStore.voterId!)
    ]);

    if (voteStatus.hasVoted) {
      showError(voteStatus.message || 'You have already voted');
      router.push({ name: 'voter-confirmation' });
      return;
    }

    if (!electionInfo.isOpen) {
      showError('Online voting is not currently open for this election');
      return;
    }

    await onlineVotingStore.loadCandidates(electionGuid.value);

    const numToElect = electionInfo.numberToElect || 9;
    votes.value = Array.from({ length: numToElect }, (_, i) => ({
      position: i + 1,
      candidate: null,
      searchText: ''
    }));
  } catch (error) {
    console.error('Error loading election data:', error);
  } finally {
    loading.value = false;
  }
}

const candidateOptions = computed(() => {
  return onlineVotingStore.candidates.map(c => ({
    value: c.fullName,
    candidate: c
  }));
});

function handleCandidateSelect(position: number, value: string) {
  const voteSlot = votes.value.find(v => v.position === position);
  if (!voteSlot) return;

  const option = candidateOptions.value.find(opt => opt.value === value);
  if (option) {
    voteSlot.candidate = option.candidate;
    voteSlot.searchText = value;
  }
}

function clearVote(position: number) {
  const voteSlot = votes.value.find(v => v.position === position);
  if (voteSlot) {
    voteSlot.candidate = null;
    voteSlot.searchText = '';
  }
}

const canSubmit = computed(() => {
  return votes.value.some(v => v.candidate !== null);
});

const duplicateVotes = computed(() => {
  const selectedCandidates = votes.value
    .filter(v => v.candidate !== null)
    .map(v => v.candidate!.personGuid);
  
  const duplicates = selectedCandidates.filter((guid, index) => 
    selectedCandidates.indexOf(guid) !== index
  );
  
  return duplicates.length > 0;
});

async function handleSubmit() {
  if (duplicateVotes.value) {
    showError('You cannot vote for the same candidate multiple times');
    return;
  }

  try {
    submitting.value = true;

    const onlineVotes: OnlineVote[] = votes.value
      .filter(v => v.candidate !== null)
      .map(v => ({
        personGuid: v.candidate!.personGuid,
        voteName: v.candidate!.fullName,
        positionOnBallot: v.position
      }));

    await onlineVotingStore.submitBallot(electionGuid.value, {
      electionGuid: electionGuid.value,
      voterId: onlineVotingStore.voterId!,
      votes: onlineVotes
    });

    showSuccess('Ballot submitted successfully!');
    router.push({ name: 'voter-confirmation' });
  } catch (error) {
    console.error('Error submitting ballot:', error);
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="voter-ballot-page">
    <div class="ballot-container">
      <ElCard v-if="loading">
        <div style="text-align: center; padding: 40px;">Loading election information...</div>
      </ElCard>

      <ElCard v-else-if="onlineVotingStore.electionInfo" class="ballot-card">
        <template #header>
          <div class="card-header">
            <h2>{{ onlineVotingStore.electionInfo.name }}</h2>
            <p v-if="onlineVotingStore.electionInfo.dateOfElection">
              Date: {{ new Date(onlineVotingStore.electionInfo.dateOfElection).toLocaleDateString() }}
            </p>
            <p v-if="onlineVotingStore.electionInfo.convenor">
              Convenor: {{ onlineVotingStore.electionInfo.convenor }}
            </p>
          </div>
        </template>

        <ElAlert 
          v-if="duplicateVotes" 
          type="error" 
          :closable="false"
          style="margin-bottom: 20px"
        >
          You have selected the same candidate multiple times. Please review your ballot.
        </ElAlert>

        <ElAlert 
          type="info" 
          :closable="false"
          style="margin-bottom: 20px"
        >
          {{ onlineVotingStore.electionInfo.instructions }}
        </ElAlert>

        <ElForm @submit.prevent="handleSubmit">
          <div 
            v-for="vote in votes" 
            :key="vote.position"
            class="vote-item"
          >
            <ElFormItem :label="`${vote.position}.`">
              <div class="vote-input-wrapper">
                <ElAutocomplete
                  v-model="vote.searchText"
                  :fetch-suggestions="(queryString: string, cb: Function) => {
                    const results = queryString
                      ? candidateOptions.filter(opt => 
                          opt.value.toLowerCase().includes(queryString.toLowerCase())
                        )
                      : candidateOptions;
                    cb(results);
                  }"
                  placeholder="Search for a candidate"
                  @select="(item: any) => handleCandidateSelect(vote.position, item.value)"
                  clearable
                  style="flex: 1"
                  size="large"
                />
                <ElButton 
                  v-if="vote.candidate"
                  @click="clearVote(vote.position)"
                  type="danger"
                  text
                >
                  Clear
                </ElButton>
              </div>
              <div v-if="vote.candidate" class="candidate-info">
                <span class="candidate-name">{{ vote.candidate.fullName }}</span>
                <span v-if="vote.candidate.area" class="candidate-detail">{{ vote.candidate.area }}</span>
              </div>
            </ElFormItem>
          </div>

          <ElAlert 
            type="warning" 
            :closable="false"
            style="margin: 20px 0"
          >
            ⚠️ You can only vote once! Please review your ballot carefully before submitting.
          </ElAlert>

          <div class="submit-actions">
            <ElButton 
              type="primary" 
              native-type="submit"
              :loading="submitting"
              :disabled="!canSubmit || duplicateVotes"
              size="large"
              style="width: 100%"
            >
              Submit Ballot
            </ElButton>
          </div>
        </ElForm>
      </ElCard>

      <ElCard v-else>
        <ElEmpty description="Election not found" />
      </ElCard>
    </div>
  </div>
</template>

<style lang="less" scoped>
.voter-ballot-page {
  min-height: calc(100vh - 100px);
  padding: 20px;
}

.ballot-container {
  max-width: 800px;
  margin: 0 auto;
}

.ballot-card {
  .card-header {
    h2 {
      margin: 0 0 10px 0;
      color: var(--el-color-primary);
    }
    
    p {
      margin: 5px 0;
      color: var(--el-text-color-secondary);
      font-size: 14px;
    }
  }
}

.vote-item {
  margin-bottom: 20px;

  .vote-input-wrapper {
    display: flex;
    gap: 10px;
    align-items: center;
  }

  .candidate-info {
    display: flex;
    flex-direction: column;
    margin-top: 5px;
    padding: 8px 12px;
    background-color: var(--el-fill-color-light);
    border-radius: 4px;

    .candidate-name {
      font-weight: 500;
      color: var(--el-text-color-primary);
    }

    .candidate-detail {
      font-size: 12px;
      color: var(--el-text-color-secondary);
      margin-top: 2px;
    }
  }
}

.submit-actions {
  margin-top: 30px;
}
</style>
