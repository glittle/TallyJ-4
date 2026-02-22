<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { usePersonSearch } from '@/composables/usePersonSearch';
import type { VoteDto } from '@/types/Vote';
import type { SearchablePersonDto } from '@/types/Person';
import { Close, WarningFilled, Plus } from '@element-plus/icons-vue';

const props = defineProps<{
  positionOnBallot: number;
  modelValue?: VoteDto | null;
  candidates: SearchablePersonDto[];
  duplicatePersonGuids: string[];
  onAddNewPerson?: () => void;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: VoteDto | null];
  'vote-selected': [vote: VoteDto];
  'vote-cleared': [positionOnBallot: number];
  'add-new-person': [];
}>();

const { t } = useI18n();

const searchQuery = ref('');
const inputRef = ref();
const selectedPerson = ref<SearchablePersonDto | null>(null);
const showDropdown = ref(false);

const candidatesRef = computed(() => props.candidates);
const { searchResults } = usePersonSearch(searchQuery, candidatesRef, { maxResults: 20 });

const hasSearchQuery = computed(() => {
  return searchQuery.value.trim().length > 0;
});

const hasResults = computed(() => {
  return searchResults.value.length > 0;
});

const showEmptyState = computed(() => {
  return hasSearchQuery.value && !hasResults.value && !selectedPerson.value;
});

const isDuplicate = computed(() => {
  return selectedPerson.value ? props.duplicatePersonGuids.includes(selectedPerson.value.personGuid) : false;
});

const displayValue = computed(() => {
  if (selectedPerson.value) {
    return selectedPerson.value.fullName;
  }
  if (props.modelValue?.personFullName) {
    return props.modelValue.personFullName;
  }
  return '';
});

watch(() => props.modelValue, (newValue) => {
  if (newValue?.personGuid && newValue?.personFullName) {
    const person = props.candidates.find(c => c.personGuid === newValue.personGuid);
    if (person) {
      selectedPerson.value = person;
      searchQuery.value = person.fullName;
    } else {
      selectedPerson.value = {
        personGuid: newValue.personGuid,
        fullName: newValue.personFullName,
        lastName: newValue.personFullName,
        _searchText: newValue.personFullName,
        _soundexCodes: [],
        voteCount: 0
      } as SearchablePersonDto;
      searchQuery.value = newValue.personFullName;
    }
  } else {
    selectedPerson.value = null;
    searchQuery.value = '';
  }
}, { immediate: true });

function handleSelect(item: SearchablePersonDto) {
  selectedPerson.value = item;
  searchQuery.value = item.fullName;

  const isSpoiled = item.canReceiveVotes === false;
  const statusCode = isSpoiled ? (item.ineligibleReasonCode || 'X01') : 'Ok';

  const vote: VoteDto = {
    rowId: props.modelValue?.rowId || 0,
    ballotGuid: props.modelValue?.ballotGuid || '',
    positionOnBallot: props.positionOnBallot,
    personGuid: item.personGuid,
    personFullName: item.fullName,
    statusCode
  };

  emit('update:modelValue', vote);
  emit('vote-selected', vote);
}

function handleClear() {
  selectedPerson.value = null;
  searchQuery.value = '';
  emit('update:modelValue', null);
  emit('vote-cleared', props.positionOnBallot);
  inputRef.value?.focus();
}

function handleKeyDown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    if (searchQuery.value && !selectedPerson.value) {
      searchQuery.value = '';
      event.stopPropagation();
      event.preventDefault();
    } else if (selectedPerson.value) {
      handleClear();
      event.stopPropagation();
      event.preventDefault();
    }
  }
}

function querySearch(queryString: string, cb: (results: any[]) => void) {
  searchQuery.value = queryString;
  
  if (!queryString || !queryString.trim()) {
    cb([]);
    return;
  }

  const results = searchResults.value.map(person => ({
    value: person.fullName,
    person: person
  }));

  if (results.length === 0 && props.onAddNewPerson) {
    cb([{
      value: '__EMPTY_STATE__',
      isEmpty: true
    }]);
  } else {
    cb(results);
  }
}

function handleAutocompleteSelect(item: any) {
  if (item && item.isEmpty) {
    return;
  }
  if (item && item.person) {
    handleSelect(item.person);
  }
}

function handleAddNewPerson() {
  emit('add-new-person');
  if (props.onAddNewPerson) {
    props.onAddNewPerson();
  }
}

function focusInput() {
  inputRef.value?.focus();
}

defineExpose({
  focusInput
});
</script>

