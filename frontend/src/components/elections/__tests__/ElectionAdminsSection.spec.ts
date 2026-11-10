import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import ElectionAdminsSection from "../ElectionAdminsSection.vue";

const list = vi.fn();
const add = vi.fn();
const remove = vi.fn();
const showSuccessMessage = vi.fn();
const handleApiError = vi.fn();
const mockConfirm = vi.fn();

vi.mock("@/services/electionAdminService", () => ({
  electionAdminService: {
    list: (...args: unknown[]) => list(...args),
    add: (...args: unknown[]) => add(...args),
    remove: (...args: unknown[]) => remove(...args),
  },
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({ showSuccessMessage }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError }),
}));

vi.mock("element-plus", async () => {
  const actual =
    await vi.importActual<typeof import("element-plus")>("element-plus");
  return {
    ...actual,
    ElMessageBox: {
      confirm: (...args: unknown[]) => mockConfirm(...args),
    },
  };
});

const owner = {
  userId: "owner-1",
  email: "owner@example.com",
  displayName: "Ada Owner",
  role: "Admin",
  canRemove: false,
};

const coAdmin = {
  userId: "co-1",
  email: "co@example.com",
  displayName: "Bo Co",
  role: "Admin",
  canRemove: true,
};

function mountSection() {
  return mount(ElectionAdminsSection, {
    props: { electionGuid: "election-1" },
    global: { plugins: [i18n, ElementPlus] },
  });
}

describe("ElectionAdminsSection", () => {
  beforeEach(() => {
    list.mockReset();
    add.mockReset();
    remove.mockReset();
    showSuccessMessage.mockReset();
    handleApiError.mockReset();
    mockConfirm.mockReset();
  });

  it("lists admins and adds one by email", async () => {
    list
      .mockResolvedValueOnce([owner])
      .mockResolvedValueOnce([{ ...owner, canRemove: true }, coAdmin]);
    add.mockResolvedValue({ admin: coAdmin, alreadyAdmin: false });

    const wrapper = mountSection();
    await flushPromises();

    expect(list).toHaveBeenCalledWith("election-1");
    expect(wrapper.text()).toContain("Ada Owner");
    expect(wrapper.text()).toContain("owner@example.com");
    expect(
      wrapper.find('[data-testid="remove-election-admin-owner-1"]').exists(),
    ).toBe(false);

    await wrapper
      .get('[data-testid="election-admin-email"]')
      .setValue("  co@example.com  ");
    await wrapper.get('[data-testid="add-election-admin"]').trigger("click");
    await flushPromises();

    expect(add).toHaveBeenCalledWith("election-1", "co@example.com");
    expect(wrapper.text()).toContain("Bo Co");
    expect(
      wrapper.find('[data-testid="remove-election-admin-co-1"]').exists(),
    ).toBe(true);
    expect(showSuccessMessage).toHaveBeenCalledWith("Admin added.");
  });

  it("removes a co-admin after confirmation", async () => {
    list
      .mockResolvedValueOnce([{ ...owner, canRemove: true }, coAdmin])
      .mockResolvedValueOnce([owner]);
    mockConfirm.mockResolvedValue("confirm");
    remove.mockResolvedValue(undefined);

    const wrapper = mountSection();
    await flushPromises();

    await wrapper
      .get('[data-testid="remove-election-admin-co-1"]')
      .trigger("click");
    await flushPromises();

    expect(mockConfirm).toHaveBeenCalled();
    expect(remove).toHaveBeenCalledWith("election-1", "co-1");
    expect(wrapper.text()).not.toContain("Bo Co");
    expect(showSuccessMessage).toHaveBeenCalledWith("Admin removed.");
  });

  it("does not remove when confirmation is cancelled", async () => {
    list.mockResolvedValue([{ ...owner, canRemove: true }, coAdmin]);
    mockConfirm.mockRejectedValue("cancel");

    const wrapper = mountSection();
    await flushPromises();

    await wrapper
      .get('[data-testid="remove-election-admin-co-1"]')
      .trigger("click");
    await flushPromises();

    expect(remove).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Bo Co");
  });
});
