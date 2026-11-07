import { describe, it, expect, vi } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import LoginPage from "./LoginPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import { useAuthStore } from "@/stores/authStore";
import ElementPlus from "element-plus";

describe("LoginPage", () => {
  it("renders login title", () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    expect(wrapper.text()).toContain("Login");
  });

  it("keeps password login and steers new tellers to Google", () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    expect(wrapper.text()).toContain(
      "New teller? Create an account with email and password, or sign in with Google",
    );
    expect(wrapper.find('a[href="/register"]').exists()).toBe(true);
  });

  it("asks for the authenticator code and does not open the app when the password is accepted", async () => {
    const authStore = useAuthStore(pinia);
    authStore.requires2FA = false;
    authStore.pending2FAEmail = null;
    const push = vi.spyOn(router, "push").mockResolvedValue(undefined as never);
    const login = vi.spyOn(authStore, "login").mockImplementation(async () => {
      authStore.requires2FA = true;
      authStore.pending2FAEmail = "stefan@example.com";
      return {
        email: "stefan@example.com",
        requires2FA: true,
      };
    });

    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    await wrapper.find('input[type="text"]').setValue("stefan@example.com");
    await wrapper.find('input[type="password"]').setValue("a-real-password");
    await wrapper.get("button.submit-btn").trigger("click");
    await flushPromises();

    expect(wrapper.find('[data-testid="login-2fa-code"]').exists()).toBe(true);
    expect(wrapper.text()).toContain("Two-factor authentication is required");
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
    expect(push).not.toHaveBeenCalled();
    login.mockRestore();
    push.mockRestore();
  });

  it("stays on the authenticator step when the code is rejected", async () => {
    const authStore = useAuthStore(pinia);
    authStore.requires2FA = false;
    authStore.pending2FAEmail = null;
    const push = vi.spyOn(router, "push").mockResolvedValue(undefined as never);
    vi.spyOn(authStore, "verify2FA").mockRejectedValue(new Error("bad code"));

    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    await wrapper.find('input[type="text"]').setValue("stefan@example.com");
    await wrapper.find('input[type="password"]').setValue("a-real-password");
    authStore.requires2FA = true;
    authStore.pending2FAEmail = "stefan@example.com";
    await flushPromises();

    await wrapper.get('[data-testid="login-2fa-code"]').setValue("000000");
    await wrapper.get('[data-testid="login-2fa-submit"]').trigger("click");
    await flushPromises();

    expect(authStore.verify2FA).toHaveBeenCalledWith({
      email: "stefan@example.com",
      password: "a-real-password",
      code: "000000",
    });
    expect(wrapper.find('[data-testid="login-2fa-code"]').exists()).toBe(true);
    expect(push).not.toHaveBeenCalled();
    authStore.requires2FA = false;
    authStore.pending2FAEmail = null;
    push.mockRestore();
  });

  it("does not submit a short authenticator code", async () => {
    const authStore = useAuthStore(pinia);
    authStore.requires2FA = false;
    authStore.pending2FAEmail = null;
    const verify = vi
      .spyOn(authStore, "verify2FA")
      .mockResolvedValue({ email: "stefan@example.com", requires2FA: false });

    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    await wrapper.find('input[type="password"]').setValue("a-real-password");
    authStore.requires2FA = true;
    authStore.pending2FAEmail = "stefan@example.com";
    await flushPromises();

    await wrapper.get('[data-testid="login-2fa-code"]').setValue("12345");
    await wrapper.get('[data-testid="login-2fa-submit"]').trigger("click");
    await flushPromises();

    expect(verify).not.toHaveBeenCalled();
    expect(wrapper.find('[data-testid="login-2fa-code"]').exists()).toBe(true);
    wrapper.unmount();
    verify.mockRestore();
  });

  it("returns to the password step after the authenticator page is left and opened again", async () => {
    const authStore = useAuthStore(pinia);
    authStore.requires2FA = false;
    authStore.pending2FAEmail = null;
    const verify = vi
      .spyOn(authStore, "verify2FA")
      .mockResolvedValue({ email: "stefan@example.com", requires2FA: false });

    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });

    await wrapper.find('input[type="text"]').setValue("stefan@example.com");
    await wrapper.find('input[type="password"]').setValue("a-real-password");
    authStore.requires2FA = true;
    authStore.pending2FAEmail = "stefan@example.com";
    await flushPromises();
    expect(wrapper.find('[data-testid="login-2fa-code"]').exists()).toBe(true);

    wrapper.unmount();
    expect(authStore.requires2FA).toBe(false);
    expect(authStore.pending2FAEmail).toBeNull();

    authStore.requires2FA = true;
    authStore.pending2FAEmail = "stefan@example.com";
    const again = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(again.find('input[type="password"]').exists()).toBe(true);
    expect(again.find('[data-testid="login-2fa-code"]').exists()).toBe(false);
    expect(verify).not.toHaveBeenCalled();
    again.unmount();
    expect(authStore.requires2FA).toBe(false);
    verify.mockRestore();
  });
});
