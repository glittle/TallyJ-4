import { describe, expect, it, vi } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { createRouter, createWebHistory } from "vue-router";
import ResetPasswordPage from "./ResetPasswordPage.vue";
import { i18n, pinia } from "@/test/setup";
import { authService } from "@/services/authService";
import { useAuthStore } from "@/stores/authStore";

const token = "abc+def/ghi=";

function capabilities(local: boolean) {
  return {
    officers: {
      local,
      google: true,
      facebook: false,
      kakao: false,
      telegram: false,
    },
    voters: {
      google: true,
      facebook: true,
      kakao: false,
      telegram: false,
      emailCode: true,
      phoneCode: false,
      sms: false,
      voice: false,
      whatsApp: false,
      directCode: true,
    },
  };
}

async function mountPage(query = "") {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      { path: "/reset-password", component: ResetPasswordPage },
      { path: "/login", component: { template: "<div>login</div>" } },
      {
        path: "/forgot-password",
        component: { template: "<div>forgot</div>" },
      },
    ],
  });
  await router.push(`/reset-password${query}`);
  await router.isReady();
  const wrapper = mount(ResetPasswordPage, {
    global: { plugins: [pinia, router, i18n, ElementPlus] },
  });
  await flushPromises();
  return { wrapper, router };
}

describe("ResetPasswordPage", () => {
  it("updates the password and returns to login without signing in", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const reset = vi.spyOn(authService, "resetPassword").mockResolvedValue();
    const authStore = useAuthStore(pinia);
    const login = vi.spyOn(authStore, "login");

    const { wrapper, router } = await mountPage(
      `?email=officer@example.com&token=${encodeURIComponent(token)}`,
    );
    await wrapper
      .get('[data-testid="reset-password-new"]')
      .setValue("NewPassword123!");
    await wrapper
      .get('[data-testid="reset-password-confirm"]')
      .setValue("NewPassword123!");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(reset).toHaveBeenCalledWith(
      "officer@example.com",
      token,
      "NewPassword123!",
      "NewPassword123!",
    );
    expect(login).not.toHaveBeenCalled();
    expect(
      wrapper.get('[data-testid="reset-password-success"]').text(),
    ).toContain("authenticator app is still required");
    expect(wrapper.find('[data-testid="reset-password-new"]').exists()).toBe(
      false,
    );
    expect(window.location.pathname).not.toContain("token");

    await wrapper.get('[data-testid="reset-password-login"]').trigger("click");
    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/login");
    reset.mockRestore();
    login.mockRestore();
  });

  it("shows an incomplete link without a password form", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const { wrapper } = await mountPage();
    expect(
      wrapper.get('[data-testid="reset-password-invalid"]').text(),
    ).toContain("incomplete");
    expect(wrapper.find('[data-testid="reset-password-new"]').exists()).toBe(
      false,
    );
  });

  it("shows invalid, expired, and reused links the same way", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    vi.spyOn(authService, "resetPassword").mockRejectedValue({
      error: "auth.errors.invalidResetToken",
    });

    const { wrapper } = await mountPage(
      "?email=officer@example.com&token=used-token",
    );
    await wrapper
      .get('[data-testid="reset-password-new"]')
      .setValue("NewPassword123!");
    await wrapper
      .get('[data-testid="reset-password-confirm"]')
      .setValue("NewPassword123!");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(
      wrapper.get('[data-testid="reset-password-invalid"]').text(),
    ).toContain("invalid, expired, or already used");
    expect(wrapper.text()).not.toContain("used-token");
  });

  it("accepts 8 characters when the other rules are met", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const reset = vi.spyOn(authService, "resetPassword").mockResolvedValue();
    const { wrapper } = await mountPage(
      "?email=officer@example.com&token=still-good",
    );
    await wrapper
      .get('[data-testid="reset-password-new"]')
      .setValue("Ab1!wxyz");
    await wrapper
      .get('[data-testid="reset-password-confirm"]')
      .setValue("Ab1!wxyz");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(reset).toHaveBeenCalledWith(
      "officer@example.com",
      "still-good",
      "Ab1!wxyz",
      "Ab1!wxyz",
    );
    expect(wrapper.text()).not.toContain("at least 8 characters");
    reset.mockRestore();
  });

  it("rejects 7 characters that otherwise meet the rules", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const reset = vi.spyOn(authService, "resetPassword");
    const { wrapper } = await mountPage(
      "?email=officer@example.com&token=still-good",
    );
    await wrapper.get('[data-testid="reset-password-new"]').setValue("Ab1!xyz");
    await wrapper
      .get('[data-testid="reset-password-confirm"]')
      .setValue("Ab1!xyz");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(reset).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("at least 8 characters");
    reset.mockRestore();
  });

  it("keeps a weak password off the API", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(true),
    );
    const reset = vi.spyOn(authService, "resetPassword");
    const { wrapper } = await mountPage(
      "?email=officer@example.com&token=still-good",
    );
    await wrapper.get('[data-testid="reset-password-new"]').setValue("short");
    await wrapper
      .get('[data-testid="reset-password-confirm"]')
      .setValue("short");
    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(reset).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("at least 8 characters");
    expect(wrapper.find('[data-testid="reset-password-new"]').exists()).toBe(
      true,
    );
    reset.mockRestore();
  });

  it("hides the form when local officer sign-in is off", async () => {
    vi.spyOn(authService, "getSignInCapabilities").mockResolvedValue(
      capabilities(false),
    );
    const reset = vi.spyOn(authService, "resetPassword");
    const { wrapper } = await mountPage(
      "?email=officer@example.com&token=still-good",
    );

    expect(
      wrapper.get('[data-testid="reset-password-invalid"]').text(),
    ).toContain("not offered on this server");
    expect(wrapper.find('[data-testid="reset-password-new"]').exists()).toBe(
      false,
    );
    expect(reset).not.toHaveBeenCalled();
    reset.mockRestore();
  });
});
