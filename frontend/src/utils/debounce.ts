/**
 * Simple debounce utility.
 * Returns a debounced version of the provided function.
 */
export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): (...args: Parameters<T>) => void {
  let timer: ReturnType<typeof setTimeout> | null = null;

  return (...args: Parameters<T>) => {
    if (timer) {
      clearTimeout(timer);
    }
    timer = setTimeout(() => {
      fn(...args);
    }, delay);
  };
}

/**
 * Vue-friendly debounce for async functions that returns a debounced version.
 * Use this when you want to debounce a function that will be called frequently
 * (e.g. from input handlers or watchers).
 */
export function useDebounceFn<T extends (...args: any[]) => any>(
  fn: T,
  delay: number,
): (...args: Parameters<T>) => void {
  return debounce(fn, delay);
}
