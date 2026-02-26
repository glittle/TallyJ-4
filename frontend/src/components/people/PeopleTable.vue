<script setup lang="ts">
import { CircleCheck } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import type { PersonListDto } from '../../types';

const { t } = useI18n();

const props = defineProps<{
  people: PersonListDto[];
  loading: boolean;
  showSelection?: boolean;
  selected?: PersonListDto[];
}>();

const emit = defineEmits<{
  edit: [person: PersonListDto];
  delete: [person: PersonListDto];
  selectionChange?: [selection: PersonListDto[]];
}>();

function handleSelectionChange(selection: PersonListDto[]) {
  emit('selectionChange', selection);
}
</script>

<template>
  <el-table
    :data="people"
    v-loading="loading"
    style="width: 100%"
    height="600"
    @selection-change="handleSelectionChange"
  >
    <el-table-column v-if="showSelection" type="selection" width="55" />
    <el-table-column prop="fullName" :label="$t('people.fullName')" min-width="200" sortable />
    <el-table-column prop="email" :label="$t('people.email')" min-width="200" />
    <el-table-column prop="phone" :label="$t('people.phone')" width="130" />
    <el-table-column :label="$t('eligibility.label')" width="200" align="center">
      <template #default="scope">
        <el-icon v-if="!scope.row.ineligibleReasonCode" color="#67c23a" :size="18">
          <CircleCheck />
        </el-icon>
        <span v-else>{{ $t(`eligibility.${scope.row.ineligibleReasonCode}`) }}</span>
      </template>
    </el-table-column>
    <el-table-column prop="area" :label="$t('people.area')" width="120" />
    <el-table-column :label="$t('common.actions')" width="150" fixed="right">
      <template #default="scope">
        <el-button-group>
          <el-button size="small" @click="$emit('edit', scope.row)">
            {{ $t('common.edit') }}
          </el-button>
          <el-button size="small" type="danger" @click="$emit('delete', scope.row)">
            {{ $t('common.delete') }}
          </el-button>
        </el-button-group>
      </template>
    </el-table-column>
  </el-table>
</template>

<style lang="less">
.people-table-container {
  .people-table-container {
    height: 100%;
    width: 100%;
  }
}
</style>
