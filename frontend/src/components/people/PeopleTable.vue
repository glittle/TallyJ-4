<script setup lang="ts">
import { CircleCheck } from "@element-plus/icons-vue";
import {
  ElAutoResizer,
  ElTableV2,
  ElButton,
  ElCheckbox,
  ElIcon,
  ElTag,
} from "element-plus";
import { useI18n } from "vue-i18n";
import { computed, h } from "vue";
import type { PersonListDto } from "../../types";
import type { Column } from "element-plus";
import {
  phoneSmsListHint,
  phoneSmsListLabel,
  phoneSmsListTagType,
} from "@/utils/phoneOnlineVoterStatus";

const { t } = useI18n();

const props = defineProps<{
  people: PersonListDto[];
  loading: boolean;
  tableHeight: number;
  selectedGuids: string[];
}>();

const emit = defineEmits<{
  edit: [person: PersonListDto];
  "update:selectedGuids": [guids: string[]];
}>();

const selectedSet = computed(() => new Set(props.selectedGuids));

const allVisibleSelected = computed(
  () =>
    props.people.length > 0 &&
    props.people.every((person) => selectedSet.value.has(person.personGuid)),
);

const someVisibleSelected = computed(() =>
  props.people.some((person) => selectedSet.value.has(person.personGuid)),
);

function setSelected(guids: string[]) {
  emit("update:selectedGuids", guids);
}

function toggleRow(personGuid: string, selected: boolean) {
  const next = new Set(props.selectedGuids);
  if (selected) {
    next.add(personGuid);
  } else {
    next.delete(personGuid);
  }
  setSelected([...next]);
}

function toggleVisible(selected: boolean) {
  const next = new Set(props.selectedGuids);
  for (const person of props.people) {
    if (selected) {
      next.add(person.personGuid);
    } else {
      next.delete(person.personGuid);
    }
  }
  setSelected([...next]);
}

const columns = computed<Column<any>[]>(() => [
  {
    key: "selection",
    width: 50,
    cellRenderer: ({ rowData }: { rowData: PersonListDto }) =>
      h(ElCheckbox, {
        class: "people-table__select",
        modelValue: selectedSet.value.has(rowData.personGuid),
        onChange: (value: string | number | boolean) =>
          toggleRow(rowData.personGuid, value === true),
      }),
    headerCellRenderer: () =>
      h(ElCheckbox, {
        class: "people-table__select-all",
        modelValue: allVisibleSelected.value,
        indeterminate: someVisibleSelected.value && !allVisibleSelected.value,
        onChange: (value: string | number | boolean) =>
          toggleVisible(value === true),
      }),
  },
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
          class: "people-table__name",
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
    key: "sms",
    title: t("people.phoneOnlineVoter.smsColumn"),
    width: 140,
    cellRenderer: ({ rowData }: { rowData: PersonListDto }) => {
      const hint = phoneSmsListHint(rowData.phoneOnlineVoter);
      if (hint === "none") {
        return h("span", {}, "");
      }
      return h(
        ElTag,
        {
          size: "small",
          type: phoneSmsListTagType(hint),
          class: `people-table__sms people-table__sms--${hint}`,
        },
        { default: () => phoneSmsListLabel(rowData.phoneOnlineVoter, t) },
      );
    },
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
          row-key="personGuid"
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

  .people-table__name {
    --el-button-text-color: var(--color-text-link);
    --el-button-hover-text-color: var(--color-text-link-hover);
    --el-button-hover-link-text-color: var(--color-text-link-hover);
    font-weight: var(--font-weight-medium);
  }
}
</style>
