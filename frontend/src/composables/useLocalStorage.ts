import { ref, watch } from "vue";
import type { Ref } from "vue";

/**
 * Minimal replacement for @vueuse/core useStorage.
 * Persists a reactive ref to localStorage (JSON serialized).
 *
 * Only supports the simple string key + default value usage pattern
 * currently present in the codebase.
 */
export function useLocalStorage<T>(key: string, defaultValue: T): Ref<T> {
  let initialValue = defaultValue;

  try {
    const stored = localStorage.getItem(key);
    if (stored !== null) {
      initialValue = JSON.parse(stored) as T;
    }
  } catch {
    // Ignore parse errors, fall back to default
  }

  const data = ref<T>(initialValue) as Ref<T>;

  watch(
    data,
    (val) => {
      try {
        localStorage.setItem(key, JSON.stringify(val));
      } catch {
        // Ignore storage errors (quota, private mode, etc.)
      }
    },
    { deep: true },
  );

  return data;
}
