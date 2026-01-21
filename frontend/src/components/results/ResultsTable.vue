<template>
  <el-table :data="results" style="width: 100%">
    <el-table-column prop="rank" :label="$t('results.rank')" width="80" sortable />
    <el-table-column prop="fullName" :label="$t('results.candidate')" min-width="250" />
    <el-table-column prop="voteCount" :label="$t('results.votes')" width="100" align="center" sortable />
    <el-table-column prop="section" :label="$t('results.section')" width="120">
      <template #default="scope">
        <el-tag :type="getSectionType(scope.row.section)">
          {{ getSectionLabel(scope.row.section) }}
        </el-tag>
      </template>
    </el-table-column>
    <el-table-column :label="$t('results.flags')" width="150">
      <template #default="scope">
        <el-space wrap :size="5">
          <el-tag v-if="scope.row.isTied" type="warning" size="small">
            {{ $t('results.tied') }}
          </el-tag>
          <el-tag v-if="scope.row.tieBreakRequired" type="danger" size="small">
            {{ $t('results.tieBreakRequired') }}
          </el-tag>
          <el-tag v-if="scope.row.closeToNext" type="info" size="small">
            {{ $t('results.closeToNext') }}
          </el-tag>
        </el-space>
      </template>
    </el-table-column>
  </el-table>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import type { CandidateResultDto } from '../../types';

defineProps<{
  results: CandidateResultDto[];
}>();

const { t } = useI18n();

function getSectionType(section: string) {
  const typeMap: Record<string, any> = {
    'E': 'success',
    'X': 'warning',
    'O': 'info'
  };
  return typeMap[section] || '';
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    'E': t('results.elected'),
    'X': t('results.extra'),
    'O': t('results.other')
  };
  return labelMap[section] || section;
}
</script>
