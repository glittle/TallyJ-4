import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { describe, expect, it } from "vitest";
import VoterCodeDeliveryStatus from "../VoterCodeDeliveryStatus.vue";
import type { VoterCodeDeliveryStatusEvent } from "@/types/SignalREvents";

const messages = {
  en: {
    "voting.auth.delivery.sending": "Sending your code…",
    "voting.auth.delivery.sent": "The code has been sent.",
    "voting.auth.delivery.delivered": "The code was delivered.",
    "voting.auth.delivery.failed": "The code could not be delivered.",
    "voting.auth.delivery.finalOk": "Delivery finished successfully.",
    "voting.auth.delivery.finalFailed": "Delivery finished with an error.",
  },
};

function mountStatus(status: VoterCodeDeliveryStatusEvent | null) {
  const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: { en: messages.en },
  });

  return mount(VoterCodeDeliveryStatus, {
    props: { status },
    global: {
      plugins: [i18n],
      stubs: {
        ElAlert: {
          props: ["title", "type"],
          template: '<div class="el-alert" :data-type="type">{{ title }}</div>',
        },
      },
    },
  });
}

describe("VoterCodeDeliveryStatus", () => {
  it("hides when there is no status yet", () => {
    const wrapper = mountStatus(null);
    expect(
      wrapper.find("[data-testid='voter-code-delivery-status']").exists(),
    ).toBe(false);
  });

  it.each([
    ["sending", "Sending your code…", "info"],
    ["sent", "The code has been sent.", "success"],
    ["delivered", "The code was delivered.", "success"],
    ["failed", "The code could not be delivered.", "error"],
  ] as const)("renders %s from the i18n key", (status, text, type) => {
    const wrapper = mountStatus({
      status,
      messageKey: `voting.auth.delivery.${status}`,
    });

    expect(wrapper.text()).toContain(text);
    expect(wrapper.find(".el-alert").attributes("data-type")).toBe(type);
    expect(wrapper.text()).not.toContain("ABC123");
    expect(wrapper.text()).not.toMatch(/\b\d{5,6}\b/);
  });

  it("renders final success and does not show an OTP even if one is smuggled in extra fields", () => {
    const wrapper = mountStatus({
      status: "final",
      okay: true,
      messageKey: "voting.auth.delivery.finalOk",
      providerStatus: "delivered",
    });

    expect(wrapper.text()).toContain("Delivery finished successfully.");
    expect(wrapper.find(".el-alert").attributes("data-type")).toBe("success");
    expect(wrapper.html()).not.toContain("verifyCode");
    expect(wrapper.html()).not.toContain("ABC123");
  });

  it("renders final failure", () => {
    const wrapper = mountStatus({
      status: "final",
      okay: false,
      messageKey: "voting.auth.delivery.finalFailed",
    });

    expect(wrapper.text()).toContain("Delivery finished with an error.");
    expect(wrapper.find(".el-alert").attributes("data-type")).toBe("error");
  });
});
