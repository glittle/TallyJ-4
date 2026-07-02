<script setup lang="ts">
import { CircleCheck } from "@element-plus/icons-vue";
import { ElAutoResizer, ElTableV2, ElButton, ElIcon } from "element-plus";
import { useI18n } from "vue-i18n";
import { computed, h } from "vue";
import type { PersonListDto } from "../../types";
import type { Column } from "element-plus";

const { t } = useI18n();

defineProps<{
  people: PersonListDto[];
  loading: boolean;
  tableHeight: number;
}>();

const emit = defineEmits<{
  edit: [person: PersonListDto];
}>();

const columns = computed<Column<any>[]>(() => [
  {
    key: "fullName",
    dataKey: "fullName",
    title: t("people.fullName"),
    width: 220,
    sortable: true,
    cellRenderer: ({ rowData }) =>
      h(
        ElButton,
        {
          type: "primary",
          link: true,
          onClick: () => emit("edit", rowData),
        },
        { default: () => rowData.fullName },
      ),
  },
  {
    key: "eligibility",
    title: t("eligibility.label"),
    width: 200,
    align: "center",
    cellRenderer: ({ rowData }) => {
      if (!rowData.ineligibleReasonCode) {
        return h(
          ElIcon,
          { color: "#67c23a", size: 18 },
          {
            default: () => h(CircleCheck),
          },
        );
      } else {
        return h("span", {}, t(`eligibility.${rowData.ineligibleReasonCode}`));
      }
    },
  },
  {
    key: "email",
    dataKey: "email",
    title: t("people.email"),
    width: 200,
  },
  {
    key: "phone",
    dataKey: "phone",
    title: t("people.phone"),
    width: 130,
  },
  {
    key: "area",
    dataKey: "area",
    title: t("people.area"),
    width: 120,
  },
]);
</script>

<template>
  <div class="people-table" :style="{ height: `${tableHeight}px` }">
    <el-auto-resizer>
      <template #default="{ height, width }">
        <el-table-v2
          v-loading="loading"
          :columns="columns"
          :data="people"
          :width="width"
          :height="height"
          fixed
        />
      </template>
    </el-auto-resizer>
  </div>
</template>

<style lang="less">
.people-table {
  width: 100%;
  min-height: 200px;
}
</style>
