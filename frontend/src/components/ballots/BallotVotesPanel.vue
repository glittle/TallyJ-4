<script setup lang="ts">
import type { VoteDto } from "@/types/Vote";
import {
  hasDisplayableRawVote,
  isUnresolvedRawVote,
  parseOnlineVoteRaw,
  rawVoteDisplayName,
} from "@/utils/onlineVoteRaw";
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
  targetedVoteRowId: number | null;
  canRemoveVotes?: boolean;
  canSelectAnyVote?: boolean;
  isPersistedVote: (vote: VoteDto | null | undefined) => boolean;
}>();

const emit = defineEmits<{
  remove: [positionOnBallot: number];
  "drag-start": [index: number];
  "drag-over": [event: DragEvent, index: number];
  drop: [index: number];
  "drag-end": [];
  "select-target": [vote: VoteDto];
  "clear-target": [];
  find: [vote: VoteDto];
}>();

function rawParts(vote: VoteDto | null) {
  return parseOnlineVoteRaw(vote?.onlineVoteRaw);
}

function rawName(vote: VoteDto | null) {
  const raw = rawParts(vote);
  if (!raw) {
    return "";
  }
  return raw.otherInfo.trim() || rawVoteDisplayName(raw);
}

function canFindRawName(vote: VoteDto | null) {
  const raw = rawParts(vote);
  return !!(raw?.first || raw?.last);
}
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
          // Interaction only while reorder is allowed; handle stays visible on persisted votes
          'is-draggable':
            canReorderVotes && isPersistedVote(vote) && !reorderingVotes,
          'is-raw': hasDisplayableRawVote(vote),
          'is-raw-unresolved': isUnresolvedRawVote(vote),
          'is-raw-target':
            vote &&
            targetedVoteRowId !== null &&
            vote.rowId === targetedVoteRowId,
        }"
        :draggable="
          canReorderVotes && isPersistedVote(vote) && !reorderingVotes
        "
        @click="
          vote && (hasDisplayableRawVote(vote) || canSelectAnyVote)
            ? emit('select-target', vote)
            : emit('clear-target')
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
              v-if="isPersistedVote(vote) && canRemoveVotes !== false"
              class="drag-handle"
              :class="{
                'is-inactive': !canReorderVotes || reorderingVotes,
              }"
              :title="$t('ballots.dragToReorder')"
            >
              <el-icon><Rank /></el-icon>
            </span>
            <div class="vote-name-block">
              <div v-if="rawName(vote)" class="raw-vote">
                <el-button
                  v-if="canFindRawName(vote) && isUnresolvedRawVote(vote)"
                  class="raw-find-btn"
                  size="small"
                  type="primary"
                  :title="$t('ballots.findRawNameHint')"
                  @click.stop="emit('find', vote)"
                >
                  {{ $t("ballots.findRawName") }}
                </el-button>
                <span class="raw-name">{{ rawName(vote) }}</span>
              </div>
              <span
                v-if="
                  !isUnresolvedRawVote(vote) &&
                  (vote.personFullName || isVoteDtoSpoiled(vote))
                "
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
                v-if="canFindRawName(vote) && !isUnresolvedRawVote(vote)"
                class="raw-find-btn"
                size="small"
                :title="$t('ballots.findRawNameHint')"
                @click.stop="emit('find', vote)"
              >
                {{ $t("ballots.changeRawName") }}
              </el-button>
              <el-button
                v-if="canRemoveVotes !== false"
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
      <p
        v-if="
          canRemoveVotes !== false &&
          votes.some((vote) => isPersistedVote(vote))
        "
        class="votes-drag-hint"
      >
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

    // Mix onto --el-bg-color so the tint is pale on white and dark on navy.
    // EP *-light-9 stays mint/peach in dark while --el-text-color-* goes
    // near-white (Glen UAT: light-on-light).
    &.has-vote {
      background-color: color-mix(
        in srgb,
        var(--el-color-success) 18%,
        var(--el-bg-color)
      );
      border: 1px solid
        color-mix(in srgb, var(--el-color-success) 40%, var(--el-bg-color));
      color: var(--color-text-primary);
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

    // v3 used a saturated peach (#f1b787) so unresolved lines jump out next to
    // matched green. Warning stripe + Find is the mark; no text chip.
    &.is-raw-unresolved {
      background-color: color-mix(
        in srgb,
        var(--el-color-warning) 28%,
        var(--el-bg-color)
      );
      border: 2px solid var(--el-color-warning);
      box-shadow: inset 4px 0 0 var(--el-color-warning);
      color: var(--color-text-primary);

      .raw-name {
        font-weight: 600;
      }
    }

    &.is-raw:not(.is-raw-unresolved) {
      background-color: color-mix(
        in srgb,
        var(--el-color-success) 18%,
        var(--el-bg-color)
      );

      .vote-name:not(.is-spoiled) {
        color: var(--el-color-warning-dark);
      }
    }

    &.is-raw-target {
      border-color: var(--el-color-success);
      box-shadow: inset 3px 0 0 var(--el-color-success);
    }

    &.is-raw-target.is-raw-unresolved {
      background-color: color-mix(
        in srgb,
        var(--el-color-warning) 36%,
        var(--el-bg-color)
      );
      border-color: var(--el-color-warning-dark);
      box-shadow: inset 4px 0 0 var(--el-color-warning-dark);
    }

    .vote-position {
      width: 24px;
      text-align: right;
      color: var(--el-text-color-secondary);
      font-size: var(--el-font-size-small);
    }

    &.has-vote .vote-position,
    &.is-raw-unresolved .vote-position,
    &.has-vote .raw-name,
    &.is-raw-unresolved .raw-name,
    &.has-vote .drag-handle {
      color: inherit;
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

        // Keep layout stable while a new vote is saving (reorder disabled)
        &.is-inactive {
          opacity: 0.35;
          cursor: default;
        }
      }

      .vote-name-block {
        display: flex;
        flex-direction: column;
        gap: 2px;
        margin-right: auto;
        margin-left: 10px;
        min-width: 0;
      }

      .raw-vote {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 6px 8px;
      }

      .raw-find-btn {
        flex: 0 0 auto;

        &.el-button--primary {
          color: #fff;
          --el-button-text-color: #fff;
          --el-button-hover-text-color: #fff;
          --el-button-active-text-color: #fff;
        }
      }

      .raw-name {
        font-weight: 400;
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

        .el-button:not(.raw-find-btn) {
          opacity: 0.65;
        }

        .el-button:not(.raw-find-btn):hover {
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
