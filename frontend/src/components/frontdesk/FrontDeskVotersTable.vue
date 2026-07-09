<script setup lang="ts">
import type { FrontDeskVoterDto } from "@/types/FrontDesk";
import {
  ElAutoResizer,
  ElButton,
  ElTableV2,
  ElTag,
  type Column,
  type TableV2Instance,
} from "element-plus";
import { computed, h, nextTick, ref } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  voters: FrontDeskVoterDto[];
  loading: boolean;
  tableHeight: number;
  selectedIndex: number;
  /** Version bumped when highlight set changes so row classes re-render. */
  rowHighlightVersion: number;
  highlightedPersonGuids: ReadonlySet<string>;
  electionFlags: string[];
  enableEnvelopeNumbers: boolean;
  hasActiveTeller: boolean;
  columnWidths: {
    fullName: number;
    method: number;
    bahaiId: number;
    area: number;
    flags: number;
    time: number;
    envNum: number;
  };
}>();

const emit = defineEmits<{
  rowClick: [voter: FrontDeskVoterDto];
  openEnvelope: [voter: FrontDeskVoterDto];
}>();

const { t } = useI18n();
const tableRef = ref<TableV2Instance | null>(null);

/** Force virtual rows to re-evaluate class names when selection/highlight changes. */
const tableData = computed(() => {
  void props.rowHighlightVersion;
  void props.highlightedPersonGuids.size;
  void props.selectedIndex;
  return props.voters.slice();
});

function getVotingMethodLabel(method?: string): string {
  switch (method) {
    case "I":
      return t("frontDesk.votingMethod.inPerson");
    case "M":
      return t("frontDesk.votingMethod.mail");
    case "O":
      return t("frontDesk.votingMethod.online");
    case "C":
      return t("frontDesk.votingMethod.callIn");
    default:
      return method ?? t("frontDesk.common.dash");
  }
}

function getVotingMethodTagType(
  method: string,
): "success" | "info" | "primary" | "warning" {
  switch (method) {
    case "I":
      return "success";
    case "M":
      return "info";
    case "O":
      return "primary";
    case "C":
      return "warning";
    default:
      return "info";
  }
}

function getFlagAbbr(flag: string): string {
  return flag
    .split(" ")
    .map((word) => word[0])
    .join("")
    .toUpperCase()
    .slice(0, 3);
}

function hasFlag(voter: FrontDeskVoterDto, flag: string): boolean {
  if (!voter.flags) {
    return false;
  }
  return voter.flags
    .split(",")
    .map((f) => f.trim())
    .includes(flag);
}

function formatTimeShort(time?: string): string {
  if (!time) {
    return "";
  }
  return new Date(time).toLocaleTimeString();
}

function getRowClassName({
  rowData,
  rowIndex,
}: {
  rowData: FrontDeskVoterDto;
  rowIndex: number;
}) {
  const classes: string[] = [];
  if (rowIndex === props.selectedIndex) {
    classes.push("selected-row");
  }
  if (props.highlightedPersonGuids.has(rowData.personGuid)) {
    classes.push("recently-updated-row");
  }
  return classes.join(" ");
}

const rowEventHandlers = {
  onClick: ({ rowData }: { rowData: FrontDeskVoterDto }) => {
    emit("rowClick", rowData);
  },
};

