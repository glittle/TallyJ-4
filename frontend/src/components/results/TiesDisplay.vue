<template>
  <div class="ties-display">
    <el-empty v-if="ties.length === 0" :description="$t('results.noTies')" />
    
    <div v-else>
      <el-alert
        v-if="hasRequiredTieBreaks"
        :title="$t('results.tieBreakAlert')"
        type="warning"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <el-card
        v-for="tie in ties"
        :key="tie.tieBreakGroup"
        class="tie-card"
        :class="{ 'tie-break-required': tie.tieBreakRequired }"
      >
        <template #header>
          <div class="tie-header">
            <span>
              {{ $t('results.tieGroup', { group: tie.tieBreakGroup }) }}
              - {{ tie.voteCount }} {{ $t('results.votes') }}
            </span>
            <el-tag v-if="tie.tieBreakRequired" type="danger">
              {{ $t('results.tieBreakRequired') }}
            </el-tag>
          </div>
        </template>

        <div class="tie-content">
          <div class="tie-info">
            <strong>{{ $t('results.section') }}:</strong>
            <el-tag :type="getSectionType(tie.section)" style="margin-left: 10px;">
              {{ getSectionLabel(tie.section) }}
            </el-tag>
          </div>

          <div class="candidates-list">
            <strong>{{ $t('results.tiedCandidates') }}:</strong>
            <ul>
              <li v-for="(name, index) in tie.candidateNames" :key="index">
                {{ name }}
              </li>
            </ul>
          </div>

          <el-alert
            v-if="tie.tieBreakRequired"
            :title="$t('results.tieBreakMessage')"
            type="info"
            :closable="false"
          />
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { TieInfoDto } from '../../types';

const props = defineProps<{
  ties: TieInfoDto[];
}>();

const { t } = useI18n();

const hasRequiredTieBreaks = computed(() => 
  props.ties.some(tie => tie.tieBreakRequired)
);

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

<style scoped>
.ties-display {
  padding: 10px 0;
}

.tie-card {
  margin-bottom: 20px;
}

.tie-card.tie-break-required {
  border: 2px solid #f56c6c;
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

.tie-info {
  display: flex;
  align-items: center;
}

.candidates-list ul {
  margin: 10px 0 0 0;
  padding-left: 20px;
}

.candidates-list li {
  margin: 5px 0;
  color: #606266;
}
</style>
