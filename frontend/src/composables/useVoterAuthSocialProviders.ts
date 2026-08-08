import { getAppConfig } from "@/config/appConfig";
import type { useOnlineVotingStore } from "@/stores/onlineVotingStore";
import { ref } from "vue";
import type { Router, RouteLocationNormalizedLoaded } from "vue-router";

declare const FB: {
  init: (opts: Record<string, unknown>) => void;
  login: (
    cb: (res: { authResponse?: { accessToken?: string } }) => void,
    opts: { scope: string },
  ) => void;
};
declare const Kakao: {
  isInitialized: () => boolean;
  init: (key: string) => void;
  Auth: {
    login: (opts: {
      success: (authObj: { access_token: string }) => void;
      fail: (err: unknown) => void;
    }) => void;
  };
};

type OnlineVotingStore = ReturnType<typeof useOnlineVotingStore>;
type Translate = (key: string) => string;

export type UseVoterAuthSocialProvidersOptions = {
  onlineVotingStore: OnlineVotingStore;
  router: Router;
  route: RouteLocationNormalizedLoaded;
  t: Translate;
  showErrorMessage: (msg: string) => void;
  setLoading: (value: boolean) => void;
};

/**
 * Facebook / Kakao / Telegram SDK load + login handlers for the online voter auth page.
 */
