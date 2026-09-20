<script setup lang="ts">
import { computed } from "vue";
import { ElAlert } from "element-plus";
import type { VoterCodeDeliveryStatusEvent } from "@/types/SignalREvents";

const props = defineProps<{
  status: VoterCodeDeliveryStatusEvent | null;
}>();

const visible = computed(() => !!props.status?.status);

const alertType = computed(() => {
  switch (props.status?.status) {
    case "failed":
      return "error";
    case "final":
      return props.status.okay ? "success" : "error";
    case "delivered":
    case "sent":
      return "success";
    default:
      return "info";
  }
});

const messageKey = computed(
  () => props.status?.messageKey || "voting.auth.delivery.sending",
);
</script>

<template>
  <div
    v-if="visible"
    class="voter-code-delivery-status"
    data-testid="voter-code-delivery-status"
  >
    <ElAlert
      :title="$t(messageKey)"
      :type="alertType"
      :closable="false"
      show-icon
      :data-status="status?.status"
    />
  </div>
</template>

<style lang="less">
.voter-code-delivery-status {
  margin: 0 0 20px;

  .el-alert {
    align-items: flex-start;
  }
}
</style>