const columns = computed<Column<FrontDeskVoterDto>[]>(() => {
  const widths = props.columnWidths;
  const cols: Column<FrontDeskVoterDto>[] = [
    {
      key: "fullName",
      dataKey: "fullName",
      title: t("frontDesk.table.name"),
      width: widths.fullName,
    },
    {
      key: "method",
      title: t("frontDesk.table.method"),
      width: widths.method,
      cellRenderer: ({ rowData }) => {
        if (!rowData.votingMethod) {
          return h("span", t("frontDesk.common.dash"));
        }
        return h(
          ElTag,
          { type: getVotingMethodTagType(rowData.votingMethod) },
          { default: () => getVotingMethodLabel(rowData.votingMethod) },
        );
      },
    },
    {
      key: "bahaiId",
      dataKey: "bahaiId",
      title: t("frontDesk.table.bahaiId"),
      width: widths.bahaiId,
    },
    {
      key: "area",
      dataKey: "area",
      title: t("frontDesk.table.area"),
      width: widths.area,
    },
  ];

  if (props.enableEnvelopeNumbers) {
    cols.push({
      key: "envNum",
      title: t("frontDesk.table.envNum"),
      width: widths.envNum,
      align: "center",
      cellRenderer: ({ rowData }) =>
        h(
          ElButton,
          {
            link: true,
            type: "primary",
            size: "small",
            disabled: !props.hasActiveTeller,
            onClick: (event: MouseEvent) => {
              event.stopPropagation();
              emit("openEnvelope", rowData);
            },
          },
          {
            default: () =>
              rowData.envNum !== null && rowData.envNum !== undefined
                ? String(rowData.envNum)
                : t("frontDesk.envelope.set"),
          },
        ),
    });
  }

  if (props.electionFlags.length > 0) {
    cols.push({
      key: "flags",
      title: t("frontDesk.table.flags"),
      width: widths.flags,
      cellRenderer: ({ rowData }) => {
        if (!rowData.flags) {
          return h("span", t("frontDesk.common.dash"));
        }
        const activeFlags = props.electionFlags.filter((flag) =>
          hasFlag(rowData, flag),
        );
        if (activeFlags.length === 0) {
          return h("span", t("frontDesk.common.dash"));
        }
        return h(
          "div",
          { class: "front-desk-flag-tags" },
          activeFlags.map((flag) =>
            h(
              ElTag,
              { key: flag, type: "success", class: "flag-tag" },
              { default: () => getFlagAbbr(flag) },
            ),
          ),
        );
      },
    });
  }

  cols.push({
    key: "time",
    title: t("frontDesk.table.time"),
    width: widths.time,
    cellRenderer: ({ rowData }) => {
      if (!rowData.registrationTime) {
        return h("span", t("frontDesk.common.dash"));
      }
      return h("span", formatTimeShort(rowData.registrationTime));
    },
  });

  return cols;
});

function scrollToSelectedRow(rowIndex: number) {
  nextTick(() => {
    if (rowIndex >= 0 && tableRef.value) {
      tableRef.value.scrollToRow(rowIndex, "smart");
    }
  });
}

defineExpose({
  scrollToSelectedRow,
});
</script>

<template>
  <div class="front-desk-voters-table" :style="{ height: `${tableHeight}px` }">
    <el-auto-resizer>
      <template #default="{ height, width }">
        <el-table-v2
          ref="tableRef"
          v-loading="loading"
          class="front-desk-table-v2"
          :columns="columns"
          :data="tableData"
          :width="width"
          :height="height"
          row-key="personGuid"
          :row-class="getRowClassName"
          :row-event-handlers="rowEventHandlers"
          :header-height="40"
          :row-height="48"
          scrollbar-always-on
          fixed
        />
      </template>
    </el-auto-resizer>
  </div>
</template>

<style lang="less">
.front-desk-voters-table {
  width: 100%;
  min-height: 200px;

  .front-desk-table-v2 {
    --el-table-border-color: var(--el-border-color-lighter);
    --el-table-header-bg-color: var(--el-fill-color-blank);
    --el-table-header-text-color: var(--el-text-color-secondary);
    font-size: var(--font-size-sm);

    .el-table-v2__header-cell {
      font-weight: var(--font-weight-medium);
      font-size: var(--font-size-sm);
      text-transform: uppercase;
      letter-spacing: 0.02em;
    }

    .el-table-v2__row {
      cursor: pointer;
    }

    .el-table-v2__row.selected-row {
      background-color: var(--color-frontdesk-row-selected-bg) !important;
      color: var(--color-frontdesk-row-selected-text) !important;

      .el-table-v2__row-cell {
        background-color: transparent;
        color: inherit;
      }
    }

    .el-table-v2__row.recently-updated-row {
      animation: front-desk-row-highlight-fade 2s ease-out forwards;
    }

    .el-table-v2__row.selected-row.recently-updated-row {
      animation: front-desk-row-highlight-fade-selected 2s ease-out forwards;
    }
  }

  .front-desk-flag-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    align-items: center;
  }

  .flag-tag {
    margin-right: 0;
  }
}

@keyframes front-desk-row-highlight-fade {
  0%,
  70% {
    background-color: color-mix(
      in srgb,
      var(--color-frontdesk-row-highlight) 35%,
      var(--el-bg-color)
    ) !important;
  }
  100% {
    background-color: transparent !important;
  }
}

@keyframes front-desk-row-highlight-fade-selected {
  0%,
  70% {
    background-color: color-mix(
      in srgb,
      var(--color-frontdesk-row-highlight) 35%,
      var(--el-bg-color)
    ) !important;
  }
  100% {
    background-color: var(--color-frontdesk-row-selected-bg) !important;
  }
}
</style>
