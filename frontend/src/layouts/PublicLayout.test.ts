import { describe, it, expect, beforeEach, vi } from "vitest";
import { mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import { i18n } from "../test/setup";
import versionJson from "../../../version.json";

vi.mock("../components/common/LanguageFlagsSelector.vue", () => ({
  default: {
    name: "LanguageFlagsSelector",
    template: "<div class='language-flags-selector-stub' />",
  },
}));

vi.mock("../components/common/LanguageSelector.vue", () => ({
  default: {
    name: "LanguageSelector",
    template: "<div class='language-selector-stub' />",
  },
}));

vi.mock("../components/common/ThemeSelector.vue", () => ({
  default: {
    name: "ThemeSelector",
    template: "<div class='theme-selector-stub' />",
  },
}));

vi.mock("../components/version", async () => {
  const mod = await import("../../../version.json");
  const versionData = (mod as { default?: { version: string } }).default ?? mod;
  return {
    VERSION: (versionData as { version: string }).version,
    getBuildDate: () => "April 11, 2026",
    getBuildDateBadi: () => "183.02.03",
  };
});

import PublicLayout from "./PublicLayout.vue";

const VERSION = versionJson.version;

const layoutStubs = {
  RouterView: {
    name: "RouterView",
    template: "<div class='router-view-stub' />",
  },
};

describe("PublicLayout", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    router = createRouter({
      history: createWebHistory(),
      routes: [],
    });
  });

  it("renders properly", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    expect(wrapper.exists()).toBe(true);
  });

  it("displays the application title", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    expect(wrapper.text()).toContain("Version 4 Beta");
    expect(wrapper.text()).toContain(VERSION);
  });

  it("has the correct layout structure", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    expect(wrapper.classes()).toContain("public-layout");
    expect(wrapper.find(".public-header").exists()).toBe(true);
    expect(wrapper.find(".public-content").exists()).toBe(true);
  });

  it("includes the logo image", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    const img = wrapper.find("img");
    expect(img.exists()).toBe(true);
    expect(img.attributes("alt")).toBe("TallyJ Logo");
    expect(img.attributes("src")).toBe("/assets/logo-trans.png");
  });

  it("renders language selector and ThemeSelector components", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    expect(wrapper.find(".language-flags-selector-stub").exists()).toBe(true);
    expect(wrapper.find(".theme-selector-stub").exists()).toBe(true);
  });

  it("includes router-view for content", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    expect(wrapper.find(".router-view-stub").exists()).toBe(true);
  });

  it("displays version tooltip on logo", () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: layoutStubs,
      },
    });
    const versionName = wrapper.find(".logo .versionName");
    expect(versionName.exists()).toBe(true);
    expect(versionName.attributes("title")).toBeTruthy();
    expect(versionName.text()).toBe(VERSION);
  });
});