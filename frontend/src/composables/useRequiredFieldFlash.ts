import { nextTick, onBeforeUnmount, ref } from "vue";

/** Matches the CSS animation on `.required-field-flash` (1–2 seconds). */
export const REQUIRED_FIELD_FLASH_MS = 1600;

export function useRequiredFieldFlash(durationMs = REQUIRED_FIELD_FLASH_MS) {
  const flashing = ref(false);
  let timer: ReturnType<typeof setTimeout> | undefined;

  function clearTimer() {
    if (timer !== undefined) {
      clearTimeout(timer);
      timer = undefined;
    }
  }

  async function flash() {
    clearTimer();
    flashing.value = false;
    await nextTick();
    flashing.value = true;
    timer = setTimeout(() => {
      flashing.value = false;
      timer = undefined;
    }, durationMs);
  }

  onBeforeUnmount(clearTimer);

  return { flashing, flash };
}
