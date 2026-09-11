/**
 * Debounced function plus cancel/flush so callers can drop or run the
 * pending invocation (e.g. submit / unmount).
 */
export type DebouncedFn<T extends (...args: any[]) => any> = ((
  ...args: Parameters<T>
) => void) & {
  cancel: () => void;
  flush: () => ReturnType<T> | undefined;
};

/**
 * Simple debounce utility.
 * Returns a debounced version of the provided function.
 */
export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): DebouncedFn<T> {
  let timer: ReturnType<typeof setTimeout> | null = null;
  let lastArgs: Parameters<T> | null = null;

  const run = ((...args: Parameters<T>) => {
    lastArgs = args;
    if (timer) {
      clearTimeout(timer);
    }
    timer = setTimeout(() => {
      timer = null;
      const callArgs = lastArgs ?? args;
      lastArgs = null;
      fn(...callArgs);
    }, delay);
  }) as DebouncedFn<T>;

  run.cancel = () => {
    if (timer) {
      clearTimeout(timer);
      timer = null;
    }
    lastArgs = null;
  };

  run.flush = () => {
    if (!timer && lastArgs === null) {
      return undefined;
    }
    const callArgs = lastArgs ?? ([] as unknown as Parameters<T>);
    run.cancel();
    return fn(...callArgs);
  };

  return run;
}

/**
 * Vue-friendly debounce for async functions that returns a debounced version.
 * Use this when you want to debounce a function that will be called frequently
 * (e.g. from input handlers or watchers).
 */
export function useDebounceFn<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): DebouncedFn<T> {
  return debounce(fn, delay);
}
