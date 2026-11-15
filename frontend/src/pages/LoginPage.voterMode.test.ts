import { flushPromises, mount } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n, pinia } from "@/test/setup";
import ElementPlus from "element-plus";
import LoginPage from "./LoginPage.vue";

const { showSuccessMessage, showWarningMessage } = vi.hoisted(() => ({
  showSuccessMessage: vi.fn(),
  showWarningMessage: vi.fn(),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage,
    showErrorMessage: vi.fn(),
    showWarningMessage,
  }),
}));

vi.mock("@/composables/useGoogleOneTap", async () => {
  const { ref } = await import("vue");
  return {
    useGoogleOneTap: () => ({
      googleReady: ref(false),
      initGoogleOneTap: vi.fn(),
      teardownGoogleOneTap: vi.fn(),
    }),
  };
});

describe("LoginPage voter mode", () => {
  beforeEach(() => {
    showSuccessMessage.mockClear();
    showWarningMessage.mockClear();
  });

  it("leaves for voter sign-in and does not claim a code was sent", async () => {
    const router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: "/login", component: LoginPage },
        {
          path: "/voter-auth",
          name: "voter-auth",
          component: { template: "<div class='voter-auth-target' />" },
        },
      ],
    });
    await router.push({ path: "/login", query: { mode: "voter" } });
    await router.isReady();

    const wrapper = mount(LoginPage, {
      global: { plugins: [pinia, router, i18n, ElementPlus] },
    });
    await flushPromises();

    expect(router.currentRoute.value.name).toBe("voter-auth");
    expect(showSuccessMessage).not.toHaveBeenCalled();
    expect(showWarningMessage).not.toHaveBeenCalled();
    expect(wrapper.text()).not.toContain(
      "Verification code sent to your email",
    );
    expect(wrapper.text()).not.toContain("Request Code");
    expect(wrapper.text()).not.toContain(
      "Voter OTC login not fully implemented in backend yet.",
    );
  });
});
