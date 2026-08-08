<script setup lang="ts">
import type {
  FrontDeskVoterDto,
  RegistrationHistoryEntryDto,
} from "@/types/FrontDesk";
import { Check, Close } from "@element-plus/icons-vue";
import { nextTick, ref, watch } from "vue";

const props = defineProps<{
  voter: FrontDeskVoterDto;
  registrationHistory: RegistrationHistoryEntryDto[];
  registrationTypes: { value: string; label: string }[];
  electionFlags: string[];
  hasActiveTeller: boolean;
  checkInInProgress: boolean;
  pendingVotingMethod: string | null;
  selectedButtonIndex: number;
  getDialogButtonKey: (value: string) => string;
  isDialogButtonKeyboardFocused: (value: string) => boolean;
  hasFlag: (voter: FrontDeskVoterDto, flag: string) => boolean;
  getVotingMethodLabel: (method?: string) => string;
  formatTime: (time?: string) => string;
  formatTimeline: (entry: RegistrationHistoryEntryDto) => string;
}>();

const emit = defineEmits<{
  keydown: [event: KeyboardEvent];
  unregister: [];
  close: [];
  "click-button": [value: string];
}>();

const overlayRef = ref<HTMLDivElement | null>(null);

watch(
  () => props.voter.personGuid,
  () => {
    nextTick(() => overlayRef.value?.focus());
  },
  { immediate: true },
);

defineExpose({
  focus: () => overlayRef.value?.focus(),
  el: overlayRef,
  querySelector: (selector: string) =>
    overlayRef.value?.querySelector(selector) ?? null,
});
</script>