<template>
  <div 
    class="vote-entry-row"
    :class="{ 'is-duplicate': isDuplicate }"
  >
    <div class="vote-entry-row__position">
      {{ positionOnBallot }}
    </div>
    
    <div class="vote-entry-row__input">
      <el-autocomplete
        ref="inputRef"
        v-model="searchQuery"
        :fetch-suggestions="querySearch"
        :placeholder="$t('ballots.candidate')"
        :disabled="!!selectedPerson"
        clearable
        @select="handleAutocompleteSelect"
        @keydown="handleKeyDown"
        :aria-label="`${$t('ballots.position')} ${positionOnBallot} - ${$t('ballots.candidate')}`"
        :aria-describedby="isDuplicate ? `duplicate-warning-${positionOnBallot}` : undefined"
        class="vote-entry-row__autocomplete"
      >
        <template #default="{ item }">
          <div v-if="item.isEmpty" class="autocomplete-empty">
            <div class="autocomplete-empty__message">
              <span>{{ $t('ballots.noMatchesFound') }}</span>
              <small>{{ $t('ballots.checkSpelling') }}</small>
            </div>
            <el-button
              v-if="onAddNewPerson"
              :icon="Plus"
              size="small"
              type="primary"
              plain
              class="autocomplete-empty__button"
              @click.stop="handleAddNewPerson"
            >
              {{ $t('ballots.addNewPerson') }}
            </el-button>
          </div>
          <div v-else class="autocomplete-item" :class="{ 'autocomplete-item--ineligible': item.person?.canReceiveVotes === false }">
            <span class="autocomplete-item__name">{{ item.value }}</span>
            <span v-if="item.person?.canReceiveVotes === false" class="autocomplete-item__ineligible-code" :title="$t('ballots.ineligible')">
              {{ item.person.ineligibleReasonCode }}
            </span>
            <span v-if="item.person?.voteCount > 0" class="autocomplete-item__vote-count">{{ item.person.voteCount }}</span>
          </div>
        </template>
      </el-autocomplete>

      <el-button
        v-if="selectedPerson"
        :icon="Close"
        circle
        size="small"
        class="vote-entry-row__clear"
        @click="handleClear"
        :aria-label="$t('common.clear')"
      />
    </div>

    <div 
      v-if="isDuplicate"
      :id="`duplicate-warning-${positionOnBallot}`"
      class="vote-entry-row__warning"
      role="alert"
      aria-live="polite"
    >
      <el-icon class="vote-entry-row__warning-icon">
        <WarningFilled />
      </el-icon>
      <span>{{ $t('ballots.duplicateWarning') }}</span>
    </div>
  </div>
</template>

<style lang="less" scoped>
.vote-entry-row {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-3, 12px);
  padding: var(--spacing-2, 8px) 0;
  position: relative;

  &__position {
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 32px;
    height: 32px;
    padding: 0 var(--spacing-2, 8px);
    background-color: var(--el-fill-color-lighter);
    border-radius: var(--radius-sm, 4px);
    font-weight: var(--font-weight-medium, 500);
    color: var(--el-text-color-regular);
    font-size: var(--font-size-sm, 14px);
  }

  &__input {
    flex: 1;
    display: flex;
    align-items: center;
    gap: var(--spacing-2, 8px);
    position: relative;
  }

  &__autocomplete {
    flex: 1;

    :deep(.el-input__wrapper) {
      transition: all var(--transition-normal, 0.2s);
    }
  }

  &.is-duplicate {
    :deep(.el-input__wrapper) {
      border-color: var(--el-color-warning);
      box-shadow: 0 0 0 1px var(--el-color-warning) inset;
    }
  }

  &__clear {
    flex-shrink: 0;
  }

  &__warning {
    position: absolute;
    top: 100%;
    left: 44px;
    display: flex;
    align-items: center;
    gap: var(--spacing-1, 4px);
    padding: var(--spacing-1, 4px) var(--spacing-2, 8px);
    background-color: var(--el-color-warning-light-9);
    border: 1px solid var(--el-color-warning-light-5);
    border-radius: var(--radius-sm, 4px);
    font-size: var(--font-size-sm, 14px);
    color: var(--el-color-warning-dark-2);
    margin-top: var(--spacing-1, 4px);
    z-index: 1;
  }

  &__warning-icon {
    font-size: 16px;
  }
}

.autocomplete-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-2, 8px);
  padding: var(--spacing-2, 8px);

  &__name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__vote-count {
    flex-shrink: 0;
    min-width: 20px;
    padding: 1px 6px;
    background-color: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    border-radius: 10px;
    font-size: var(--font-size-xs, 12px);
    font-weight: var(--font-weight-medium, 500);
    text-align: center;
  }

  &__ineligible-code {
    flex-shrink: 0;
    padding: 1px 6px;
    background-color: var(--el-color-warning-light-9);
    color: var(--el-color-warning-dark-2);
    border-radius: 4px;
    font-size: var(--font-size-xs, 12px);
    font-weight: var(--font-weight-medium, 500);
    font-family: monospace;
  }

  &--ineligible {
    .autocomplete-item__name {
      color: var(--el-color-warning-dark-2);
    }
  }
}

.autocomplete-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-3, 12px);
  padding: var(--spacing-4, 16px);
  text-align: center;

  &__message {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-1, 4px);

    span {
      font-size: var(--font-size-base, 16px);
      font-weight: var(--font-weight-medium, 500);
      color: var(--el-text-color-regular);
    }

    small {
      font-size: var(--font-size-sm, 14px);
      color: var(--el-text-color-secondary);
    }
  }

  &__button {
    width: 100%;
    max-width: 200px;
  }
}
</style>
