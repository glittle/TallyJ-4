import { i18n } from "@/test/setup";
import { createTestingPinia } from "@pinia/testing";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { beforeAll, describe, expect, it } from "vitest";
import { createRouter, createWebHistory } from "vue-router";
import PeopleManagementPage from "./PeopleManagementPage.vue";

beforeAll(() => {
  if (typeof globalThis.ResizeObserver === "undefined") {
    globalThis.ResizeObserver = class {
      observe() {}
      unobserve() {}
      disconnect() {}
    } as typeof ResizeObserver;
  }
});

describe("PeopleManagementPage toolbar", () => {
  it("wraps the actions so Delete All People stays in the toolbar", async () => {
    const pinia = createTestingPinia({ stubActions: true });
    const router = createRouter({
      history: createWebHistory(),
      routes: [
        {
          path: "/elections/:id/people",
          component: { template: "<div />" },
        },
      ],
    });
    await router.push("/elections/abc/people");
    await router.isReady();

    const wrapper = mount(PeopleManagementPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    const toolbar = wrapper.get(".people-toolbar");
    expect(toolbar.attributes("style") ?? "").toMatch(/flex-wrap:\s*wrap/);
    const deleteButton = wrapper.get(".people-toolbar-delete");
    expect(toolbar.element.contains(deleteButton.element)).toBe(true);
    expect(deleteButton.text()).toContain("Delete All People");
  });
});
