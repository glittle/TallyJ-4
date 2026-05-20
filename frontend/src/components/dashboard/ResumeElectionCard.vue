<script setup lang="ts">
import StageIndicator from "@/components/nav/StageIndicator.vue";
import { tallyStatusToStage } from "@/domain/electionStages";
import { useElectionStore } from "@/stores/electionStore";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

const { t } = useI18n();
const router = useRouter();
const electionStore = useElectionStore();

const mostRecent = computed(() => {
  const sorted = [...electionStore.elections].sort((a, b) => {
    const aDate = a.dateOfElection ? new Date(a.dateOfElection).getTime() : 0;
    const bDate = b.dateOfElection ? new Date(b.dateOfElection).getTime() : 0;
    return bDate - aDate;
  });
  return sorted[0] ?? null;
});

const stage = computed(() =>
  mostRecent.value
    ? tallyStatusToStage(mostRecent.value.tallyStatus)
    : "SettingUp",
);

const participationPct = computed(() => {
  const e = mostRecent.value;
  if (!e || !e.voterCount) {
    return null;
  }
  return Math.round((e.ballotCount / e.voterCount) * 100);
});

function open() {
  if (mostRecent.value) {
    router.push(`/elections/${mostRecent.value.electionGuid}`);
  }
}
</script>

<template>
  <section v-if="mostRecent" class="resume-card">
    <header class="resume-card__header">
      <span class="resume-card__eyebrow">{{
        t("dashboard.resumeCard.eyebrow")
      }}</span>
    </header>
    <h3 class="resume-card__name">{{ mostRecent.name }}</h3>
    <StageIndicator :stage="stage" size="sm" class="resume-card__stage" />
    <dl class="resume-card__stats">
      <div>
        <dt>{{ t("dashboard.resumeCard.people") }}</dt>
        <dd>{{ mostRecent.voterCount }}</dd>
      </div>
      <div>
        <dt>{{ t("dashboard.resumeCard.ballots") }}</dt>
        <dd>{{ mostRecent.ballotCount }}</dd>
      </div>
      <div>
        <dt>{{ t("dashboard.resumeCard.participation") }}</dt>
        <dd>{{ participationPct !== null ? `${participationPct}%` : "—" }}</dd>
      </div>
    </dl>
    <button class="resume-card__open" @click="open">
      {{ t("dashboard.resumeCard.open") }}
    </button>
  </section>
</template>

<style lang="less" scoped>
.resume-card {
  background: var(--el-bg-color, #fff);
  border: 1px solid var(--color-border-light, #e4e7ed);
  border-radius: 8px;
  padding: 16px;
  flex-grow: 1;

  &__header {
    margin-bottom: 4px;
  }

  &__eyebrow {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--color-text-secondary, #909399);
  }

  &__name {
    font-size: 15px;
    font-weight: 600;
    color: var(--color-text-primary, #303133);
    margin: 4px 0 8px;
    line-height: 1.3;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__stage {
    margin-bottom: 12px;
  }

  &__stats {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
    margin: 0 0 14px;
    font-size: 12px;

    dt {
      color: var(--color-text-secondary, #909399);
      margin-bottom: 2px;
    }

    dd {
      font-weight: 600;
      color: var(--color-text-primary, #303133);
      margin: 0;
    }
  }

  &__open {
    display: block;
    width: 100%;
    padding: 8px;
    background: var(--el-color-primary, #409eff);
    color: #fff;
    border: none;
    border-radius: 5px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.15s;

    &:hover {
      background: var(--el-color-primary-dark-2, #337ecc);
    }
  }
}
</style>
