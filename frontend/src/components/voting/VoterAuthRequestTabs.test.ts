import { describe, expect, it } from "vitest";
import { mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { i18n } from "../../test/setup";
import VoterAuthRequestTabs from "./VoterAuthRequestTabs.vue";

const defaultProps = {
  activeTab: "google",
  emailForm: { email: "" },
  phoneForm: { phone: "", deliveryMethod: "sms" as const },
  codeForm: { code: "" },
  loading: false,
  googleReady: true,
  googleError: false,
  fbReady: true,
  fbError: false,
  kakaoReady: true,
  kakaoError: false,
  telegramReady: false,
  telegramError: false,
  telegramBotUsername: null,
  emailRules: {},
  phoneRules: {},
  codeRules: {},
};

describe("VoterAuthRequestTabs", () => {
  it("uses currentColor for Facebook and Kakao tab icons", () => {
    const wrapper = mount(VoterAuthRequestTabs, {
      props: defaultProps,
      global: { plugins: [i18n, ElementPlus] },
    });

    const facebookPath = wrapper.find(".facebook-icon path");
    const kakaoPath = wrapper.find(".kakao-icon path");
    expect(facebookPath.exists()).toBe(true);
    expect(kakaoPath.exists()).toBe(true);
    expect(facebookPath.attributes("fill")).toBe("currentColor");
    expect(kakaoPath.attributes("fill")).toBe("currentColor");
    expect(facebookPath.attributes("fill")).not.toBe("#1877F2");
  });

  it("hides disabled voter methods and an SMS transport without hiding email", () => {
    const wrapper = mount(VoterAuthRequestTabs, {
      props: {
        ...defaultProps,
        signIn: {
          google: false,
          facebook: true,
          kakao: false,
          telegram: false,
          email: true,
          phone: true,
          sms: false,
          voice: true,
          whatsApp: true,
          directCode: false,
        },
      },
      global: { plugins: [i18n, ElementPlus] },
    });

    expect(wrapper.text()).not.toContain("sign in with Google");
    expect(wrapper.text()).toContain("Email");
    expect(wrapper.text()).not.toContain("personal voting code");
    expect(wrapper.text()).not.toContain("SMS Text Message");
    expect(wrapper.text()).toContain("Voice Call");
    expect(wrapper.text()).toContain("WhatsApp Message");
  });
});
