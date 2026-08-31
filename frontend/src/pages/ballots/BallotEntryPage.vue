<script setup lang="ts">
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useRequiredFieldFlash } from "@/composables/useRequiredFieldFlash";
import {
  getActiveTellers,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import type { BallotStartBlockReason } from "@/utils/ballotStartRequirements";
import {
  electionBallotEntryPath,
  electionBallotsPath,
} from "@/utils/ballotRoutes";
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import BallotEntryPanel from "../../components/ballots/BallotEntryPanel.vue";

const route = useRoute();
const router = useRouter();
const electionGuid = route.params.id as string;
const ballotGuid = route.params.ballotId as string;

function handleBallotCreated(newBallotGuid: string) {
  router.push(electionBallotEntryPath(electionGuid, newBallotGuid));
}

function handleBallotDeleted() {
  router.push(electionBallotsPath(electionGuid));
}

const activeTellers = ref<ActiveTellers>(getActiveTellers());
const { flashing: tellerFlashing, flash: flashTeller } =
  useRequiredFieldFlash();
const hasKeyboardTeller = computed(() =>
  Boolean(activeTellers.value.teller1.trim()),
);

function onTellersChanged(tellers: ActiveTellers) {
  activeTellers.value = tellers;
}

function onBallotStartBlocked(reason: BallotStartBlockReason) {
  if (reason === "teller") {
    void flashTeller();
  }
}
</script>

<template>
  <div class="ballot-entry-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <ActiveTellerSelector
            :election-guid="electionGuid"
            :highlight-teller1="tellerFlashing"
            @tellers-changed="onTellersChanged"
          />
        </div>
      </template>

      <BallotEntryPanel
        :key="ballotGuid"
        :election-guid="electionGuid"
        :ballot-guid="ballotGuid"
        :has-keyboard-teller="hasKeyboardTeller"
        @ballot-created="handleBallotCreated"
        @ballot-deleted="handleBallotDeleted"
        @ballot-start-blocked="onBallotStartBlocked"
      />
    </el-card>
  </div>
</template>

<style lang="less">
.ballot-entry-page {
  max-width: var(--normal-max-width);
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
}
</style>
