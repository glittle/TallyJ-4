import { computed } from 'vue';

export interface DesignTokens {
  colors: {
    primary: Record<number, string>;
    gray: Record<number, string>;
    success: Record<number, string>;
    warning: Record<number, string>;
    error: Record<number, string>;
    bg: {
      primary: string;
      secondary: string;
      tertiary: string;
      overlay: string;
    };
    text: {
      primary: string;
      secondary: string;
      tertiary: string;
      inverse: string;
    };
  };
  spacing: Record<number, string>;
  fontSize: Record<string, string>;
  fontWeight: Record<string, number>;
  lineHeight: Record<string, number>;
  radius: Record<string, string>;
  shadow: Record<string, string>;
  transition: Record<string, string>;
  zIndex: Record<string, number>;
}

export function useDesignTokens() {
  const tokens = computed<DesignTokens>(() => ({
    colors: {
      primary: {
        50: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-50').trim(),
        100: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-100').trim(),
        200: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-200').trim(),
        300: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-300').trim(),
        400: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-400').trim(),
        500: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-500').trim(),
        600: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-600').trim(),
        700: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-700').trim(),
        800: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-800').trim(),
        900: getComputedStyle(document.documentElement).getPropertyValue('--color-primary-900').trim(),
      },
      gray: {
        50: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-50').trim(),
        100: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-100').trim(),
        200: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-200').trim(),
        300: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-300').trim(),
        400: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-400').trim(),
        500: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-500').trim(),
        600: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-600').trim(),
        700: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-700').trim(),
        800: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-800').trim(),
        900: getComputedStyle(document.documentElement).getPropertyValue('--color-gray-900').trim(),
      },
      success: {
        50: getComputedStyle(document.documentElement).getPropertyValue('--color-success-50').trim(),
        500: getComputedStyle(document.documentElement).getPropertyValue('--color-success-500').trim(),
        600: getComputedStyle(document.documentElement).getPropertyValue('--color-success-600').trim(),
        700: getComputedStyle(document.documentElement).getPropertyValue('--color-success-700').trim(),
      },
      warning: {
        50: getComputedStyle(document.documentElement).getPropertyValue('--color-warning-50').trim(),
        500: getComputedStyle(document.documentElement).getPropertyValue('--color-warning-500').trim(),
        600: getComputedStyle(document.documentElement).getPropertyValue('--color-warning-600').trim(),
        700: getComputedStyle(document.documentElement).getPropertyValue('--color-warning-700').trim(),
      },
      error: {
        50: getComputedStyle(document.documentElement).getPropertyValue('--color-error-50').trim(),
        500: getComputedStyle(document.documentElement).getPropertyValue('--color-error-500').trim(),
        600: getComputedStyle(document.documentElement).getPropertyValue('--color-error-600').trim(),
        700: getComputedStyle(document.documentElement).getPropertyValue('--color-error-700').trim(),
      },
      bg: {
        primary: getComputedStyle(document.documentElement).getPropertyValue('--color-bg-primary').trim(),
        secondary: getComputedStyle(document.documentElement).getPropertyValue('--color-bg-secondary').trim(),
        tertiary: getComputedStyle(document.documentElement).getPropertyValue('--color-bg-tertiary').trim(),
        overlay: getComputedStyle(document.documentElement).getPropertyValue('--color-bg-overlay').trim(),
      },
      text: {
        primary: getComputedStyle(document.documentElement).getPropertyValue('--color-text-primary').trim(),
        secondary: getComputedStyle(document.documentElement).getPropertyValue('--color-text-secondary').trim(),
        tertiary: getComputedStyle(document.documentElement).getPropertyValue('--color-text-tertiary').trim(),
        inverse: getComputedStyle(document.documentElement).getPropertyValue('--color-text-inverse').trim(),
      },
    },
    spacing: {
      1: getComputedStyle(document.documentElement).getPropertyValue('--spacing-1').trim(),
      2: getComputedStyle(document.documentElement).getPropertyValue('--spacing-2').trim(),
      3: getComputedStyle(document.documentElement).getPropertyValue('--spacing-3').trim(),
      4: getComputedStyle(document.documentElement).getPropertyValue('--spacing-4').trim(),
      5: getComputedStyle(document.documentElement).getPropertyValue('--spacing-5').trim(),
      6: getComputedStyle(document.documentElement).getPropertyValue('--spacing-6').trim(),
      8: getComputedStyle(document.documentElement).getPropertyValue('--spacing-8').trim(),
      10: getComputedStyle(document.documentElement).getPropertyValue('--spacing-10').trim(),
      12: getComputedStyle(document.documentElement).getPropertyValue('--spacing-12').trim(),
      16: getComputedStyle(document.documentElement).getPropertyValue('--spacing-16').trim(),
      20: getComputedStyle(document.documentElement).getPropertyValue('--spacing-20').trim(),
      24: getComputedStyle(document.documentElement).getPropertyValue('--spacing-24').trim(),
    },
    fontSize: {
      xs: getComputedStyle(document.documentElement).getPropertyValue('--font-size-xs').trim(),
      sm: getComputedStyle(document.documentElement).getPropertyValue('--font-size-sm').trim(),
      base: getComputedStyle(document.documentElement).getPropertyValue('--font-size-base').trim(),
      lg: getComputedStyle(document.documentElement).getPropertyValue('--font-size-lg').trim(),
      xl: getComputedStyle(document.documentElement).getPropertyValue('--font-size-xl').trim(),
      '2xl': getComputedStyle(document.documentElement).getPropertyValue('--font-size-2xl').trim(),
      '3xl': getComputedStyle(document.documentElement).getPropertyValue('--font-size-3xl').trim(),
      '4xl': getComputedStyle(document.documentElement).getPropertyValue('--font-size-4xl').trim(),
    },
    fontWeight: {
      normal: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--font-weight-normal').trim()),
      medium: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--font-weight-medium').trim()),
      semibold: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--font-weight-semibold').trim()),
      bold: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--font-weight-bold').trim()),
    },
    lineHeight: {
      tight: parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--line-height-tight').trim()),
      normal: parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--line-height-normal').trim()),
      relaxed: parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--line-height-relaxed').trim()),
    },
    radius: {
      sm: getComputedStyle(document.documentElement).getPropertyValue('--radius-sm').trim(),
      md: getComputedStyle(document.documentElement).getPropertyValue('--radius-md').trim(),
      lg: getComputedStyle(document.documentElement).getPropertyValue('--radius-lg').trim(),
      xl: getComputedStyle(document.documentElement).getPropertyValue('--radius-xl').trim(),
      '2xl': getComputedStyle(document.documentElement).getPropertyValue('--radius-2xl').trim(),
      full: getComputedStyle(document.documentElement).getPropertyValue('--radius-full').trim(),
    },
    shadow: {
      sm: getComputedStyle(document.documentElement).getPropertyValue('--shadow-sm').trim(),
      md: getComputedStyle(document.documentElement).getPropertyValue('--shadow-md').trim(),
      lg: getComputedStyle(document.documentElement).getPropertyValue('--shadow-lg').trim(),
      xl: getComputedStyle(document.documentElement).getPropertyValue('--shadow-xl').trim(),
    },
    transition: {
      fast: getComputedStyle(document.documentElement).getPropertyValue('--transition-fast').trim(),
      normal: getComputedStyle(document.documentElement).getPropertyValue('--transition-normal').trim(),
      slow: getComputedStyle(document.documentElement).getPropertyValue('--transition-slow').trim(),
    },
    zIndex: {
      dropdown: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-dropdown').trim()),
      sticky: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-sticky').trim()),
      fixed: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-fixed').trim()),
      modalBackdrop: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-modal-backdrop').trim()),
      modal: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-modal').trim()),
      popover: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-popover').trim()),
      tooltip: parseInt(getComputedStyle(document.documentElement).getPropertyValue('--z-tooltip').trim()),
    },
  }));

  return {
    tokens,
  };
}
