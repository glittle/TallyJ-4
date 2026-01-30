<template>
  <el-dialog
    :model-value="modelValue"
    :title="$t('ballots.votes', { code: ballot?.ballotCode })"
    width="800px"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <div v-if="ballot">
      <div class="ballot-info">
        <el-descriptions :column="2" border size="small">
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
        </el-descriptions>
      </div>

      <div class="votes-section">
        <div class="section-header">
          <h4>{{ $t('ballots.votesList') }}</h4>
          <el-button size="small" type="primary" @click="showAddVote = true">
            <el-icon><Plus /></el-icon>
            {{ $t('ballots.addVote') }}
          </el-button>
        </div>

        <el-table :data="ballot.votes" style="width: 100%" size="small">
          <el-table-column prop="positionOnBallot" :label="$t('ballots.position')" width="80" />
          <el-table-column prop="personFullName" :label="$t('ballots.candidate')" min-width="200" />
          <el-table-column prop="statusCode" :label="$t('ballots.status')" width="100">
            <template #default="scope">
              <el-tag size="small" :type="getVoteStatusType(scope.row.statusCode)">
                {{ scope.row.statusCode }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="$t('common.actions')" width="100">
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

    <VoteFormDialog
      v-model="showAddVote"
      :ballot-guid="ballot?.ballotGuid || ''"
      :election-guid="electionGuid"
      :next-position="nextPosition"
      @success="handleVoteSuccess"
    />
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';
import { useBallotStore } from '../../stores/ballotStore';
import type { BallotDto, VoteDto } from '../../types';
import VoteFormDialog from './VoteFormDialog.vue';

const props = defineProps<{
  modelValue: boolean;
  ballot: BallotDto | null;
  electionGuid: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();

const showAddVote = ref(false);

const nextPosition = computed(() => {
  if (!props.ballot?.votes.length) return 1;
  return Math.max(...props.ballot.votes.map(v => v.positionOnBallot)) + 1;
});

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
  if (props.ballot) {
    ballotStore.fetchBallotById(props.ballot.ballotGuid);
  }
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
.ballot-info {
  margin-bottom: 20px;
}

.votes-section {
  margin-top: 20px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.section-header h4 {
  margin: 0;
  font-size: 16px;
  color: #303133;
}
</style>
