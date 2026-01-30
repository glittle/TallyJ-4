<template>
  <div class="tie-management">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('tieManagement.title')" />
          <div class="header-actions">
            <el-button
              type="primary"
              :loading="saving"
              :disabled="!hasChanges"
              @click="saveTieCounts"
            >
              {{ $t('tieManagement.saveChanges') }}
            </el-button>
          </div>
        </div>
      </template>

      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else-if="tieDetails.length > 0">
        <el-alert
          :title="$t('tieManagement.instructions')"
          type="info"
          :closable="false"
          style="margin-bottom: 20px;"
        />

        <div v-for="tie in tieDetails" :key="tie.tieBreakGroup" class="tie-group-card">
          <el-card class="tie-card" :class="{ 'tie-break-required': tie.section === 'E' }">
            <template #header>
              <div class="tie-header">
                <span>
                  {{ $t('tieManagement.tieGroup', { group: tie.tieBreakGroup }) }}
                  - {{ $t('tieManagement.section') }}: {{ getSectionLabel(tie.section) }}
                </span>
                <el-tag v-if="tie.section === 'E'" type="danger">
                  {{ $t('tieManagement.tieBreakRequired') }}
                </el-tag>
              </div>
            </template>

            <div class="tie-content">
              <div class="instructions" v-if="tie.instructions">
                <el-alert :title="tie.instructions" type="warning" :closable="false" />
              </div>

              <div class="candidates-table">
                <el-table :data="tie.candidates" stripe style="width: 100%">
                  <el-table-column prop="fullName" :label="$t('tieManagement.candidate')" width="300" />
                  <el-table-column prop="voteCount" :label="$t('tieManagement.voteCount')" width="150" align="center" />
                  <el-table-column :label="$t('tieManagement.tieBreakCount')" width="200">
                    <template #default="scope">
                      <el-input-number
                        v-model="scope.row.tieBreakCount"
                        :min="0"
                        :max="999"
                        :precision="0"
                        controls-position="right"
                        @change="onTieBreakCountChange"
                        style="width: 120px;"
                      />
                    </template>
                  </el-table-column>
                  <el-table-column :label="$t('tieManagement.actions')" width="150">
                    <template #default="scope">
                      <el-button
                        type="primary"
                        size="small"
                        @click="clearTieBreakCount(scope.row)"
                      >
                        {{ $t('common.clear') }}
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </div>

              <div class="tie-validation" v-if="getTieValidation(tie)">
                <el-alert
                  :title="getTieValidation(tie)"
                  type="error"
                  :closable="false"
                />
              </div>
            </div>
          </el-card>
        </div>
      </div>

      <el-empty v-else :description="$t('tieManagement.noTies')" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useResultStore } from '../../stores/resultStore';
import type { TieDetailsDto, TieCandidateDto } from '../../types';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();

const electionGuid = route.params.id as string;
const tieDetails = ref<TieDetailsDto[]>([]);
const originalTieDetails = ref<TieDetailsDto[]>([]);
const loading = ref(false);
const saving = ref(false);

const hasChanges = computed(() => {
  return JSON.stringify(tieDetails.value) !== JSON.stringify(originalTieDetails.value);
});

onMounted(async () => {
  await loadTieDetails();
});

async function loadTieDetails() {
  try {
    loading.value = true;
    const details = await resultStore.fetchTieDetails(electionGuid);
    tieDetails.value = JSON.parse(JSON.stringify(details)); // Deep copy
    originalTieDetails.value = JSON.parse(JSON.stringify(details)); // Store original
  } catch (error) {
    ElMessage.error(t('tieManagement.loadError'));
  } finally {
    loading.value = false;
  }
}

async function saveTieCounts() {
  try {
    await ElMessageBox.confirm(
      t('tieManagement.confirmSave'),
      t('common.confirmation'),
      {
        confirmButtonText: t('common.yes'),
        cancelButtonText: t('common.no'),
        type: 'warning',
      }
    );

    saving.value = true;

    // Collect all tie break counts
    const counts: { personGuid: string; tieBreakCount: number }[] = [];

    tieDetails.value.forEach(tie => {
      tie.candidates.forEach(candidate => {
        if (candidate.tieBreakCount !== undefined && candidate.tieBreakCount > 0) {
          counts.push({
            personGuid: candidate.personGuid,
            tieBreakCount: candidate.tieBreakCount
          });
        }
      });
    });

    const response = await resultStore.saveTieCounts(electionGuid, counts);

    if (response.success) {
      ElMessage.success(t('tieManagement.saveSuccess'));
      await loadTieDetails(); // Reload to get updated data

      if (response.reAnalysisTriggered) {
        ElMessage.info(t('tieManagement.reAnalysisTriggered'));
      }
    } else {
      ElMessage.error(response.message || t('tieManagement.saveError'));
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('tieManagement.saveError'));
    }
  } finally {
    saving.value = false;
  }
}

function onTieBreakCountChange() {
  // Validation could be added here
}

function clearTieBreakCount(candidate: TieCandidateDto) {
  candidate.tieBreakCount = 0;
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    'E': t('results.elected'),
    'X': t('results.extra'),
    'O': t('results.other')
  };
  return labelMap[section] || section;
}

function getTieValidation(tie: TieDetailsDto): string | null {
  // Check if all candidates in elected ties have tie break counts
  if (tie.section === 'E') {
    const candidatesWithoutCount = tie.candidates.filter(c => !c.tieBreakCount || c.tieBreakCount === 0);
    if (candidatesWithoutCount.length > 0) {
      return t('tieManagement.validationRequired');
    }
  }
  return null;
}

function goBack() {
  router.push(`/elections/${electionGuid}/results`);
}
</script>

<style lang="less">
.tie-management {
  max-width: 1200px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.tie-group-card {
  margin-bottom: 20px;
}

.tie-card {
  border: 2px solid #ebeef5;
}

.tie-card.tie-break-required {
  border-color: #f56c6c;
}

.tie-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 600;
}

.tie-content {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.instructions {
  margin-bottom: 10px;
}

.candidates-table {
  margin: 15px 0;
}

.tie-validation {
  margin-top: 10px;
}

.loading-container {
  padding: 40px;
}
</style>