import { describe, expect, it, vi } from "vitest";

vi.mock("@/api/gen/configService/sdk.gen", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/api/gen/configService/sdk.gen")>();
  return {
    ...actual,
    putApiElectionsByGuidStage: vi.fn(),
  };
});

import { putApiElectionsByGuidStage } from "@/api/gen/configService/sdk.gen";
import { electionService } from "../electionService";

describe("electionService.changeStage", () => {
  it("sends confirmLeavingFinalized when leaving Finalized", async () => {
    vi.mocked(putApiElectionsByGuidStage).mockResolvedValue({
      data: {
        success: true,
        data: {
          electionGuid: "election-1",
          electionStage: "ProcessingBallots",
        },
      },
    } as never);

    await electionService.changeStage(
      "election-1",
      "ProcessingBallots",
      true,
    );

    expect(putApiElectionsByGuidStage).toHaveBeenCalledWith({
      path: { guid: "election-1" },
      body: {
        electionStage: "ProcessingBallots",
        confirmLeavingFinalized: true,
      },
    });
  });

  it("sends confirmLeavingFinalized false by default", async () => {
    vi.mocked(putApiElectionsByGuidStage).mockResolvedValue({
      data: {
        success: true,
        data: {
          electionGuid: "election-1",
          electionStage: "Finalized",
        },
      },
    } as never);

    await electionService.changeStage("election-1", "Finalized");

    expect(putApiElectionsByGuidStage).toHaveBeenCalledWith({
      path: { guid: "election-1" },
      body: {
        electionStage: "Finalized",
        confirmLeavingFinalized: false,
      },
    });
  });
});
