<script setup lang="ts">
import { usePersonSearch } from "@/composables/usePersonSearch";
import type { SearchablePersonDto } from "@/types/Person";
import { getIneligibleReasonLabel } from "@/utils/voteSpoiledLabel";
import { computed, nextTick, ref, toRef, watch } from "vue";

/** Temporary: show strategy weight next to search results. Flip to true when debugging ranking. */
const SHOW_SEARCH_MATCH_DEBUG = false;

const props = defineProps<{
  canAddVotes: boolean;
  people: SearchablePersonDto[];
  personGuidsOnBallot: Set<string>;
}>();

const emit = defineEmits<{
  select: [person: SearchablePersonDto];
}>();

const searchQuery = ref("");
const searchInputRef = ref();
const selectedSearchIndex = ref(0);
const searchResultsListRef = ref<HTMLElement | null>(null);
const ignoreMouseHover = ref(false);

const peopleRef = toRef(props, "people");
const { searchResults } = usePersonSearch(searchQuery, peopleRef, {
  maxResults: 20,
});

const maxResultVoteCount = computed(() => {
  let max = 0;
  for (const person of searchResults.value) {
    const count = person.voteCount ?? 0;
    if (count > max) {
      max = count;
    }
  }
  return max;
});

function relativePopularityWidth(person: SearchablePersonDto): number {
  const count = person.voteCount ?? 0;
  if (count <= 0 || maxResultVoteCount.value <= 0) {
    return 0;
  }
  return Math.max((count / maxResultVoteCount.value) * 100, 8);
}

watch(searchResults, () => {
  selectedSearchIndex.value = 0;
  ignoreMouseHover.value = true;
});

function scrollToSelected() {
  nextTick(() => {
    const list = searchResultsListRef.value;
    if (list) {
      const selected = list.querySelector(".is-selected") as HTMLElement;
      if (selected) {
        selected.scrollIntoView({ block: "nearest" });
      }
    }
  });
}

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === "ArrowDown") {
    e.preventDefault();
    if (selectedSearchIndex.value < searchResults.value.length - 1) {
      selectedSearchIndex.value++;
      scrollToSelected();
    }
  } else if (e.key === "ArrowUp") {
    e.preventDefault();
    if (selectedSearchIndex.value > 0) {
      selectedSearchIndex.value--;
      scrollToSelected();
    }
  } else if (e.key === "Enter") {
    e.preventDefault();
    if (
      searchResults.value.length > 0 &&
      selectedSearchIndex.value >= 0 &&
      selectedSearchIndex.value < searchResults.value.length
    ) {
      emit("select", searchResults.value[selectedSearchIndex.value]);
    }
  } else if (e.key === "Escape") {
    e.preventDefault();
    clearSearch();
  }
}

function handleSearchListMouseMove() {
  ignoreMouseHover.value = false;
}

function handleSearchItemMouseOver(index: number) {
  if (ignoreMouseHover.value) {
    return;
  }
  selectedSearchIndex.value = index;
}

function clearSearch() {
  searchQuery.value = "";
  selectedSearchIndex.value = 0;
}

async function focus() {
  await nextTick();
  searchInputRef.value?.focus();
}

defineExpose({ focus, clearSearch });
</script>

