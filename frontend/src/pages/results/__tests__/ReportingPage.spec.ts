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
    downloadAllReports: (...args: unknown[]) => mockDownloadAllReports(...args),
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
      {
        code: "VotersByArea",
        name: "Eligible and Voted by Area",
        category: "Voter Reports",
      },
    ]);
    mockDownloadAllReports.mockResolvedValue(new Blob(["zip"]));
    vi.stubGlobal("URL", {
      createObjectURL: vi.fn(() => "blob:reports"),
      revokeObjectURL: vi.fn(),
    });
  });

  it("downloads every listed report in one click", async () => {
    const click = vi
      .spyOn(HTMLAnchorElement.prototype, "click")
      .mockImplementation(() => undefined);

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
    const clicked = click.mock.instances[0] as HTMLAnchorElement;
    expect(clicked.download).toBe("election-reports.zip");

    click.mockRestore();
  });
});