export function useVoterAuthSocialProviders(
  options: UseVoterAuthSocialProvidersOptions,
) {
  const fbReady = ref(false);
  const fbError = ref(false);
  const fbScriptLoaded = ref(false);

  const kakaoReady = ref(false);
  const kakaoError = ref(false);
  const kakaoScriptLoaded = ref(false);

  const telegramReady = ref(false);
  const telegramError = ref(false);
  const telegramBotUsername = ref<string | null>(null);

  async function redirectAfterAuth() {
    const electionGuid = options.route.query.election as string;
    if (electionGuid) {
      await options.router.push(`/vote/${electionGuid}`);
    } else {
      await options.router.push({ name: "voter-elections" });
    }
  }

  const loadFacebookSdk = (): Promise<void> => {
    return new Promise((resolve, reject) => {
      if (fbScriptLoaded.value || typeof FB !== "undefined") {
        fbScriptLoaded.value = true;
        resolve();
        return;
      }
      const script = document.createElement("script");
      script.src = "https://connect.facebook.net/en_US/sdk.js";
      script.async = true;
      script.defer = true;
      script.crossOrigin = "anonymous";
      script.onload = () => {
        fbScriptLoaded.value = true;
        resolve();
      };
      script.onerror = () => reject(new Error("Failed to load Facebook SDK"));
      document.head.appendChild(script);
    });
  };

  const initFacebookSdk = async () => {
    try {
      const config = getAppConfig();
      fbError.value = false;
      console.log("Initializing Facebook SDK...");
      if (!config?.facebookAppId) {
        console.error("No Facebook App ID in config");
        fbError.value = true;
        return;
      }

      const isLocalhost =
        window.location.hostname === "localhost" ||
        window.location.hostname === "127.0.0.1";
      if (isLocalhost) {
        console.warn(
          "Facebook login may not work on localhost. Configure your Facebook App with a local domain like local.tallyj.com and update your hosts file, or use ngrok for testing.",
        );
      }

      console.log("Loading Facebook SDK...");
      await loadFacebookSdk();
      console.log("Facebook SDK loaded, initializing...");
      FB.init({
        appId: config.facebookAppId,
        cookie: true,
        xfbml: true,
        version: "v18.0",
      });
      console.log("Facebook SDK initialized successfully");
      fbReady.value = true;
    } catch (error) {
      console.error("Failed to initialize Facebook SDK:", error);
      fbError.value = true;
    }
  };

  const handleFacebookLogin = async () => {
    try {
      options.setLoading(true);

      if (typeof FB === "undefined") {
        console.error("Facebook SDK not loaded");
        options.showErrorMessage(options.t("voting.auth.facebook.error"));
        options.setLoading(false);
        return;
      }

      const timeoutId = setTimeout(() => {
        console.warn("Facebook login timeout - callback never called");
        options.setLoading(false);
        options.showErrorMessage(
          options.t("voting.auth.facebook.popupBlocked"),
        );
      }, 10000);

      console.log("Calling FB.login...");
      try {
        FB.login(
          async (res) => {
            console.log("FB.login callback called with result:", res);
            clearTimeout(timeoutId);
            if (res.authResponse?.accessToken) {
              await options.onlineVotingStore.facebookAuth({
                accessToken: res.authResponse.accessToken,
              });
              await redirectAfterAuth();
            } else {
              options.showErrorMessage(
                options.t("voting.auth.facebook.cancelled"),
              );
            }
            options.setLoading(false);
          },
          { scope: "email" },
        );
      } catch (syncError) {
        console.error("FB.login threw synchronously:", syncError);
        clearTimeout(timeoutId);
        options.showErrorMessage(options.t("voting.auth.facebook.error"));
        options.setLoading(false);
      }
    } catch (error) {
      console.error("Error with Facebook authentication:", error);
      options.setLoading(false);
    }
  };

  const loadKakaoSdk = (): Promise<void> => {
    return new Promise((resolve, reject) => {
      if (kakaoScriptLoaded.value || typeof Kakao !== "undefined") {
        kakaoScriptLoaded.value = true;
        resolve();
        return;
      }
      const script = document.createElement("script");
      script.src = "https://t1.kakaocdn.net/kakao_js_sdk/2.7.2/kakao.min.js";
      script.async = true;
      script.onload = () => {
        kakaoScriptLoaded.value = true;
        resolve();
      };
      script.onerror = () => reject(new Error("Failed to load Kakao SDK"));
      document.head.appendChild(script);
    });
  };

  const initKakaoSdk = async () => {
    try {
      const config = getAppConfig();
      kakaoError.value = false;
      if (!config?.kakaoApiJsKey) {
        kakaoError.value = true;
        return;
      }
      await loadKakaoSdk();
      if (!Kakao.isInitialized()) {
        Kakao.init(config.kakaoApiJsKey);
      }
      kakaoReady.value = true;
    } catch (error) {
      console.error("Failed to initialize Kakao SDK:", error);
      kakaoError.value = true;
    }
  };

  const handleKakaoLogin = async () => {
    try {
      options.setLoading(true);

      const timeoutId = setTimeout(() => {
        options.setLoading(false);
        options.showErrorMessage(options.t("voting.auth.kakao.popupBlocked"));
      }, 10000);

      Kakao.Auth.login({
        success: async (authObj) => {
          clearTimeout(timeoutId);
          await options.onlineVotingStore.kakaoAuth({
            accessToken: authObj.access_token,
          });
          await redirectAfterAuth();
          options.setLoading(false);
        },
        fail: (err) => {
          clearTimeout(timeoutId);
          console.error("Kakao login failed:", err);
          options.setLoading(false);
        },
      });
    } catch (error) {
      console.error("Error with Kakao authentication:", error);
      options.setLoading(false);
    }
  };

  const handleTelegramLogin = async (user: {
    id: number;
    first_name?: string;
    last_name?: string;
    username?: string;
    photo_url?: string;
    auth_date: number;
    hash: string;
  }) => {
    try {
      options.setLoading(true);
      await options.onlineVotingStore.telegramAuth({
        id: user.id,
        firstName: user.first_name,
        lastName: user.last_name,
        username: user.username,
        photoUrl: user.photo_url,
        authDate: user.auth_date,
        hash: user.hash,
      });
      await redirectAfterAuth();
    } catch (error) {
      console.error("Error with Telegram authentication:", error);
    } finally {
      options.setLoading(false);
    }
  };

  return {
    fbReady,
    fbError,
    kakaoReady,
    kakaoError,
    telegramReady,
    telegramError,
    telegramBotUsername,
    initFacebookSdk,
    handleFacebookLogin,
    initKakaoSdk,
    handleKakaoLogin,
    handleTelegramLogin,
    redirectAfterAuth,
  };
}
