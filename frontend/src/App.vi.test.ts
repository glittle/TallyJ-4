import { describe, it, expect, vi, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import App from "./App.vue";
import { pinia, router, i18n } from "./test/setup";

vi.mock("./components/common/ErrorBoundary.vue", () => ({
  default: {
    name: "ErrorBoundary",
    template: "<div><slot /></div>",
  },
}));

const globalMountOptions = {
  global: {
    plugins: [pinia, router, i18n],
    stubs: ["RouterView"],
  },
};

describe("App", () => {
  const originalBranchName = process.env.BRANCH_NAME;

  afterEach(() => {
    process.env.BRANCH_NAME = originalBranchName;
  });

  it("renders properly", () => {
    const wrapper = mount(App, globalMountOptions);
    expect(wrapper.exists()).toBe(true);
  });

  it("displays branch name when not on main branch", async () => {
    process.env.BRANCH_NAME = "feature/test-branch";

    const wrapper = mount(App, globalMountOptions);
    await wrapper.vm.$nextTick();

    const branchElement = wrapper.find(".bottomCorner");
    expect(branchElement.exists()).toBe(true);
    expect(branchElement.text()).toContain("Branch: feature/test-branch");
  });

  it("does not display branch name when on HEAD", async () => {
    process.env.BRANCH_NAME = "HEAD";

    const wrapper = mount(App, globalMountOptions);
    await wrapper.vm.$nextTick();

    expect(wrapper.find(".bottomCorner").exists()).toBe(false);
  });

  it("hides branch name when clicked", async () => {
    process.env.BRANCH_NAME = "feature/test-branch";

    const wrapper = mount(App, globalMountOptions);
    await wrapper.vm.$nextTick();

    const branchElement = wrapper.find(".bottomCorner");
    expect(branchElement.exists()).toBe(true);

    await branchElement.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.find(".bottomCorner").exists()).toBe(false);
  });

  it("has correct title attribute on branch display", async () => {
    process.env.BRANCH_NAME = "feature/test-branch";

    const wrapper = mount(App, globalMountOptions);
    await wrapper.vm.$nextTick();

    const branchElement = wrapper.find(".bottomCorner");
    expect(branchElement.attributes("title")).toBe("Click to remove");
  });
});
