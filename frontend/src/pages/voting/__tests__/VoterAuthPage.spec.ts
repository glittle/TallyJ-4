import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import VoterAuthPage from "../VoterAuthPage.vue";

const showErrorMessage = vi.fn();
const verifyCode = vi.fn();
const requestVerificationCode = vi.fn().mockResolvedValue({
  messageKey: "voting.auth.requestCode.sent",
  channelToken: "channel-token",
});

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => ({ query: {}, params: {} }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage,
  }),
}));

vi.mock("@/stores/onlineVotingStore", () => ({
  useOnlineVotingStore: () => ({
    requestVerificationCode,
    verifyCode,
  }),
}));

vi.mock("@/composables/useVoterCodeDelivery", () => ({
  useVoterCodeDelivery: () => ({
    deliveryStatus: { value: null },
    watchChannel: vi.fn().mockResolvedValue(undefined),
    stopWatching: vi.fn(),
  }),
}));

vi.mock("@/composables/useGoogleOneTap", () => ({
  useGoogleOneTap: () => ({
    googleReady: { value: true },
    googleError: { value: false },
    initGoogleOneTap: vi.fn(),
  }),
}));

vi.mock("@/composables/useVoterAuthSocialProviders", () => ({
  useVoterAuthSocialProviders: () => ({
    fbReady: { value: false },
    fbError: { value: false },
    kakaoReady: { value: false },
    kakaoError: { value: false },
    telegramReady: { value: false },
    telegramError: { value: false },
    telegramBotUsername: { value: null },
    initFacebookSdk: vi.fn(),
    handleFacebookLogin: vi.fn(),
    initKakaoSdk: vi.fn(),
    handleKakaoLogin: vi.fn(),
    handleTelegramLogin: vi.fn(),
    redirectAfterAuth: vi.fn(),
  }),
}));

const stubs = {
  ElButton: { template: "<button><slot /></button>" },
  ElCard: { template: "<div><slot /></div>" },
  ElIcon: { template: "<span />" },
  VoterAuthFaq: { template: "<div />" },
  VoterAuthRequestTabs: {
    template:
      "<button class='request-email' @click=\"$emit('request-email')\" />",
  },
  VoterAuthVerifyStep: {
    template: "<button class='verify-code' @click=\"$emit('verify')\" />",
  },
};

async function mountOnVerifyStep() {
  const wrapper = mount(VoterAuthPage, {
    global: {
      plugins: [i18n],
      stubs,
    },
  });
  await wrapper.get(".request-email").trigger("click");
  await flushPromises();
  return wrapper;
}

describe("VoterAuthPage verify failures", () => {
  beforeEach(() => {
    showErrorMessage.mockReset();
    verifyCode.mockReset();
    requestVerificationCode.mockResolvedValue({
      messageKey: "voting.auth.requestCode.sent",
      channelToken: "channel-token",
    });
  });

  it.each([
    [
      "voting.auth.verify.codeExpired",
      "Verification code has expired. Please request a new code.",
    ],
    [
      "voting.auth.verify.alreadyUsed",
      "This verification code has already been used. Please request a new code.",
    ],
    [
      "voting.auth.verify.tooManyAttempts",
      "Too many failed attempts. Please request a new code.",
    ],
    [
      "voting.auth.verify.noCodeFound",
      "No verification code found. Please request a new code.",
    ],
    ["error.tooManyRequests", "Too many requests. Please try again later."],
  ])(
    "shows %s instead of the generic verify fallback",
    async (key, expected) => {
      verifyCode.mockRejectedValue({ error: key });
      const wrapper = await mountOnVerifyStep();

      await wrapper.get(".verify-code").trigger("click");
      await flushPromises();

      expect(showErrorMessage).toHaveBeenCalledWith(expected);
      expect(showErrorMessage).not.toHaveBeenCalledWith(
        "Could not verify that code. Please check it and try again.",
      );
    },
  );

  it("shows remaining attempts for a mismatch instead of the generic fallback", async () => {
    verifyCode.mockRejectedValue({
      error: "voting.auth.verify.invalidCode",
      attempts: 4,
    });
    const wrapper = await mountOnVerifyStep();

    await wrapper.get(".verify-code").trigger("click");
    await flushPromises();

    expect(showErrorMessage).toHaveBeenCalledWith(
      "Invalid verification code. 4 attempts remaining.",
    );
  });
});
