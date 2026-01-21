<template>
  <div class="election-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>{{ $t('elections.list') }}</h2>
          <el-button type="primary" @click="createElection">
            <el-icon><Plus /></el-icon>
            {{ $t('elections.createNew') }}
          </el-button>
        </div>
      </template>
      
      <div class="table-container">
        <el-table :data="elections" v-loading="loading" style="width: 100%">
          <el-table-column prop="name" :label="$t('elections.name')" min-width="250" />
          <el-table-column prop="electionType" :label="$t('elections.type')" width="120" />
          <el-table-column prop="dateOfElection" :label="$t('elections.date')" width="140">
            <template #default="scope">
              {{ formatDate(scope.row.dateOfElection) }}
            </template>
          </el-table-column>
          <el-table-column prop="numberToElect" :label="$t('elections.toElect')" width="100" />
          <el-table-column prop="tallyStatus" :label="$t('elections.status')" width="120">
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row.tallyStatus)">
                {{ scope.row.tallyStatus || 'Draft' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="$t('common.actions')" width="200" fixed="right">
            <template #default="scope">
              <el-button-group>
                <el-button size="small" @click="viewElection(scope.row.electionGuid)">
                  {{ $t('common.view') }}
                </el-button>
                <el-button size="small" @click="editElection(scope.row.electionGuid)">
                  {{ $t('common.edit') }}
                </el-button>
              </el-button-group>
            </template>
          </el-table-column>
        </el-table>
        
        <div class="pagination-container">
          <el-pagination
            v-model:current-page="pagination.page"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="pagination.total"
            layout="total, sizes, prev, pager, next"
            @size-change="handleSizeChange"
            @current-change="handlePageChange"
          />
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus } from '@element-plus/icons-vue';
import { useElectionStore } from '../../stores/electionStore';
import { ElMessage } from 'element-plus';

const router = useRouter();
const { t } = useI18n();
const electionStore = useElectionStore();

const loading = computed(() => electionStore.loading);
const allElections = computed(() => electionStore.elections);

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
});

const elections = computed(() => {
  const start = (pagination.value.page - 1) * pagination.value.pageSize;
  const end = start + pagination.value.pageSize;
  pagination.value.total = allElections.value.length;
  return allElections.value.slice(start, end);
});

onMounted(async () => {
  await loadElections();
});

async function loadElections() {
  try {
    await electionStore.fetchElections();
  } catch (error) {
    ElMessage.error(t('elections.loadError'));
  }
}

function createElection() {
  router.push('/elections/create');
}

function viewElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function editElection(guid: string) {
  router.push(`/elections/${guid}/edit`);
}

function handleSizeChange() {
  loadElections();
}

function handlePageChange() {
  loadElections();
}

function formatDate(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'Draft': '',
    'Voting': 'success',
    'Tallying': 'warning',
    'Finalized': 'info'
  };
  return typeMap[status] || '';
}
</script>

<style scoped>
.election-list-page {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h2 {
  margin: 0;
  color: #303133;
}

.table-container {
  margin-top: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
