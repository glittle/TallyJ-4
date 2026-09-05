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
});
