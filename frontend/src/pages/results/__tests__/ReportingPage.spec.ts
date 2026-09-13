import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import ReportingPage from "../ReportingPage.vue";

const mockGetAvailableReports = vi.fn();
const mockGetReport = vi.fn();
const mockDownloadAllReports = vi.fn();

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "elec-1" } }),
  useRouter: () => ({ back: vi.fn() }),
}));

vi.mock("@/services/reportService", () => ({
  reportService: {
    getAvailableReports: (...args: unknown[]) =>
      mockGetAvailableReports(...args),
    getReport: (...args: unknown[]) => mockGetReport(...args),
    downloadAllReports: (...args: unknown[]) =>
      mockDownloadAllReports(...args),
  },
}));

vi.mock("@/components/results/ReportingReportBody.vue", () => ({
  default: {
    name: "ReportingReportBody",
    props: ["selectedReport", "selectedReportName", "reportData"],
    template: "<div class='report-body-stub' />",
  },
}));

const stubs = {
  ElButton: {
    props: ["disabled", "loading", "type"],
    template:
      "<button :disabled='disabled' @click='$emit(\"click\")'><slot /></button>",
  },
  ElSkeleton: { template: "<div />" },
};

describe("ReportingPage download all", () => {
  beforeEach(() => {
    mockGetAvailableReports.mockReset();
    mockGetReport.mockReset();
    mockDownloadAllReports.mockReset();
    mockGetAvailableReports.mockResolvedValue([
      { code: "VotersByArea", name: "Eligible and Voted by Area", category: "Voter Reports" },
    ]);
    mockDownloadAllReports.mockResolvedValue(new Blob(["zip"]));
    vi.stubGlobal("URL", {
      createObjectURL: vi.fn(() => "blob:reports"),
      revokeObjectURL: vi.fn(),
    });
  });

  it("downloads every listed report in one click", async () => {
    const click = vi.fn();
    const anchor = {
      href: "",
      download: "",
      click,
    } as unknown as HTMLAnchorElement;
    const createElement = vi
      .spyOn(document, "createElement")
      .mockImplementation((tag: string) => {
        if (tag === "a") {
          return anchor;
        }
        return document.createElement(tag);
      });
    const append = vi
      .spyOn(document.body, "appendChild")
      .mockImplementation((node) => node);
    const remove = vi.fn();
    Object.defineProperty(anchor, "remove", { value: remove });

    const wrapper = mount(ReportingPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    const buttons = wrapper.findAll("button");
    const download = buttons.find((b) =>
      b.text().includes("Download all reports"),
    );
    expect(download).toBeTruthy();
    await download!.trigger("click");
    await flushPromises();

    expect(mockDownloadAllReports).toHaveBeenCalledWith("elec-1");
    expect(click).toHaveBeenCalled();
    expect(anchor.download).toBe("election-reports.zip");

    createElement.mockRestore();
    append.mockRestore();
  });
});