<template>
  <div
    ref="overlayRef"
    class="front-desk-registration-overlay"
    tabindex="-1"
    @keydown.capture="emit('keydown', $event)"
  >
    <div class="registration-buttons">
      <el-alert
        v-if="!hasActiveTeller"
        type="warning"
        :title="$t('frontDesk.tellerRequired.title')"
        :description="$t('frontDesk.tellerRequired.message')"
        show-icon
        :closable="false"
        class="teller-required-alert"
      />
      <div class="registration-header">
        <div class="selected-voter-info">
          <strong>
            {{
              voter.isCheckedIn
                ? $t("frontDesk.dialog.update")
                : $t("frontDesk.dialog.checkIn")
            }}
            {{ voter.fullName }}
          </strong>
          <span v-if="voter.bahaiId" class="voter-detail">
            {{ $t("frontDesk.dialog.id") }}
            {{ voter.bahaiId }}
          </span>
          <span v-if="voter.area" class="voter-detail">
            {{ $t("frontDesk.dialog.area") }}
            {{ voter.area }}
          </span>
        </div>
        <div class="registration-header-actions">
          <el-button
            v-if="voter.isCheckedIn"
            type="default"
            size="large"
            data-dialog-button="__unregister__"
            class="unregister-button dialog-option-button"
            :disabled="!hasActiveTeller"
            :class="{
              'keyboard-focused-button':
                isDialogButtonKeyboardFocused('__unregister__'),
            }"
            @click="emit('unregister')"
          >
            {{ $t("frontDesk.dialog.unregister") }}
            <kbd>{{ getDialogButtonKey("__unregister__") }}</kbd>
          </el-button>
          <el-button
            type="default"
            size="large"
            data-dialog-button="__close__"
            class="close-dialog-button"
            :class="{
              'keyboard-focused-button':
                isDialogButtonKeyboardFocused('__close__'),
            }"
            :disabled="checkInInProgress"
            :title="$t('common.close')"
            @click="emit('close')"
          >
            <el-icon>
              <Close />
            </el-icon>
            {{ $t("common.close") }}
          </el-button>
        </div>
      </div>

      <div v-if="voter.isCheckedIn" class="button-section checked-in-section">
        <h4>{{ $t("frontDesk.dialog.currentRegistration") }}</h4>
        <div class="checked-in-details">
          <el-tag type="success" size="large">
            {{ getVotingMethodLabel(voter.votingMethod) }}
          </el-tag>
          <span v-if="voter.envNum" class="checked-in-detail">
            {{
              $t("frontDesk.dialog.envelope", {
                num: voter.envNum,
              })
            }}
          </span>
          <span v-if="voter.registrationTime" class="checked-in-detail">
            {{ formatTime(voter.registrationTime) }}
          </span>
        </div>
      </div>

      <div v-if="!voter.isCheckedIn" class="button-section">
        <h4>{{ $t("frontDesk.dialog.votingMethod") }}</h4>
        <div
          class="button-group"
          :class="{ 'check-in-pending': checkInInProgress }"
        >
          <el-button
            v-for="type in registrationTypes"
            :key="type.value"
            :data-dialog-button="type.value"
            :type="pendingVotingMethod === type.value ? 'primary' : 'default'"
            size="large"
            class="dialog-option-button"
            :disabled="
              !hasActiveTeller ||
              (checkInInProgress && pendingVotingMethod !== type.value)
            "
            :class="{
              'keyboard-focused-button': isDialogButtonKeyboardFocused(
                type.value,
              ),
              'pending-button': pendingVotingMethod === type.value,
            }"
            @click="emit('click-button', type.value)"
          >
            {{ type.label }}
            <kbd>{{ getDialogButtonKey(type.value) }}</kbd>
          </el-button>
        </div>
      </div>

      <div v-if="electionFlags.length > 0" class="button-section">
        <h4>{{ $t("frontDesk.dialog.flags") }}</h4>
        <div class="button-group">
          <el-button
            v-for="flag in electionFlags"
            :key="flag"
            :data-dialog-button="flag"
            :type="hasFlag(voter, flag) ? 'success' : 'default'"
            size="large"
            class="dialog-option-button"
            :disabled="!hasActiveTeller || checkInInProgress"
            :class="{
              'keyboard-focused-button': isDialogButtonKeyboardFocused(flag),
            }"
            @click="emit('click-button', flag)"
          >
            {{ flag }}
            <kbd>{{ getDialogButtonKey(flag) }}</kbd>
            <el-icon v-if="hasFlag(voter, flag)" style="margin-left: 5px">
              <Check />
            </el-icon>
          </el-button>
        </div>
      </div>

      <div v-if="registrationHistory.length" class="dialog-history-section">
        <h4>{{ $t("frontDesk.dialog.registrationHistory") }}</h4>
        <el-timeline>
          <el-timeline-item
            v-for="(entry, index) in registrationHistory"
            :key="index"
            :timestamp="formatTime(entry.timestamp)"
          >
            {{ formatTimeline(entry) }}
          </el-timeline-item>
        </el-timeline>
      </div>

      <div class="instruction-text">
        <template v-if="checkInInProgress">
          {{ $t("frontDesk.dialog.checkingIn") }}
        </template>
        <template v-else>
          {{ $t("frontDesk.dialog.instructions") }}
        </template>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.front-desk-registration-overlay {
  position: absolute;
  inset: 0;
  z-index: 10;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 24px 16px;
  background: color-mix(in srgb, var(--el-bg-color) 88%, transparent);
  backdrop-filter: blur(2px);
  overflow-y: auto;
  outline: none;

  .teller-required-alert {
    margin: 0 0 12px;
  }

  .registration-buttons {
    width: 100%;
    position: relative;
    max-width: 900px;
    padding: 20px;
    background: var(--color-orange-50);
    border-radius: 8px;
    border: 1px solid var(--el-border-color);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  }

  .button-section {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 10px 0;
      font-size: 14px;
      color: var(--el-text-color-regular);
    }
  }

  .registration-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 20px;
  }

  .selected-voter-info {
    display: flex;
    flex: 1;
    flex-wrap: wrap;
    align-items: flex-end;
    gap: 0 4em;
    min-width: 0;
    font-size: 18px;

    strong {
      font-size: 1.4em;
    }
  }

  .voter-detail {
    display: flex;
    flex-direction: column;
    margin: 0;
  }

  .registration-header-actions {
    display: flex;
    align-items: flex-start;
    justify-content: flex-end;
    gap: 10px;
    flex: 0 0 auto;
  }

  .unregister-button,
  .dialog-option-button {
    flex: 0 0 auto;
    width: auto;
    position: relative;
    white-space: nowrap;
    padding-right: 2.25rem;
  }

  .close-dialog-button {
    flex: 0 0 auto;
    position: relative;
  }

  .button-group {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 10px;
    margin-bottom: 10px;
  }

  .registration-buttons .el-button kbd {
    position: absolute;
    top: 50%;
    right: 10px;
    transform: translateY(-50%);
    font-size: 10px;
    padding: 2px 4px;
    background: rgba(0, 0, 0, 0.1);
    border-radius: 3px;
    line-height: 1;
  }

  .keyboard-focused-button.el-button:not(.unregister-button) {
    border: 2px solid var(--el-color-primary) !important;
    box-shadow: 0 0 0 1px var(--el-color-primary-light-5);
  }

  .keyboard-focused-button.el-button--success:not(.unregister-button) {
    border-color: var(--el-color-success) !important;
    box-shadow: 0 0 0 1px var(--el-color-success-light-5);
  }

  .unregister-button.keyboard-focused-button.el-button {
    border: 2px solid var(--el-text-color-secondary) !important;
    box-shadow: 0 0 0 1px var(--el-border-color);
  }

  .pending-button.el-button {
    background-color: var(--el-color-primary) !important;
    border-color: var(--el-color-primary) !important;
    color: #fff !important;
    box-shadow: 0 0 0 2px var(--el-color-primary-light-5);
  }

  .pending-button.el-button:hover,
  .pending-button.el-button:focus {
    background-color: var(--el-color-primary-dark-2) !important;
    border-color: var(--el-color-primary-dark-2) !important;
    color: #fff !important;
  }

  .check-in-pending .el-button:not(.pending-button) {
    opacity: 0.55;
  }

  .instruction-text {
    text-align: center;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    margin-top: 10px;
  }

  .checked-in-section {
    .checked-in-details {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 16px;
      margin-bottom: 16px;
      .el-tag {
        --el-tag-font-size: 16px;
      }
    }

    .checked-in-detail {
      color: var(--el-text-color-secondary);
      font-size: 14px;
    }
  }

  .dialog-history-section {
    margin-top: 20px;
    padding-top: 16px;
    max-height: 300px;
    overflow-y: auto;
    border-top: 1px solid var(--el-border-color);

    h4 {
      margin: 0 0 12px 0;
      font-size: 14px;
      color: var(--el-text-color-regular);
    }
  }
}
</style>
