import { describe, it, expect, vi } from "vitest";
import { mount } from "@vue/test-utils";
import RegisterPage from "./RegisterPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { useAuthStore } from "@/stores/authStore";

describe("RegisterPage", () => {
  it("renders register title", () => {
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    expect(wrapper.text()).toContain("Register");
  });

  it("renders properly", () => {
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    expect(wrapper.exists()).toBe(true);
  });

  describe("Password Validation", () => {
    it("validates password minimum length of 12 characters", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      // Test the validation function directly
      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "Short1!", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });

    it("validates password requires lowercase letter", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "PASSWORD123!", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });

    it("validates password requires uppercase letter", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "password123!", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });

    it("validates password requires digit", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "Password!", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });

    it("validates password requires special character", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "Password123", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });

    it("accepts valid password that meets all requirements", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePassword = (wrapper.vm as any).validatePassword;
      let hasError = false;

      await validatePassword(null, "ValidPassword123!", (_error: any) => {
        if (_error) {
          hasError = true;
        }
      });

      expect(hasError).toBe(false);
    });

    it("validates password confirmation matches", async () => {
      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      const validatePass2 = (wrapper.vm as any).validatePass2;
      let hasError = false;

      // Set password first
      (wrapper.vm as any).registerForm.password = "ValidPassword123!";

      await validatePass2(null, "DifferentPassword123!", (_error: any) => {
        hasError = true;
      });

      expect(hasError).toBe(true);
    });
  });

  describe("Registration Flow", () => {
    it("calls auth store register method on form submission", async () => {
      const authStore = useAuthStore(pinia);
      const registerSpy = vi.fn().mockResolvedValue(undefined);
      authStore.register = registerSpy;

      const wrapper = mount(RegisterPage, {
        global: {
          plugins: [pinia, router, i18n, ElementPlus],
        },
      });

      // Mock form validation to return valid
      const formRef = {
        validate: vi.fn().mockImplementation((callback) => callback(true)),
      };
      (wrapper.vm as any).registerFormRef = formRef;

      // Set form data directly on the component
      const vm = wrapper.vm as any;
      vm.registerForm.email = "test@example.com";
      vm.registerForm.password = "ValidPassword123!";
      vm.registerForm.confirmPassword = "ValidPassword123!";

      // Call handleRegister
      await (wrapper.vm as any).handleRegister();

      // Check that register was called with correct data
      expect(registerSpy).toHaveBeenCalledWith({
        email: "test@example.com",
        password: "ValidPassword123!",
        confirmPassword: "ValidPassword123!",
      });
    });
  });
});
