import { beforeEach, describe, expect, it, vi } from "vitest";
import { HubConnectionState } from "@microsoft/signalr";

const invoke = vi.fn();
const start = vi.fn().mockResolvedValue(undefined);
const stop = vi.fn().mockResolvedValue(undefined);
const on = vi.fn();
const onclose = vi.fn();
const onreconnecting = vi.fn();
const onreconnected = vi.fn();
const withUrl = vi.fn().mockReturnThis();

vi.mock("@microsoft/signalr", () => {
  class HubConnectionBuilder {
    withUrl(...args: unknown[]) {
      withUrl(...args);
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

describe("signalrService voter hubs", () => {
  beforeEach(async () => {
    vi.resetModules();
    invoke.mockReset().mockResolvedValue(undefined);
    start.mockClear();
    stop.mockClear();
    withUrl.mockClear();
    onreconnected.mockClear();
  });

  it("connectVoterHubs joins AllVoters then VoterPersonal with credentials only", async () => {
    const { signalrService } = await import("../signalrService");

    await signalrService.connectVoterHubs();

    expect(withUrl).toHaveBeenCalledWith(
      "http://localhost:5016/hubs/all-voters",
      expect.objectContaining({
        withCredentials: true,
      }),
    );
    expect(withUrl).toHaveBeenCalledWith(
      "http://localhost:5016/hubs/voter-personal",
      expect.objectContaining({
        withCredentials: true,
      }),
    );

    for (const call of withUrl.mock.calls) {
      const opts = call[1] as { accessTokenFactory?: unknown };
      expect(opts.accessTokenFactory).toBeUndefined();
    }

    const joinCalls = invoke.mock.calls.filter((c) => c[0] === "Join");
    expect(joinCalls).toHaveLength(2);
  });

  it("joinOnlineVoterElection invokes JoinElection and leave invokes LeaveElection", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectVoterHubs();
    invoke.mockClear();

    await signalrService.joinOnlineVoterElection("election-9");
    expect(invoke).toHaveBeenCalledWith("JoinElection", "election-9");

    invoke.mockClear();
    await signalrService.leaveOnlineVoterElection();
    expect(invoke).toHaveBeenCalledWith("LeaveElection");
  });

  it("re-invokes JoinElection after reconnect when election presence was joined", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectVoterHubs();
    await signalrService.joinOnlineVoterElection("election-9");
    invoke.mockClear();

    const reconnectHandlers = onreconnected.mock.calls.map((c) => c[0]);
    for (const handler of reconnectHandlers) {
      await handler("new-connection-id");
    }

    expect(invoke).toHaveBeenCalledWith("JoinElection", "election-9");
  });

  it("disconnectVoterHubs leaves then disconnects both hubs", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectVoterHubs();
    invoke.mockClear();
    stop.mockClear();

    await signalrService.disconnectVoterHubs();

    expect(invoke).toHaveBeenCalledWith("LeaveElection");
    expect(invoke).toHaveBeenCalledWith("Leave");
    expect(invoke.mock.calls.filter((c) => c[0] === "Leave")).toHaveLength(2);
    expect(stop).toHaveBeenCalledTimes(2);
  });

  it("re-invokes Join after reconnect when voter groups were joined", async () => {
    const { signalrService } = await import("../signalrService");
    await signalrService.connectVoterHubs();
    invoke.mockClear();

    expect(onreconnected).toHaveBeenCalled();
    const reconnectHandlers = onreconnected.mock.calls.map((c) => c[0]);
    expect(reconnectHandlers.length).toBeGreaterThanOrEqual(2);

    for (const handler of reconnectHandlers) {
      await handler("new-connection-id");
    }

    const rejoinCalls = invoke.mock.calls.filter((c) => c[0] === "Join");
    expect(rejoinCalls.length).toBeGreaterThanOrEqual(2);
  });
});
