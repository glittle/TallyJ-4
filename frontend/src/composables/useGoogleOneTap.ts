import type { GoogleCredentialResponse } from "../types/google-one-tap";
import { getAppConfig } from "../config/appConfig";
import { nextTick, onBeforeUnmount, ref, watch, type Ref } from "vue";
import { useI18n } from "vue-i18n";

declare global {
  interface Window {
    google: any;
  }
}

export function useGoogleOneTap(options: {
  buttonRef: Ref<HTMLElement | undefined>;
  onCredential: (response: GoogleCredentialResponse) => void | Promise<void>;
  promptOnInit?: boolean;
}) {
  const { locale } = useI18n();
  const googleClientId = ref<string | null>(null);
  const googleReady = ref(false);
  const googleError = ref(false);
  const gisScriptLoaded = ref(false);
  let gisCleanup: (() => void) | null = null;
  let isInitializingGis = false;

  const loadGisScript = (): Promise<void> => {
    return new Promise((resolve, reject) => {
      if (gisScriptLoaded.value || globalThis.google !== undefined) {
        gisScriptLoaded.value = true;
        resolve();
        return;
      }

      const script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      script.async = true;
      script.defer = true;
      script.onload = () => {
        gisScriptLoaded.value = true;
        resolve();
      };
      script.onerror = () =>
        reject(new Error("Failed to load Google Identity Services script"));
      document.head.appendChild(script);
    });
  };

  const renderGoogleButton = () => {
    nextTick(() => {
      if (
        options.buttonRef.value &&
        googleClientId.value &&
        googleReady.value &&
        globalThis.google !== undefined
      ) {
        globalThis.google.accounts.id.renderButton(options.buttonRef.value, {
          type: "standard",
          theme: "outline",
          size: "large",
          text: "signin_with",
          shape: "rectangular",
          width: "300",
          locale: locale.value,
        });
      }
    });
  };

  watch(options.buttonRef, (el) => {
    if (el && googleReady.value) {
      renderGoogleButton();
    }
  });

  watch(locale, () => {
    if (googleReady.value) {
      renderGoogleButton();
    }
  });

  const initGoogleOneTap = async () => {
    if (googleReady.value || isInitializingGis) {
      return;
    }
    isInitializingGis = true;
    try {
      googleError.value = false;
      await loadGisScript();
      const clientId = getAppConfig().googleClientId ?? null;
      googleClientId.value = clientId;

      if (!clientId || globalThis.google === undefined) {
        googleError.value = true;
        googleReady.value = false;
        return;
      }

      globalThis.google.accounts.id.initialize({
        client_id: clientId,
        callback: options.onCredential,
        auto_select: false,
        cancel_on_tap_outside: true,
        use_fedcm_for_prompt: false,
      });

      googleReady.value = true;
      renderGoogleButton();

      if (options.promptOnInit !== false) {
        globalThis.google.accounts.id.prompt();
      }

      gisCleanup = () => {
        if (globalThis.google !== undefined && googleReady.value) {
          try {
            globalThis.google.accounts.id.cancel();
          } catch {
            // Ignore errors from cancel
          }
        }
        googleReady.value = false;
      };
    } catch (error) {
      console.error("Failed to initialize Google One Tap:", error);
      googleError.value = true;
      googleReady.value = false;
    } finally {
      isInitializingGis = false;
    }
  };

  const teardownGoogleOneTap = () => {
    if (gisCleanup) {
      gisCleanup();
      gisCleanup = null;
    }
  };

  onBeforeUnmount(() => {
    teardownGoogleOneTap();
  });

  return {
    googleReady,
    googleError,
    initGoogleOneTap,
    teardownGoogleOneTap,
    renderGoogleButton,
  };
}
