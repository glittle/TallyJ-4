import { describe, expect, it, beforeEach, vi } from "vitest";
import { effectScope, nextTick, watch } from "vue";
import {
  resetComputerCodeCache,
  setComputerCode,
} from "@/utils/computerCodeStorage";
import { useComputerCode } from "../useComputerCode";

const electionGuid = "election-for-computer-code";

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: electionGuid } }),
}));

describe("useComputerCode", () => {
  beforeEach(() => {
    localStorage.clear();
    resetComputerCodeCache();
  });

  it("reacts when SignalR assigns a code after the composable cached empty", async () => {
    const scope = effectScope();
    const seen: string[] = [];

    scope.run(() => {
      const { computerCode } = useComputerCode();
      // Simulate header/badge reading before join completes.
      expect(computerCode.value).toBe("");

      watch(
        computerCode,
        (value) => {
          seen.push(value);
        },
        { immediate: true },
      );
    });

    setComputerCode(electionGuid, "AB");
    await nextTick();
    expect(seen).toEqual(["", "AB"]);

    scope.stop();
  });
});