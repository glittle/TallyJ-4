import { ref, onMounted, onUnmounted, computed } from "vue";

export type Breakpoint = "xs" | "sm" | "md" | "lg" | "xl" | "2xl";

export interface ResponsiveState {
  width: number;
  height: number;
  breakpoint: Breakpoint;
  isMobile: boolean;
  isTablet: boolean;
  isDesktop: boolean;
  isXs: boolean;
  isSm: boolean;
  isMd: boolean;
  isLg: boolean;
  isXl: boolean;
  is2xl: boolean;
}

const breakpoints = {
  xs: 0,
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  "2xl": 1536,
} as const;

export function useResponsive() {
  const width = ref(globalThis.innerWidth || 0);
  const height = ref(globalThis.innerHeight || 0);

  const breakpoint = computed<Breakpoint>(() => {
    const w = width.value;
    if (w >= breakpoints["2xl"]) return "2xl";
    if (w >= breakpoints.xl) return "xl";
    if (w >= breakpoints.lg) return "lg";
    if (w >= breakpoints.md) return "md";
    if (w >= breakpoints.sm) return "sm";
    return "xs";
  });

  const isMobile = computed(() => width.value < breakpoints.md);
  const isTablet = computed(
    () => width.value >= breakpoints.md && width.value < breakpoints.lg,
  );
  const isDesktop = computed(() => width.value >= breakpoints.lg);

  const isXs = computed(() => breakpoint.value === "xs");
  const isSm = computed(() => breakpoint.value === "sm");
  const isMd = computed(() => breakpoint.value === "md");
  const isLg = computed(() => breakpoint.value === "lg");
  const isXl = computed(() => breakpoint.value === "xl");
  const is2xl = computed(() => breakpoint.value === "2xl");

  function updateDimensions() {
    width.value = globalThis.innerWidth;
    height.value = globalThis.innerHeight;
  }

  onMounted(() => {
    updateDimensions();
    globalThis.addEventListener("resize", updateDimensions);
  });

  onUnmounted(() => {
    globalThis.removeEventListener("resize", updateDimensions);
  });

  const state = computed<ResponsiveState>(() => ({
    width: width.value,
    height: height.value,
    breakpoint: breakpoint.value,
    isMobile: isMobile.value,
    isTablet: isTablet.value,
    isDesktop: isDesktop.value,
    isXs: isXs.value,
    isSm: isSm.value,
    isMd: isMd.value,
    isLg: isLg.value,
    isXl: isXl.value,
    is2xl: is2xl.value,
  }));

  return {
    width,
    height,
    breakpoint,
    isMobile,
    isTablet,
    isDesktop,
    isXs,
    isSm,
    isMd,
    isLg,
    isXl,
    is2xl,
    state,
  };
}
