import { createPinia, setActivePinia } from "pinia";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Teller } from "@/types/teller";

vi.mock("@/services/tellerService", () => ({
  tellerService: {
    getTellersByElection: vi.fn(),
    getTellerById: vi.fn(),
    createTeller: vi.fn(),
    updateTeller: vi.fn(),
    deleteTeller: vi.fn(),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToMainHub: vi.fn(),
  },
}));

import { tellerService } from "@/services/tellerService";
import { signalrService } from "@/services/signalrService";
import { useTellerStore } from "./tellerStore";

const electionGuid = "elec-1";

function teller(overrides: Partial<Teller> = {}): Teller {
  return {
    rowId: 1,
    electionGuid,
    name: "Pat",
    ...overrides,
  };
}

describe("tellerStore", () => {
  let store: ReturnType<typeof useTellerStore>;

  beforeEach(() => {
    setActivePinia(createPinia());
    store = useTellerStore();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("createTeller adds the name and keeps the list alphabetical", async () => {
    store.tellers = [teller({ rowId: 2, name: "Zoe" })];
    store.totalCount = 1;
    vi.mocked(tellerService.createTeller).mockResolvedValue(
      teller({ rowId: 3, name: "Ann" }),
    );

    await store.createTeller(electionGuid, {
      electionGuid,
      name: "Ann",
    });

    expect(store.tellers.map((t) => t.name)).toEqual(["Ann", "Zoe"]);
    expect(store.totalCount).toBe(2);
  });

  it("deleteTeller removes the name from the election list", async () => {
    store.tellers = [
      teller({ rowId: 1, name: "Ann" }),
      teller({ rowId: 2, name: "Pat" }),
    ];
    store.totalCount = 2;
    vi.mocked(tellerService.deleteTeller).mockResolvedValue(true);

    await store.deleteTeller(electionGuid, 1);

    expect(store.tellers.map((t) => t.name)).toEqual(["Pat"]);
    expect(tellerService.deleteTeller).toHaveBeenCalledWith(electionGuid, 1);
  });

  it("registers tellersChanged on MainHub and applies added names alphabetically", async () => {
    const handlers = new Map<string, (data: unknown) => void>();
    vi.mocked(signalrService.connectToMainHub).mockResolvedValue({
      on: vi.fn((event: string, handler: (data: unknown) => void) => {
        handlers.set(event, handler);
      }),
    } as never);
    vi.mocked(tellerService.getTellersByElection).mockResolvedValue({
      items: [teller({ rowId: 1, name: "Zoe" })],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 50,
    });

    await store.fetchTellers(electionGuid);

    expect(handlers.has("tellersChanged")).toBe(true);

    handlers.get("tellersChanged")!({
      electionGuid,
      rowId: 4,
      name: "Ann",
      action: "added",
    });

    expect(store.tellers.map((t) => t.name)).toEqual(["Ann", "Zoe"]);
  });

  it("tellersChanged deleted removes the name without a refetch", async () => {
    store.listedElectionGuid = electionGuid;
    store.tellers = [
      teller({ rowId: 1, name: "Ann" }),
      teller({ rowId: 2, name: "Pat" }),
    ];
    store.totalCount = 2;

    store.applyTellerUpdate({
      electionGuid,
      rowId: 1,
      name: "Ann",
      action: "deleted",
    });

    expect(store.tellers.map((t) => t.name)).toEqual(["Pat"]);
    expect(store.totalCount).toBe(1);
  });

  it("ignores tellersChanged for another election", () => {
    store.listedElectionGuid = electionGuid;
    store.tellers = [teller()];

    store.applyTellerUpdate({
      electionGuid: "other-election",
      rowId: 9,
      name: "Chris",
      action: "added",
    });

    expect(store.tellers.map((t) => t.name)).toEqual(["Pat"]);
  });

  it("tellersChanged added is idempotent when the creating client already has the row", () => {
    store.listedElectionGuid = electionGuid;
    store.tellers = [teller({ rowId: 5, name: "Pat" })];
    store.totalCount = 1;

    store.applyTellerUpdate({
      electionGuid,
      rowId: 5,
      name: "Pat",
      action: "added",
    });

    expect(store.tellers).toHaveLength(1);
    expect(store.totalCount).toBe(1);
  });
});
