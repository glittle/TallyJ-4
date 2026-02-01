<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { ElCard, ElResult, ElButton } from 'element-plus';
import { useRouter } from 'vue-router';

const router = useRouter();
const onlineVotingStore = useOnlineVotingStore();

const voteStatus = ref(onlineVotingStore.voteStatus);

function handleLogout() {
  onlineVotingStore.logout();
  router.push('/');
}
</script>

<template>
  <div class="voter-confirmation-page">
    <div class="confirmation-container">
      <ElCard class="confirmation-card">
        <ElResult
          icon="success"
          title="Ballot Submitted Successfully!"
          sub-title="Thank you for participating in the election"
        >
          <template #extra>
            <div class="confirmation-details">
              <p v-if="voteStatus?.whenSubmitted">
                <strong>Submitted:</strong> {{ new Date(voteStatus.whenSubmitted).toLocaleString() }}
              </p>
              <p v-if="onlineVotingStore.electionInfo">
                <strong>Election:</strong> {{ onlineVotingStore.electionInfo.name }}
              </p>
              <div class="info-message">
                <p>Your ballot has been securely recorded.</p>
                <p>Please note that you cannot change or resubmit your ballot.</p>
              </div>
              <ElButton 
                type="primary" 
                @click="handleLogout"
                size="large"
              >
                Close
              </ElButton>
            </div>
          </template>
        </ElResult>
      </ElCard>
    </div>
  </div>
</template>

<style lang="less" scoped>
.voter-confirmation-page {
  min-height: calc(100vh - 100px);
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 20px;
}

.confirmation-container {
  width: 100%;
  max-width: 600px;
}

.confirmation-card {
  text-align: center;
}

.confirmation-details {
  margin-top: 20px;

  p {
    margin: 10px 0;
    color: var(--el-text-color-regular);

    strong {
      color: var(--el-text-color-primary);
    }
  }

  .info-message {
    margin: 20px 0;
    padding: 15px;
    background-color: var(--el-fill-color-light);
    border-radius: 4px;

    p {
      margin: 5px 0;
      font-size: 14px;
    }
  }
}
</style>
