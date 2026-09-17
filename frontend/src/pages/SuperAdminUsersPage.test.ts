import { describe, it, expect, vi, beforeEach } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import SuperAdminUsersPage from "./SuperAdminUsersPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { superAdminService } from "@/services/superAdminService";

vi.mock("@/services/superAdminService", () => ({
  superAdminService: {
    getUsers: vi.fn(),
    createAccountInvite: vi.fn(),
  },
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

const mockedService = vi.mocked(superAdminService);

describe("SuperAdminUsersPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedService.getUsers.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 25,
      totalPages: 0,
    });
  });

  it("issues a one-time invite link for SuperAdmin", async () => {
    mockedService.createAccountInvite.mockResolvedValue({
      token: "raw-token",
      inviteUrl: "https://localhost:8095/register?invite=raw-token",
      expiresAt: new Date().toISOString(),
    });

    const wrapper = mount(SuperAdminUsersPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    const issueButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Issue email/password invite"));
    expect(issueButton).toBeTruthy();
    await issueButton!.trigger("click");
    await flushPromises();

    expect(mockedService.createAccountInvite).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain("One-time account invite");
    const urlInput = wrapper.find("input[readonly]");
    expect(urlInput.exists()).toBe(true);
    expect((urlInput.element as HTMLInputElement).value).toBe(
      "https://localhost:8095/register?invite=raw-token",
    );
  });
});
