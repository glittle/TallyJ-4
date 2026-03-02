<script setup lang="ts">
import { CircleCheck } from '@element-plus/icons-vue';
import { ElAutoResizer, ElTableV2, ElCheckbox, ElButton, ElIcon, ElButtonGroup } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { computed, ref, watch, h } from 'vue';
import type { PersonListDto } from '../../types';
import type { Column } from 'element-plus';

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
  selectionChange: [selection: PersonListDto[]];
}>();

const selectedPeople = ref<PersonListDto[]>(props.selected || []);

// Watch for changes in props.selected to sync the internal state
watch(() => props.selected, (newSelected) => {
  selectedPeople.value = newSelected || [];
}, { immediate: true });

const columns = computed<Column<any>[]>(() => {
  const cols: Column<any>[] = [];

  if (props.showSelection) {
    cols.push({
      key: 'selection',
      width: 55,
      cellRenderer: ({ rowData }) => {
        const isSelected = selectedPeople.value.some(p => p.id === rowData.id);
        const onChange = (value: boolean) => {
          if (value) {
            selectedPeople.value = [...selectedPeople.value, rowData];
          } else {
            selectedPeople.value = selectedPeople.value.filter(p => p.id !== rowData.id);
          }
          emit('selectionChange', selectedPeople.value);
        };
        return h(ElCheckbox, {
          modelValue: isSelected,
          onChange: onChange
        });
      },
      headerCellRenderer: () => {
        const allSelected = props.people.length > 0 && selectedPeople.value.length === props.people.length;
        const containsChecked = selectedPeople.value.length > 0;
        const onChange = (value: boolean) => {
          if (value) {
            selectedPeople.value = [...props.people];
          } else {
            selectedPeople.value = [];
          }
          emit('selectionChange', selectedPeople.value);
        };
        return h(ElCheckbox, {
          modelValue: allSelected,
          indeterminate: containsChecked && !allSelected,
          onChange: onChange
        });
      },
    });
  }

  cols.push(
    {
      key: 'fullName',
      dataKey: 'fullName',
      title: t('people.fullName'),
      width: 200,
      sortable: true,
    },
    {
      key: 'email',
      dataKey: 'email',
      title: t('people.email'),
      width: 200,
    },
    {
      key: 'phone',
      dataKey: 'phone',
      title: t('people.phone'),
      width: 130,
    },
    {
      key: 'eligibility',
      title: t('eligibility.label'),
      width: 200,
      align: 'center',
      cellRenderer: ({ rowData }) => {
        if (!rowData.ineligibleReasonCode) {
          return h(ElIcon, { color: '#67c23a', size: 18 }, {
            default: () => h(CircleCheck)
          });
        } else {
          return h('span', {}, t(`eligibility.${rowData.ineligibleReasonCode}`));
        }
      },
    },
    {
      key: 'area',
      dataKey: 'area',
      title: t('people.area'),
      width: 120,
    },
    {
      key: 'actions',
      title: t('common.actions'),
      width: 150,
      align: 'right',
      cellRenderer: ({ rowData }) => h(ElButtonGroup, {}, {
        default: () => [
          h(ElButton, {
            size: 'small',
            onClick: () => emit('edit', rowData)
          }, { default: () => t('common.edit') }),
          h(ElButton, {
            size: 'small',
            type: 'danger',
            onClick: () => emit('delete', rowData)
          }, { default: () => t('common.delete') })
        ]
      }),
    }
  );

  return cols;
});
</script>

<template>
  <div style="height: 600px">
    <el-auto-resizer>
      <template #default="{ height, width }">
        <el-table-v2
          :columns="columns"
          :data="people"
          :width="width"
          :height="height"
          v-loading="loading"
          fixed
        />
      </template>
    </el-auto-resizer>
  </div>
</template>

<style lang="less">
.people-table {
  height: 100%;
  width: 100%;
}
</style>
