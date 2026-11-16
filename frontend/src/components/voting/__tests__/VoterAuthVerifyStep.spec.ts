import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import VoterAuthVerifyStep from "../VoterAuthVerifyStep.vue";

const stubs = {
  ElButton: { template: "<button><slot /></button>" },
  ElForm: { template: "<form><slot /></form>" },
  ElFormItem: { template: "<div><slot /></div>" },
  ElIcon: { template: "<span />" },
  ElInput: true,
  VoterCodeDeliveryStatus: true,
};

function mountStep(codeSent: boolean) {
  return mount(VoterAuthVerifyStep, {
    props: {
      loading: false,
      codeSent,
      verificationForm: {
        voterId: "voter@tallyj.test",
        verifyCode: "",
      },
    },
    global: {
      plugins: [i18n],
      stubs,
    },
  });
}

describe("VoterAuthVerifyStep", () => {
  it("says the code was sent when delivery succeeded", () => {
    const wrapper = mountStep(true);
    expect(wrapper.text()).toContain(
      "We've sent a verification code to voter@tallyj.test",
    );
    expect(wrapper.text()).toContain("Please check your inbox");
  });

  it("does not say the code was sent when delivery failed", () => {
    const wrapper = mountStep(false);
    expect(wrapper.text()).toContain(
      "We could not send a verification code to voter@tallyj.test",
    );
    expect(wrapper.text()).not.toContain("We've sent");
    expect(wrapper.text()).not.toContain("Please check your inbox");
  });
});
