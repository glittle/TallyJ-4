import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { i18n } from "../../test/setup";
import VoterAuthFaq from "./VoterAuthFaq.vue";

const source = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "VoterAuthFaq.vue"),
  "utf8",
);

describe("VoterAuthFaq", () => {
  it("renders the FAQ heading and collapse items", () => {
    const wrapper = mount(VoterAuthFaq, {
      global: { plugins: [i18n, ElementPlus] },
    });
    expect(wrapper.find(".voter-auth-faq").exists()).toBe(true);
    expect(wrapper.find(".faq-header").exists()).toBe(true);
    expect(wrapper.find(".faq-collapse").exists()).toBe(true);
    expect(wrapper.findAll(".el-collapse-item").length).toBe(7);
  });

  it("uses logical alignment so FAQ follows document dir", () => {
    expect(source).toContain("text-align: start");
    expect(source).toContain("margin-inline-start: auto");
    expect(source).toContain("margin-inline-end: 8px");
  });
});
