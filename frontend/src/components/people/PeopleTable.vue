<template>
  <el-table
    :data="people"
    v-loading="loading"
    style="width: 100%"
    @selection-change="handleSelectionChange"
  >
    <el-table-column v-if="showSelection" type="selection" width="55" />
    <el-table-column prop="fullName" :label="$t('people.fullName')" min-width="200" sortable />
    <el-table-column prop="email" :label="$t('people.email')" min-width="200" />
    <el-table-column prop="phone" :label="$t('people.phone')" width="130" />
    <el-table-column :label="$t('people.canVote')" width="100" align="center">
      <template #default="scope">
        <el-icon v-if="scope.row.canVote" color="#67c23a" :size="18">
          <CircleCheck />
        </el-icon>
        <el-icon v-else color="#909399" :size="18">
          <CircleClose />
        </el-icon>
      </template>
    </el-table-column>
    <el-table-column :label="$t('people.canReceiveVotes')" width="140" align="center">
      <template #default="scope">
        <el-icon v-if="scope.row.canReceiveVotes" color="#67c23a" :size="18">
          <CircleCheck />
        </el-icon>
        <el-icon v-else color="#909399" :size="18">
          <CircleClose />
        </el-icon>
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

<script setup lang="ts">
import { CircleCheck, CircleClose } from '@element-plus/icons-vue';
import type { PersonDto } from '../../types';

const props = defineProps<{
  people: PersonDto[];
  loading: boolean;
  showSelection?: boolean;
  selected?: PersonDto[];
}>();

const emit = defineEmits<{
  edit: [person: PersonDto];
  delete: [person: PersonDto];
  selectionChange?: [selection: PersonDto[]];
}>();

function handleSelectionChange(selection: PersonDto[]) {
  emit('selectionChange', selection);
}
</script>
