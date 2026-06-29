<script setup lang="ts">
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import {
  getActiveTellers,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import BallotEntryPanel from "../../components/ballots/BallotEntryPanel.vue";

const route = useRoute();
const electionGuid = route.params.id as string;
const ballotGuid = route.params.ballotId as string;

const activeTellers = ref<ActiveTellers>(getActiveTellers());
const hasKeyboardTeller = computed(() =>
  Boolean(activeTellers.value.teller1.trim()),
);

function onTellersChanged(tellers: ActiveTellers) {
  activeTellers.value = tellers;
}
</script>

<template>
  <div class="ballot-entry-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <ActiveTellerSelector
            :election-guid="electionGuid"
            @tellers-changed="onTellersChanged"
          />
        </div>
      </template>

      <BallotEntryPanel
        :election-guid="electionGuid"
        :ballot-guid="ballotGuid"
        :has-keyboard-teller="hasKeyboardTeller"
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
