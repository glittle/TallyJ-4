import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  ensurePeopleImportHubBound,
  resetPeopleImportHubBindingForTests,
  setPeopleImportHubListener,
} from "../peopleImportHubBinding";

describe("ensurePeopleImportHubBound", () => {
  beforeEach(() => {
    resetPeopleImportHubBindingForTests();
  });

  it("registers importError once across repeated calls", async () => {
    const handlers = new Map<string, (payload?: unknown) => void>();
    const connect = vi.fn(async () => ({
      on: (event: string, handler: (payload?: unknown) => void) => {
        handlers.set(event, handler);
      },
    }));

    await ensurePeopleImportHubBound(connect);
    await ensurePeopleImportHubBound(connect);

    expect(connect).toHaveBeenCalledTimes(1);
    expect(handlers.size).toBe(3);

    const errors: string[] = [];
    setPeopleImportHubListener({
      onProgress: () => undefined,
      onError: (message) => errors.push(message),
      onComplete: () => undefined,
    });
    handlers.get("importError")?.("people.import.noFileProvided");
    expect(errors).toEqual(["people.import.noFileProvided"]);
  });
});
