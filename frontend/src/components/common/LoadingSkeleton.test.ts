import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import LoadingSkeleton from "./LoadingSkeleton.vue";

describe("LoadingSkeleton", () => {
  it("renders with default props", () => {
    const wrapper = mount(LoadingSkeleton);
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.classes()).toContain("loading-skeleton");
  });

  it("applies default width and height", () => {
    const wrapper = mount(LoadingSkeleton);
    const skeleton = wrapper.find(".loading-skeleton");
    expect(skeleton.attributes("style")).toContain("width: 100%");
    expect(skeleton.attributes("style")).toContain("height: 20px");
  });

  it("applies custom width and height", () => {
    const wrapper = mount(LoadingSkeleton, {
      props: {
        width: "200px",
        height: "40px",
      },
    });
    const skeleton = wrapper.find(".loading-skeleton");
    expect(skeleton.attributes("style")).toContain("width: 200px");
    expect(skeleton.attributes("style")).toContain("height: 40px");
  });

  it("contains shimmer animation element", () => {
    const wrapper = mount(LoadingSkeleton);
    const shimmer = wrapper.find(".skeleton-shimmer");
    expect(shimmer.exists()).toBe(true);
  });

  it("has proper CSS classes", () => {
    const wrapper = mount(LoadingSkeleton);
    expect(wrapper.classes()).toContain("loading-skeleton");
    expect(wrapper.find(".skeleton-shimmer").classes()).toContain(
      "skeleton-shimmer",
    );
  });
});
