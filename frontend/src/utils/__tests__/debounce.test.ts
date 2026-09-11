import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { debounce } from "../debounce";

describe("debounce cancel/flush", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("cancel drops a pending invocation", () => {
    const fn = vi.fn();
    const run = debounce(fn, 800);
    run();
    run.cancel();
    vi.advanceTimersByTime(800);
    expect(fn).not.toHaveBeenCalled();
  });

  it("flush runs the pending invocation immediately", () => {
    const fn = vi.fn();
    const run = debounce(fn, 800);
    run();
    run.flush();
    expect(fn).toHaveBeenCalledTimes(1);
    vi.advanceTimersByTime(800);
    expect(fn).toHaveBeenCalledTimes(1);
  });
});
