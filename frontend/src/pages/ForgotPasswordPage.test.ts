import { describe, expect, it, vi } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { createRouter, createWebHistory } from "vue-router";
import ForgotPasswordPage from "./ForgotPasswordPage.vue";
import { i18n, pinia } from "@/test/setup";
import { authService } from "@/services/authService";

function capabilities(local: boolean) {
  return {
    officers: {
      local,
      google: false,
      facebook: false,
      kakao: false,
      telegram: false,
    },
    voters: {
      google: true,
      facebook: false,
      kakao: false,
      telegram: false,
      emailCode: true,
      phoneCode: true,
      sms: true,
      voice: true,
      whatsApp: true,
      directCode: true,
    },
  };
}

async function mountPage() {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      { path: "/forgot-password", component: ForgotPasswordPage },
      { path: "/login", component: { template: "<div>login</div>" } },
    ],
  });
  await router.push("/forgot-password");
  await router.isReady();
  const wrapper = mount(ForgotPasswordPage, {
    global: { plugins: [pinia, router, i18n, ElementPlus] },
  });
  await flushPromises();
  return wrapper;
}

describe("ForgotPasswordPage", () => {
  it("shows one generic result for a request that was accepted", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const forgot = vi
      .spyOn(authService, "forgotPassword")
      .mockResolvedValue(undefined);

    const wrapper = await mountPage();
    await wrapper
      .get('[data-testid="forgot-password-email"]')
      .setValue("officer@example.com");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(forgot).toHaveBeenCalledWith("officer@example.com");
    expect(
      wrapper.get('[data-testid="forgot-password-sent"]').text(),
    ).toContain("If a local account exists for that email");
    expect(wrapper.find('[data-testid="forgot-password-email"]').exists()).toBe(
      false,
    );
    forgot.mockRestore();
  });

  it("does not call the API for an empty email", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const forgot = vi.spyOn(authService, "forgotPassword");

    const wrapper = await mountPage();
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(forgot).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Email is required");
    forgot.mockRestore();
  });

  it("hides the form when local officer sign-in is off", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(false),
    );
    const forgot = vi.spyOn(authService, "forgotPassword");

    const wrapper = await mountPage();

    expect(
      wrapper.get('[data-testid="forgot-password-disabled"]').text(),
    ).toContain("not offered on this server");
    expect(wrapper.find('[data-testid="forgot-password-email"]').exists()).toBe(
      false,
    );
    expect(forgot).not.toHaveBeenCalled();
    forgot.mockRestore();
  });

  it("shows the server refusal without claiming an account exists", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const forgot = vi.spyOn(authService, "forgotPassword").mockRejectedValue({
      error: "auth.errors.signInMethodDisabled",
    });

    const wrapper = await mountPage();
    await wrapper
      .get('[data-testid="forgot-password-email"]')
      .setValue("officer@example.com");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(
      wrapper.get('[data-testid="forgot-password-error"]').text(),
    ).toContain("not offered on this server");
    expect(wrapper.find('[data-testid="forgot-password-sent"]').exists()).toBe(
      false,
    );
    forgot.mockRestore();
  });
});
