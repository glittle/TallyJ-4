<script setup lang="ts">
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useRoute } from "vue-router";
import BallotEntryPanel from "../../components/ballots/BallotEntryPanel.vue";

const route = useRoute();
const electionGuid = route.params.id as string;
const ballotGuid = route.params.ballotId as string;
</script>

<template>
  <div class="ballot-entry-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <ActiveTellerSelector :election-guid="electionGuid" />
        </div>
      </template>

      <BallotEntryPanel
        :election-guid="electionGuid"
        :ballot-guid="ballotGuid"
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
