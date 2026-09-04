import { describe, expect, it, vi } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { mount } from "@vue/test-utils";
import { i18n } from "../../test/setup";

vi.mock("vue-country-flag-next", () => ({
  default: {
    name: "CountryFlag",
    template: "<span class='flag' />",
  },
}));

import LanguageFlagsSelector from "./LanguageFlagsSelector.vue";

const source = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "LanguageFlagsSelector.vue"),
  "utf8",
);

describe("LanguageFlagsSelector", () => {
  it("renders a flag button per supported locale", () => {
    const wrapper = mount(LanguageFlagsSelector, {
      global: { plugins: [i18n] },
    });
    const buttons = wrapper.findAll(".flag-button");
    expect(buttons.length).toBeGreaterThan(1);
    expect(wrapper.classes()).toContain("language-flags-selector");
  });

  it("isolates flag sprites from document RTL", () => {
    expect(source).toContain("direction: ltr");
    expect(source).toContain("unicode-bidi: isolate");
    expect(source).toContain("margin-inline: -26px");
    expect(source).toContain("text-align: center");
  });
});
