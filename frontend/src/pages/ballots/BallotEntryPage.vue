<template>
  <div class="ballot-entry-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('ballots.entry', { code: ballot?.ballotCode })" />
        </div>
      </template>

      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else-if="ballot">
        <div class="ballot-info">
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="$t('ballots.location')">
              {{ ballot.locationName }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.status')">
              <el-tag :type="getStatusType(ballot.statusCode)">
                {{ ballot.statusCode }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.teller1')">
              {{ ballot.teller1 || '-' }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.teller2')">
              {{ ballot.teller2 || '-' }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.voteCount')">
              {{ ballot.voteCount }}
            </el-descriptions-item>
          </el-descriptions>
        </div>

        <div class="votes-section">
          <div class="section-header">
            <h3>{{ $t('ballots.votesList') }}</h3>
            <el-button type="primary" @click="showAddVote = true">
              <el-icon><Plus /></el-icon>
              {{ $t('ballots.addVote') }}
            </el-button>
          </div>

          <el-table :data="ballot.votes" style="width: 100%">
            <el-table-column prop="positionOnBallot" :label="$t('ballots.position')" width="80" />
            <el-table-column prop="personFullName" :label="$t('ballots.candidate')" min-width="200" />
            <el-table-column prop="statusCode" :label="$t('ballots.status')" width="100">
              <template #default="scope">
                <el-tag :type="getVoteStatusType(scope.row.statusCode)">
                  {{ scope.row.statusCode }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column :label="$t('common.actions')" width="120">
              <template #default="scope">
                <el-button
                  size="small"
                  type="danger"
                  @click="handleDeleteVote(scope.row)"
                >
                  {{ $t('common.delete') }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </el-card>

    <VoteFormDialog
      v-model="showAddVote"
      :ballot-guid="ballotGuid"
      :election-guid="electionGuid"
      :next-position="nextPosition"
      @success="handleVoteSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';
import { useBallotStore } from '../../stores/ballotStore';
import type { BallotDto, VoteDto } from '../../types';
import VoteFormDialog from '../../components/ballots/VoteFormDialog.vue';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const ballotStore = useBallotStore();

const electionGuid = route.params.id as string;
const ballotGuid = route.params.ballotId as string;
const showAddVote = ref(false);

const loading = computed(() => ballotStore.loading);
const ballot = computed(() => ballotStore.currentBallot);

const nextPosition = computed(() => {
  if (!ballot.value?.votes.length) return 1;
  return Math.max(...ballot.value.votes.map(v => v.positionOnBallot)) + 1;
});

onMounted(async () => {
  try {
    await ballotStore.initializeSignalR();
    await ballotStore.joinElection(electionGuid);
    await ballotStore.fetchBallotById(ballotGuid);
  } catch (error) {
    ElMessage.error(t('ballots.loadError'));
  }
});

onUnmounted(async () => {
  try {
    await ballotStore.leaveElection(electionGuid);
  } catch (error) {
    console.error('Failed to leave election group for ballot entry:', error);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}/ballots`);
}

async function handleDeleteVote(vote: VoteDto) {
  try {
    await ElMessageBox.confirm(
      t('ballots.deleteVoteConfirm'),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    );

    await ballotStore.deleteVote(vote.ballotGuid, vote.positionOnBallot);
    ElMessage.success(t('ballots.deleteVoteSuccess'));
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('ballots.deleteVoteError'));
    }
  }
}

function handleVoteSuccess() {
  showAddVote.value = false;
  ballotStore.fetchBallotById(ballotGuid);
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'Ok': 'success',
    'Review': 'warning',
    'Spoiled': 'danger'
  };
  return typeMap[status] || '';
}

function getVoteStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'Ok': 'success',
    'Unreadable': 'warning',
    'Invalid': 'danger'
  };
  return typeMap[status] || '';
}
</script>

<style lang="less">
.ballot-entry-page {
  max-width: 1200px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.ballot-info {
  margin-bottom: 30px;
}

.votes-section {
  margin-top: 20px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.section-header h3 {
  margin: 0;
  font-size: 18px;
  color: #303133;
}

.loading-container {
  padding: 40px;
}
</style>