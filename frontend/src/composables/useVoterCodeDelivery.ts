import { onBeforeUnmount, ref } from "vue";
import { signalrService } from "@/services/signalrService";
import type { VoterCodeDeliveryStatusEvent } from "@/types/SignalREvents";

const CHANNEL_TIMEOUT_MS = 10 * 60 * 1000;

/**
 * Joins the anonymous voter-code delivery hub after requestCode and exposes
 * live status. Leaves on final, timeout, or unmount. Token stays in memory.
 */
export function useVoterCodeDelivery() {
  const deliveryStatus = ref<VoterCodeDeliveryStatusEvent | null>(null);
  let timeoutId: ReturnType<typeof setTimeout> | null = null;
  let watching = false;

  function applyStatus(payload: VoterCodeDeliveryStatusEvent) {
    if (!payload || typeof payload.status !== "string") {
      return;
    }
    deliveryStatus.value = {
      status: payload.status,
      messageKey: payload.messageKey ?? null,
      okay: payload.okay ?? null,
      providerStatus: payload.providerStatus ?? null,
    };
    if (payload.status === "final") {
      void stopWatching();
    }
  }

  function clearTimer() {
    if (timeoutId !== null) {
      clearTimeout(timeoutId);
      timeoutId = null;
    }
  }

  async function watchChannel(channelToken: string | null | undefined) {
    await stopWatching();
    deliveryStatus.value = null;
    if (!channelToken) {
      return;
    }

    watching = true;
    const connection = await signalrService.connectToVoterCodeHub();
    connection.on("codeDeliveryStatus", applyStatus);
    await signalrService.joinVoterCodeChannel(channelToken);
    timeoutId = setTimeout(() => {
      void stopWatching();
    }, CHANNEL_TIMEOUT_MS);
  }

  async function stopWatching() {
    clearTimer();
    if (!watching) {
      return;
    }
    watching = false;
    const connection = signalrService.getConnection("/hubs/voter-code");
    connection?.off("codeDeliveryStatus", applyStatus);
    await signalrService.disconnectVoterCodeHub();
  }

  onBeforeUnmount(() => {
    void stopWatching();
  });

  return {
    deliveryStatus,
    watchChannel,
    stopWatching,
  };
}
