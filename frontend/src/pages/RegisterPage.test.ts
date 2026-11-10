import { describe, it, expect, vi, beforeEach } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import RegisterPage from "./RegisterPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { authService } from "@/services/authService";

const offeredSignIn = {
  officers: {
    local: true,
    google: true,
    facebook: false,
    kakao: false,
    telegram: false,
  },
  voters: {
    google: false,
    facebook: false,
    kakao: false,
    telegram: false,
    emailCode: false,
    phoneCode: false,
    sms: false,
    voice: false,
    whatsApp: false,
    directCode: false,
  },
};

vi.mock("@/services/authService", () => ({
  authService: {
    peekAccountInvite: vi.fn(),
    registerWithInvite: vi.fn(),
    register: vi.fn(),
    getSignInCapabilities: vi.fn(),
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
    mockedAuthService.getSignInCapabilities.mockResolvedValue(offeredSignIn);
  });

  it("offers email signup and Google without an invite", async () => {
    await router.push("/register");
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Create your teller account");
    expect(wrapper.text()).toContain("Log in with Google");
    expect(wrapper.text()).toContain("restricted until a site administrator");
    expect(wrapper.find("form").exists()).toBe(true);
    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    expect(mockedAuthService.peekAccountInvite).not.toHaveBeenCalled();
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

  it("hides password signup when local sign-in is off and keeps a valid invite from showing the form", async () => {
    mockedAuthService.getSignInCapabilities.mockResolvedValue({
      ...offeredSignIn,
      officers: { ...offeredSignIn.officers, local: false, google: false },
    });
    mockedAuthService.peekAccountInvite.mockResolvedValue({ valid: true });
    await router.push({ path: "/register", query: { invite: "valid-token" } });

    const wrapper = mount(RegisterPage, {
      global: { plugins: [pinia, router, i18n, ElementPlus] },
    });
    await flushPromises();

    expect(wrapper.find("form").exists()).toBe(false);
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="register-sign-in-none"]').exists()).toBe(
      true,
    );
  });

  it("hides Google signup when officer Google is off and still shows the password form", async () => {
    mockedAuthService.getSignInCapabilities.mockResolvedValue({
      ...offeredSignIn,
      officers: { ...offeredSignIn.officers, google: false },
    });
    await router.push("/register");

    const wrapper = mount(RegisterPage, {
      global: { plugins: [pinia, router, i18n, ElementPlus] },
    });
    await flushPromises();

    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    expect(wrapper.text()).not.toContain("Log in with Google");
  });
});
