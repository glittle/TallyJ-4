import { describe, it, expect, vi, beforeEach } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import RegisterPage from "./RegisterPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { useAuthStore } from "@/stores/authStore";
import { authService } from "@/services/authService";

vi.mock("@/services/authService", () => ({
  authService: {
    peekAccountInvite: vi.fn(),
    registerWithInvite: vi.fn(),
  },
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

const mockedAuthService = vi.mocked(authService);

describe("RegisterPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("steers new tellers to Google and does not offer email signup without invite", async () => {
    await router.push("/register");
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Create a teller account");
    expect(wrapper.text()).toContain("Log in with Google");
    expect(wrapper.text()).toContain(
      "Open email and password signup is not available",
    );
    expect(wrapper.find("form").exists()).toBe(false);
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
    expect(wrapper.find('input[type="email"]').exists()).toBe(false);
    expect(mockedAuthService.peekAccountInvite).not.toHaveBeenCalled();
  });

  it("does not expose or call open register", async () => {
    await router.push("/register");
    const authStore = useAuthStore(pinia);
    expect(authStore).not.toHaveProperty("register");

    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(wrapper.find("form").exists()).toBe(false);
  });

  it("shows invite form when the one-time invite is valid", async () => {
    mockedAuthService.peekAccountInvite.mockResolvedValue({ valid: true });
    await router.push({ path: "/register", query: { invite: "valid-token" } });

    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(mockedAuthService.peekAccountInvite).toHaveBeenCalledWith(
      "valid-token",
    );
    expect(wrapper.text()).toContain("Create your teller account");
    expect(wrapper.find("form").exists()).toBe(true);
    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
  });

  it("rejects reused or expired invite and does not show the form", async () => {
    mockedAuthService.peekAccountInvite.mockResolvedValue({ valid: false });
    await router.push({ path: "/register", query: { invite: "dead-token" } });

    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(wrapper.find("form").exists()).toBe(false);
    expect(wrapper.text()).toContain(
      "This invite link is invalid, already used, or expired.",
    );
    expect(wrapper.text()).toContain("Log in with Google");
  });
});
