<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useElectionStore } from "@/stores/electionStore";
import { tallyStatusToStage } from "@/domain/electionStages";
import TipsPanel from "@/components/nav/TipsPanel.vue";

const { t } = useI18n();
const electionStore = useElectionStore();

const hasSetupElection = computed(() =>
  electionStore.elections.some(
    (e) => tallyStatusToStage(e.tallyStatus) === "SettingUp",
  ),
);

const TIP_ID = "dashboard.setup.intro";
</script>

<template>
  <TipsPanel v-if="hasSetupElection" :tip-id="TIP_ID">
    <template #header>
      <span
        class="setup-tips-card__eyebrow"
        :style="{ color: 'var(--color-stage-setup)' }"
      >
        {{ t("dashboard.setupTipsCard.eyebrow") }}
      </span>
    </template>
    <h4 class="setup-tips-card__title">
      {{ t("dashboard.setupTipsCard.title") }}
    </h4>
    <ol class="setup-tips-card__list">
      <li>{{ t("dashboard.setupTipsCard.tip1") }}</li>
      <li>{{ t("dashboard.setupTipsCard.tip2") }}</li>
      <li>{{ t("dashboard.setupTipsCard.tip3") }}</li>
      <li>{{ t("dashboard.setupTipsCard.tip4") }}</li>
    </ol>
  </TipsPanel>
</template>

<style lang="less" scoped>
.setup-tips-card {
  &__eyebrow {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }

  &__title {
    font-size: 13px;
    font-weight: 600;
    margin: 0 0 8px;
    color: var(--color-text-primary, #303133);
  }

  &__list {
    margin: 0;
    padding-left: 16px;
    line-height: 1.7;

    li {
      margin-bottom: 2px;
    }
  }
}
</style>
