import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "./authStore";

// Mock the secure token service
vi.mock("../services/secureTokenService", () => ({
  secureTokenService: {
    getAuthData: vi.fn(),
    clearAuthData: vi.fn(),
    refreshAuthData: vi.fn(),
    isAuthenticated: vi.fn(),
  },
}));

// Mock the auth service
vi.mock("../services/authService", () => ({
  authService: {
    register: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
  },
}));

// Mock the API error handler
vi.mock("../composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

// Mock localStorage (still needed for some legacy behavior)
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};

// Mock localStorage globally before tests
Object.defineProperty(window, "localStorage", {
  value: localStorageMock,
  writable: true,
});

describe("Auth Store", () => {
  let authStore: ReturnType<typeof useAuthStore>;

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    // Reset mocks to return null by default
    localStorageMock.getItem.mockReturnValue(null);
    localStorageMock.setItem.mockImplementation(() => {});
    localStorageMock.removeItem.mockImplementation(() => {});

    // Reset secure token service mock - import it fresh each time
    // This is needed because the mock is hoisted
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe("initial state", () => {
    it("should initialize with null values when cookies are empty", async () => {
      const { secureTokenService } =
        await import("../services/secureTokenService");
      vi.mocked(secureTokenService.getAuthData).mockReturnValue({
        token: null,
        refreshToken: null,
        email: null,
        name: null,
        authMethod: null,
      });
      vi.mocked(secureTokenService.isAuthenticated).mockReturnValue(false);

      authStore = useAuthStore();

      expect(authStore.email).toBeNull();
      expect(authStore.name).toBeNull();
      expect(authStore.authMethod).toBeNull();
      expect(authStore.requires2FA).toBe(false);
      expect(authStore.pending2FAEmail).toBeNull();
      expect(authStore.isAuthenticated).toBe(false);
    });

    it("should initialize with cookie values when cookies exist", async () => {
      const { secureTokenService } =
        await import("../services/secureTokenService");
      vi.mocked(secureTokenService.getAuthData).mockReturnValue({
        token: null, // httpOnly, can't read
        refreshToken: null, // httpOnly, can't read
        email: "stored@example.com",
        name: "Stored User",
        authMethod: "Local",
      });
      vi.mocked(secureTokenService.isAuthenticated).mockReturnValue(true);

      const freshStore = useAuthStore();
      expect(freshStore.email).toBe("stored@example.com");
      expect(freshStore.name).toBe("Stored User");
      expect(freshStore.authMethod).toBe("Local");
      expect(freshStore.isAuthenticated).toBe(true);
    });
  });

  describe("register", () => {
    it("should register user successfully without 2FA", async () => {
      const { authService } = await import("../services/authService");
      const { secureTokenService } =
        await import("../services/secureTokenService");

      const mockResponse = {
        email: "test@example.com",
        name: "Test User",
        authMethod: "Local",
        requires2FA: false,
      };

      vi.mocked(authService.register).mockResolvedValue(mockResponse);
      vi.mocked(secureTokenService.refreshAuthData).mockReturnValue({
        token: null,
        refreshToken: null,
        email: "test@example.com",
        name: "Test User",
        authMethod: "Local",
      });

      authStore = useAuthStore();

      const registerData = {
        username: "testuser",
        displayName: "Test User",
        email: "test@example.com",
        password: "password123",
        confirmPassword: "password123",
      };

      const result = await authStore.register(registerData);

      expect(authService.register).toHaveBeenCalledWith(registerData);
      expect(authStore.email).toBe("test@example.com");
      expect(authStore.name).toBe("Test User");
      expect(authStore.authMethod).toBe("Local");
      expect(authStore.requires2FA).toBe(false);
      expect(authStore.pending2FAEmail).toBeNull();
      expect(secureTokenService.refreshAuthData).toHaveBeenCalled();
      expect(result).toEqual(mockResponse);
    });

    it("should handle 2FA registration", async () => {
      const { authService } = await import("../services/authService");
      const { secureTokenService } =
        await import("../services/secureTokenService");

      // Reset to null values for this test
      vi.mocked(secureTokenService.getAuthData).mockReturnValue({
        token: null,
        refreshToken: null,
        email: null,
        name: null,
        authMethod: null,
      });

      const mockResponse = {
        email: "test@example.com",
        requires2FA: true,
      };

      vi.mocked(authService.register).mockResolvedValue(mockResponse);
      authStore = useAuthStore();

      const registerData = {
        username: "testuser",
        displayName: "Test User",
        email: "test@example.com",
        password: "password123",
        confirmPassword: "password123",
      };

      const result = await authStore.register(registerData);

      expect(authStore.email).toBeNull();
      expect(authStore.requires2FA).toBe(true);
      expect(authStore.pending2FAEmail).toBe("test@example.com");
      expect(result).toEqual(mockResponse);
    });

    it("should handle registration errors", async () => {
      const { authService } = await import("../services/authService");
      const mockError = new Error("Registration failed");
      vi.mocked(authService.register).mockRejectedValue(mockError);
      authStore = useAuthStore();

      const registerData = {
        username: "testuser",
        displayName: "Test User",
        email: "test@example.com",
        password: "password123",
        confirmPassword: "password123",
      };

      await expect(authStore.register(registerData)).rejects.toThrow(
        "Registration failed",
      );
    });
  });

  describe("login", () => {
    it("should login user successfully without 2FA", async () => {
      const { authService } = await import("../services/authService");
      const { secureTokenService } =
        await import("../services/secureTokenService");

      const mockResponse = {
        email: "login@example.com",
        name: "Login User",
        authMethod: "Local",
        requires2FA: false,
      };

      vi.mocked(authService.login).mockResolvedValue(mockResponse);
      vi.mocked(secureTokenService.refreshAuthData).mockReturnValue({
        token: null,
        refreshToken: null,
        email: "login@example.com",
        name: "Login User",
        authMethod: "Local",
      });

      authStore = useAuthStore();

      const loginData = {
        email: "login@example.com",
        password: "password123",
      };

      const result = await authStore.login(loginData);

      expect(authService.login).toHaveBeenCalledWith(loginData);
      expect(authStore.email).toBe("login@example.com");
      expect(authStore.name).toBe("Login User");
      expect(authStore.authMethod).toBe("Local");
      expect(authStore.requires2FA).toBe(false);
      expect(authStore.pending2FAEmail).toBeNull();
      expect(secureTokenService.refreshAuthData).toHaveBeenCalled();
      expect(result).toEqual(mockResponse);
    });

    it("should handle 2FA login", async () => {
      const { authService } = await import("../services/authService");
      const { secureTokenService } =
        await import("../services/secureTokenService");

      // Reset to null values for this test
      vi.mocked(secureTokenService.getAuthData).mockReturnValue({
        token: null,
        refreshToken: null,
        email: null,
        name: null,
        authMethod: null,
      });

      const mockResponse = {
        email: "test@example.com",
        requires2FA: true,
      };

      vi.mocked(authService.login).mockResolvedValue(mockResponse);
      authStore = useAuthStore();

      const loginData = {
        email: "test@example.com",
        password: "password123",
      };

      const result = await authStore.login(loginData);

      expect(authStore.email).toBeNull();
      expect(authStore.requires2FA).toBe(true);
      expect(authStore.pending2FAEmail).toBe("test@example.com");
      expect(result).toEqual(mockResponse);
    });

    it("should handle login errors", async () => {
      const { authService } = await import("../services/authService");
      const mockError = new Error("Login failed");
      vi.mocked(authService.login).mockRejectedValue(mockError);
      authStore = useAuthStore();

      const loginData = {
        email: "test@example.com",
        password: "password123",
      };

      await expect(authStore.login(loginData)).rejects.toThrow("Login failed");
    });
  });

  describe("logout", () => {
    it("should clear all auth data and cookies", async () => {
      const { authService } = await import("../services/authService");
      const { secureTokenService } =
        await import("../services/secureTokenService");

      vi.mocked(authService.logout).mockResolvedValue(undefined);
      vi.mocked(secureTokenService.clearAuthData).mockImplementation(() => {});
      vi.mocked(secureTokenService.isAuthenticated).mockReturnValue(false);

      authStore = useAuthStore();
      // Set up initial state
      authStore.email = "some@example.com";
      authStore.name = "Some User";
      authStore.authMethod = "Local";
      authStore.requires2FA = true;
      authStore.pending2FAEmail = "pending@example.com";

      await authStore.logout();

      expect(authService.logout).toHaveBeenCalled();
      expect(secureTokenService.clearAuthData).toHaveBeenCalled();
      expect(authStore.email).toBeNull();
      expect(authStore.name).toBeNull();
      expect(authStore.authMethod).toBeNull();
      expect(authStore.requires2FA).toBe(false);
      expect(authStore.pending2FAEmail).toBeNull();
      expect(authStore.isAuthenticated).toBe(false);
    });
  });

  describe("computed properties", () => {
    it("should return true for isAuthenticated when user has cookies", async () => {
      const { secureTokenService } =
        await import("../services/secureTokenService");
      vi.mocked(secureTokenService.isAuthenticated).mockReturnValue(true);

      authStore = useAuthStore();
      expect(authStore.isAuthenticated).toBe(true);
    });

    it("should return false for isAuthenticated when user has no cookies", async () => {
      const { secureTokenService } =
        await import("../services/secureTokenService");
      vi.mocked(secureTokenService.isAuthenticated).mockReturnValue(false);

      authStore = useAuthStore();
      expect(authStore.isAuthenticated).toBe(false);
    });
  });
});
