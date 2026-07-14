import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { ref } from "vue";
import { useSuperAdminStore } from "./superAdminStore";

const ensureUserInfoLoaded = vi.fn();
const isSuperAdmin = ref(false);

vi.mock("./authStore", () => ({
  useAuthStore: () => ({
    get isSuperAdmin() {
      return isSuperAdmin.value;
    },
    ensureUserInfoLoaded,
  }),
}));

vi.mock("@/services/superAdminService", () => ({
  superAdminService: {
    getSummary: vi.fn(),
  },
}));

describe("superAdminStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    isSuperAdmin.value = false;
    ensureUserInfoLoaded.mockImplementation(async () => {
      isSuperAdmin.value = true;
      return true;
    });
  });

  it("delegates status check to authStore.ensureUserInfoLoaded", async () => {
    const store = useSuperAdminStore();

    const result = await store.checkSuperAdminStatus();

    expect(ensureUserInfoLoaded).toHaveBeenCalledTimes(1);
    expect(result).toBe(true);
    expect(store.isSuperAdmin).toBe(true);
  });

  it("reflects non–super-admin from auth store", async () => {
    ensureUserInfoLoaded.mockResolvedValue(false);
    isSuperAdmin.value = false;
    const store = useSuperAdminStore();

    const result = await store.checkSuperAdminStatus();

    expect(result).toBe(false);
    expect(store.isSuperAdmin).toBe(false);
  });
});
