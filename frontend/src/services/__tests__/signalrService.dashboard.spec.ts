import { beforeEach, describe, expect, it, vi } from "vitest";
import { HubConnectionState } from "@microsoft/signalr";

const invoke = vi.fn();
const start = vi.fn().mockResolvedValue(undefined);
const stop = vi.fn().mockResolvedValue(undefined);
const on = vi.fn();
const onclose = vi.fn();
const onreconnecting = vi.fn();
const onreconnected = vi.fn();

vi.mock("@microsoft/signalr", () => {
  class HubConnectionBuilder {
    withUrl() {
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    configureLogging() {
      return this;
    }
    build() {
      return {
        state: HubConnectionState.Connected,
        start,
        stop,
        invoke,
        on,
        onclose,
        onreconnecting,
        onreconnected,
      };
    }
  }
  return {
    HubConnectionBuilder,
    HubConnectionState: {
      Connected: "Connected",
      Disconnected: "Disconnected",
      Connecting: "Connecting",
      Reconnecting: "Reconnecting",
    },
    LogLevel: { Warning: 2 },
  };
});

vi.mock("@/config/appConfig", () => ({
  getAppConfig: () => ({ apiUrl: "http://localhost:5016" }),
}));

vi.mock("@/utils/clientIdStorage", () => ({
  getOrCreateClientId: () => "client-1",
}));

vi.mock("@/utils/computerCodeStorage", () => ({
  setComputerCode: vi.fn(),
}));

describe("signalrService dashboard multi-join", () => {
  beforeEach(async () => {
    vi.resetModules();
    invoke.mockReset().mockResolvedValue(undefined);
    start.mockClear();
    stop.mockClear();
  });

  it("invokes JoinElections and tracks guids for leave", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectToMainHub();

    await signalrService.joinDashboardElections(["e1", "e2", "e1", "  "]);

    expect(invoke).toHaveBeenCalledWith("JoinElections", ["e1", "e2"]);

    await signalrService.leaveDashboardElections();
    expect(invoke).toHaveBeenCalledWith("LeaveElections", ["e1", "e2"]);
  });

  it("preserves active main election when leaving dashboard set", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectToMainHub();
    invoke.mockResolvedValueOnce("A"); // JoinElection code
    await signalrService.joinElection("e1");
    invoke.mockClear();

    await signalrService.joinDashboardElections(["e1", "e2"]);
    expect(invoke).toHaveBeenCalledWith("JoinElections", ["e1", "e2"]);
    invoke.mockClear();

    await signalrService.leaveDashboardElections();
    // e1 is the active workstation election — do not LeaveElections it
    expect(invoke).toHaveBeenCalledWith("LeaveElections", ["e2"]);
  });
});
