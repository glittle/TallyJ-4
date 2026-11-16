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
  it("keeps Delete all people in a danger menu outside the send row", async () => {
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
    const menu = wrapper.get("[data-testid='people-danger-menu']");
    expect(toolbar.element.contains(menu.element)).toBe(false);
    expect(menu.text()).toContain("Danger");

    await menu.trigger("click");
    await flushPromises();
    expect(document.body.textContent).toContain("Delete All People");
  });
});