<template>
  <div class="ballot-person-search-panel">
    <div class="search-panel-header">
      <h4>{{ $t("ballots.searchPerson") }}</h4>
    </div>
    <div class="search-input-wrapper">
      <el-input
        ref="searchInputRef"
        v-model="searchQuery"
        :placeholder="$t('ballots.searchPlaceholder')"
        :disabled="!canAddVotes"
        clearable
        class="search-input"
        @keydown="handleKeyDown"
      />
    </div>

    <div class="search-help">
      <small>{{ $t("ballots.searchHelp") }}</small>
    </div>

    <div
      ref="searchResultsListRef"
      class="search-results"
      @mousemove="handleSearchListMouseMove"
    >
      <div v-if="searchQuery && searchResults.length === 0" class="no-results">
        {{ $t("ballots.noMatchesFound") }}
      </div>
      <div
        v-for="(person, index) in searchResults"
        :key="person.personGuid"
        class="search-result-item"
        :class="{
          'is-selected': index === selectedSearchIndex,
          'is-ineligible': person.canReceiveVotes === false,
          'is-on-ballot': personGuidsOnBallot.has(person.personGuid),
        }"
        @click="emit('select', person)"
        @mouseover="handleSearchItemMouseOver(index)"
      >
        <div class="person-info">
          <div class="person-row">
            <span class="person-name"
              >{{ person.fullName }}
              <span v-if="SHOW_SEARCH_MATCH_DEBUG" class="match-weight"
                >({{ person._searchWeight ?? "?"
                }}{{
                  person._matchedStrategy ? " " + person._matchedStrategy : ""
                }})</span
              ></span
            >
            <span v-if="person.area" class="person-area">{{
              person.area
            }}</span>
          </div>
          <span
            v-if="person.canReceiveVotes === false"
            class="ineligible-badge"
            :title="$t('ballots.ineligible')"
          >
            {{
              getIneligibleReasonLabel(
                $t,
                person.ineligibleReasonCode,
                "ballots.ineligible",
              )
            }}
          </span>
          <div
            v-if="(person.voteCount ?? 0) > 0"
            class="popularity-bar"
            :style="{ width: relativePopularityWidth(person) + '%' }"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.ballot-person-search-panel {
  width: 400px;
  height: 500px;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-base);
  display: flex;
  flex-direction: column;

  .search-panel-header {
    padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
    background: var(--el-fill-color-light);
    border-bottom: 1px solid var(--el-border-color-lighter);

    h4 {
      margin: 0;
      font-size: var(--el-font-size-base);
      color: var(--el-text-color-regular);
    }
  }

  .search-input-wrapper {
    padding: var(--spacing-3, 12px);
  }

  .search-help {
    padding: 0 var(--spacing-3, 12px) var(--spacing-2, 8px);
    color: var(--el-text-color-secondary);
    font-size: var(--el-font-size-small);
  }

  .search-results {
    flex: 1;
    overflow-y: auto;
    max-height: 400px;
    border-top: 1px solid var(--el-border-color-lighter);

    .no-results {
      padding: var(--spacing-4, 16px);
      text-align: center;
      color: var(--el-text-color-secondary);
    }

    .search-result-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: var(--spacing-2, 8px) var(--spacing-3, 12px);
      cursor: pointer;
      border-bottom: 1px solid var(--el-border-color-lighter);

      &:last-child {
        border-bottom: none;
      }

      &.is-selected {
        background-color: var(--el-color-primary-light-9);
      }

      &.is-ineligible {
        .person-name {
          color: var(--el-text-color-secondary);
          text-decoration: line-through;
        }
      }

      &.is-on-ballot {
        .person-name {
          color: var(--el-color-warning);
        }
      }

      .person-info {
        display: flex;
        flex-direction: column;
        align-items: stretch;
        gap: 2px;
        overflow: hidden;
        min-width: 0;
        width: 100%;

        .person-row {
          display: flex;
          flex-wrap: wrap;
          justify-content: space-between;
          align-items: baseline;
          gap: 2px 12px;
          width: 100%;
        }

        .person-name {
          flex: 1 1 auto;
          min-width: 0;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;

          .match-weight {
            margin-left: 6px;
            font-size: 11px;
            font-weight: normal;
            color: var(--el-text-color-secondary);
            font-family: monospace;
          }
        }

        .person-area {
          flex: 0 0 auto;
          margin-left: auto;
          text-align: right;
          font-size: 11px;
          line-height: 1.3;
          color: var(--el-text-color-secondary);
          opacity: 0.55;
          max-width: 100%;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .ineligible-badge {
          color: var(--el-color-danger);
          font-size: 13px;
          padding: 2px 6px;
          border-radius: 4px;
          line-height: 1.3;
          max-width: 100%;
        }

        .popularity-bar {
          height: 3px;
          border-radius: 1px;
          background: var(--el-color-primary);
          opacity: 0.55;
          transition: width 0.15s ease;
          max-width: 100%;
        }
      }
    }
  }
}
</style>
