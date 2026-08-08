<script setup lang="ts">
import type { VoteDto } from "@/types/Vote";
import { isVoteDtoSpoiled } from "@/utils/voteDtoNormalization";
import { getVoteSpoiledLabel } from "@/utils/voteSpoiledLabel";
import { Delete, Rank, WarningFilled } from "@element-plus/icons-vue";

defineProps<{
  votes: (VoteDto | null)[];
  ballotCode: string;
  canReorderVotes: boolean;
  reorderingVotes: boolean;
  dragSourceIndex: number | null;
  dragOverIndex: number | null;
  duplicatePersonGuids: string[];
  isPersistedVote: (vote: VoteDto | null | undefined) => boolean;
}>();

const emit = defineEmits<{
  remove: [positionOnBallot: number];
  "drag-start": [index: number];
  "drag-over": [event: DragEvent, index: number];
  drop: [index: number];
  "drag-end": [];
}>();
</script>

<template>
  <div class="ballot-votes-panel">
    <div class="votes-panel-header">
      <h4>{{ $t("ballots.namesOnBallot") }}</h4>
      <span class="ballot-id">{{
        $t("ballots.ballotNum", { code: ballotCode })
      }}</span>
    </div>

    <div class="votes-list">
      <div
        v-for="(vote, index) in votes"
        :key="vote?.rowId ? `vote-${vote.rowId}` : `slot-${index}`"
        class="vote-row"
        :class="{
          'has-vote': !!vote,
          'is-duplicate':
            vote && duplicatePersonGuids.includes(vote.personGuid!),
          'is-dragging': dragSourceIndex === index,
          'is-drop-target-top':
            dragOverIndex === index &&
            dragSourceIndex !== null &&
            index < dragSourceIndex,
          'is-drop-target-bottom':
            dragOverIndex === index &&
            dragSourceIndex !== null &&
            index > dragSourceIndex,
          'is-draggable': canReorderVotes && isPersistedVote(vote),
        }"
        :draggable="
          canReorderVotes && isPersistedVote(vote) && !reorderingVotes
        "
        @dragstart="emit('drag-start', index)"
        @dragover="emit('drag-over', $event, index)"
        @drop="emit('drop', index)"
        @dragend="emit('drag-end')"
      >
        <div class="vote-position">{{ index + 1 }}</div>
        <div class="vote-content">
          <template v-if="vote">
            <span
              v-if="canReorderVotes && isPersistedVote(vote)"
              class="drag-handle"
              :title="$t('ballots.dragToReorder')"
            >
              <el-icon><Rank /></el-icon>
            </span>
            <div class="vote-name-block">
              <span
                class="vote-name"
                :class="{ 'is-spoiled': isVoteDtoSpoiled(vote) }"
              >
                {{ vote.personFullName || getVoteSpoiledLabel($t, vote) }}
              </span>
              <span
                v-if="isVoteDtoSpoiled(vote) && vote.personFullName"
                class="vote-ineligible-reason"
              >
                {{ getVoteSpoiledLabel($t, vote) }}
              </span>
            </div>
            <div class="vote-actions">
              <span
                v-if="duplicatePersonGuids.includes(vote.personGuid!)"
                class="status-badge warning"
                :title="$t('ballots.duplicateWarning')"
              >
                <el-icon><WarningFilled /></el-icon>
              </span>
              <el-button
                :icon="Delete"
                circle
                plain
                size="small"
                :aria-label="$t('common.delete')"
                @click="emit('remove', index + 1)"
              />
            </div>
          </template>
          <template v-else>
            <div class="empty-slot"></div>
          </template>
        </div>
      </div>
      <p v-if="canReorderVotes" class="votes-drag-hint">
        {{ $t("ballots.dragToReorder") }}
      </p>
    </div>
  </div>
</template>

<style lang="less">
.ballot-votes-panel {
  flex: 1.5;
  max-width: 500px;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-base);

  .votes-panel-header {
    padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
    background: var(--el-fill-color-light);
    border-bottom: 1px solid var(--el-border-color-lighter);
    display: flex;
    justify-content: space-between;
    align-items: center;

    h4 {
      margin: 0;
      font-size: var(--el-font-size-base);
      color: var(--el-text-color-regular);
    }

    .ballot-id {
      font-weight: bold;
    }
  }

  .votes-drag-hint {
    margin: 0;
    padding: var(--spacing-2, 8px) var(--spacing-4, 16px);
    color: var(--el-text-color-secondary);
    font-size: var(--el-font-size-small);
  }

  .votes-list {
    padding: var(--spacing-2, 8px);
  }

  .vote-row {
    display: flex;
    align-items: center;
    gap: var(--spacing-3, 12px);
    padding: var(--spacing-1, 4px) var(--spacing-2, 8px);
    margin-bottom: var(--spacing-1, 4px);
    border-radius: var(--el-border-radius-base);

    &.has-vote {
      background-color: var(--el-color-success-light-9);
      border: 1px solid var(--el-color-success-light-5);
    }

    &.is-draggable {
      cursor: grab;
    }

    &.is-dragging {
      opacity: 0.55;
    }

    &.is-drop-target-top .vote-content {
      border-top: 2px dashed var(--el-color-primary);
    }

    &.is-drop-target-bottom .vote-content {
      border-bottom: 2px dashed var(--el-color-primary);
    }

    &.is-duplicate {
      background-color: var(--el-color-warning-light-9);
      border: 1px solid var(--el-color-warning-light-5);
    }

    .vote-position {
      width: 24px;
      text-align: right;
      color: var(--el-text-color-secondary);
      font-size: var(--el-font-size-small);
    }

    .vote-content {
      flex: 1;
      display: flex;
      justify-content: space-between;
      align-items: center;
      min-height: 32px;

      .empty-slot {
        flex: 1;
        height: 1px;
        background-color: var(--el-border-color-lighter);
        margin: auto 0;
      }

      .drag-handle {
        display: inline-flex;
        align-items: center;
        color: var(--el-text-color-secondary);
        margin-right: var(--spacing-1, 4px);
      }

      .vote-name-block {
        display: flex;
        flex-direction: column;
        gap: 2px;
        margin-right: auto;
        margin-left: 10px;
        min-width: 0;
      }

      .vote-name {
        font-weight: 500;

        &.is-spoiled {
          color: var(--el-color-danger);
          text-decoration: line-through;
        }
      }

      .vote-ineligible-reason {
        color: var(--el-color-danger);
        font-size: var(--el-font-size-small);
        line-height: 1.2;
      }

      .vote-actions {
        display: flex;
        align-items: center;
        gap: var(--spacing-2, 8px);

        .el-button {
          opacity: 0.65;
        }

        .el-button:hover {
          opacity: 1;
          background-color: var(--el-color-danger-light-9);
          color: var(--el-color-danger);
          border-color: var(--el-color-danger);
        }

        .status-badge {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          font-size: 11px;
          padding: 2px 6px;
          border-radius: 4px;
          font-weight: bold;

          &.error {
            background: var(--el-color-danger-light-9);
            color: var(--el-color-danger);
            border: 1px solid var(--el-color-danger-light-5);
          }

          &.warning {
            color: var(--el-color-warning);
          }
        }
      }
    }
  }
}
</style>
