import { defineComponent, h } from "vue";
import { flushPromises, mount, type VueWrapper } from "@vue/test-utils";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  REQUIRED_FIELD_FLASH_MS,
  useRequiredFieldFlash,
} from "../useRequiredFieldFlash";

describe("useRequiredFieldFlash", () => {
  let wrapper: VueWrapper | null = null;
  let api: ReturnType<typeof useRequiredFieldFlash> | undefined;

  function mountHarness() {
    wrapper?.unmount();
    api = undefined;
    wrapper = mount(
      defineComponent({
        setup() {
          api = useRequiredFieldFlash();
          return () => h("div");
        },
      }),
    );
  }

  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    wrapper?.unmount();
    wrapper = null;
    vi.useRealTimers();
  });

  it("turns flashing on then off after the attention duration", async () => {
    mountHarness();

    expect(api!.flashing.value).toBe(false);
    const flashPromise = api!.flash();
    await flushPromises();
    await flashPromise;
    expect(api!.flashing.value).toBe(true);

    vi.advanceTimersByTime(REQUIRED_FIELD_FLASH_MS - 1);
    expect(api!.flashing.value).toBe(true);

    vi.advanceTimersByTime(1);
    expect(api!.flashing.value).toBe(false);
  });

  it("restarts the flash when called again before it finishes", async () => {
    mountHarness();

    await api!.flash();
    await flushPromises();
    vi.advanceTimersByTime(800);

    await api!.flash();
    await flushPromises();
    expect(api!.flashing.value).toBe(true);

    vi.advanceTimersByTime(REQUIRED_FIELD_FLASH_MS - 1);
    expect(api!.flashing.value).toBe(true);
    vi.advanceTimersByTime(1);
    expect(api!.flashing.value).toBe(false);
  });
});
