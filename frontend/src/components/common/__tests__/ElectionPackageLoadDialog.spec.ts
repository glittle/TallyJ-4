import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import ElectionPackageLoadDialog from "../ElectionPackageLoadDialog.vue";

describe("ElectionPackageLoadDialog", () => {
  it("translates a loader line that is a phrase key and leaves English lines as text", () => {
    const wrapper = mount(ElectionPackageLoadDialog, {
      props: {
        modelValue: true,
        lines: [
          {
            id: 1,
            message: "elections.form.electionPasscodeNotCopied",
            isTemporary: false,
          },
          {
            id: 2,
            message: "Package validated (TallyJ4 JSON)",
            isTemporary: false,
          },
        ],
        loading: false,
        succeeded: false,
      },
      global: {
        plugins: [i18n],
        stubs: {
          ElDialog: { template: "<div><slot /></div>" },
          ElAlert: true,
          ElButton: true,
        },
      },
    });

    expect(wrapper.text()).toContain(
      "The teller passcode was shorter than 6 characters and was not copied.",
    );
    expect(wrapper.text()).not.toContain(
      "elections.form.electionPasscodeNotCopied",
    );
    expect(wrapper.text()).toContain("Package validated (TallyJ4 JSON)");
  });
});
