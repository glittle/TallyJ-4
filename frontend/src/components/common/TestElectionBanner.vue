<script setup lang="ts">
import { useElectionStore } from "@/stores/electionStore";
import { WarningFilled } from "@element-plus/icons-vue";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

const { t } = useI18n();
const route = useRoute();
const electionStore = useElectionStore();

const routeElectionGuid = computed(() => {
  const id = route.params.id;
  return typeof id === "string" ? id : "";
});

/**
 * Persistent teller chrome for the election currently on the route.
 * Hidden when there is no current election, the route is not election-scoped
 * (Dashboard, Profile, … — leftover store state does not count), the route
 * election does not match currentElection, or showAsTest is false/null.
 */
const showTestElectionBanner = computed(() => {
  const current = electionStore.currentElection;
  if (!routeElectionGuid.value || !current) {
    return false;
  }
  if (current.electionGuid !== routeElectionGuid.value) {
    return false;
  }
  return current.showAsTest === true;
});
</script>

<template>
  <div
    v-if="showTestElectionBanner"
    class="test-election-banner"
    role="status"
    data-testid="test-election-banner"
  >
    <el-icon aria-hidden="true">
      <WarningFilled />
    </el-icon>
    <span>{{ t("elections.testElectionBanner") }}</span>
  </div>
</template>

<style lang="less">
.test-election-banner {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  flex-shrink: 0;
  padding: 6px 16px;
  font-size: var(--el-font-size-small);
  font-weight: 700;
  letter-spacing: 0.03em;
  line-height: 1.2;
  text-align: center;
  /* Explicit pair — do not inherit header text/bg (dark-on-dark on the
     translucent / Front Desk header). Always gather-stage orange, not the
     current election stage and not error/danger red. White on
     --color-stage-gather meets contrast in both light (#d97706) and
     dark (#f59e0b) tokens. */
  background-color: var(--color-stage-gather);
  color: #fff;

  .el-icon {
    font-size: 16px;
    color: #fff;
  }
}
</style>
