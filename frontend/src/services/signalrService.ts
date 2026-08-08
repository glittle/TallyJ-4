import { getAppConfig } from "@/config/appConfig";
import { ConnectionState } from "@/types/SignalRConnection";
import { getOrCreateClientId } from "@/utils/clientIdStorage";
import { setComputerCode } from "@/utils/computerCodeStorage";
import * as signalR from "@microsoft/signalr";

class SignalRService {
  private readonly connections: Map<string, signalR.HubConnection> = new Map();
  private readonly connectionStates: Map<string, ConnectionState> = new Map();
  private frontDeskElectionGuid: string | null = null;
  private mainElectionGuid: string | null = null;
  /** Known-teller dashboard multi-election listen set (MainHub JoinElections). */
  private dashboardElectionGuids: string[] = [];
  private publicGroupJoined = false;
  /** Online voter hubs (Bearer voter JWT from localStorage). */
  private allVotersJoined = false;
  private voterPersonalJoined = false;
  private voterAccessTokenFactory: (() => string | null) | null = null;

  private get baseUrl(): string {
    return getAppConfig().apiUrl;
  }

  /**
   * Connect to a hub. Optional accessTokenFactory is used for online-voter hubs
   * (Bearer JWT in localStorage); teller hubs rely on cookies / default auth.
   */
  async connect(
    hubPath: string,
    accessTokenFactory?: () => string | null,
  ): Promise<signalR.HubConnection> {
    const existingConnection = this.connections.get(hubPath);
    if (existingConnection?.state === signalR.HubConnectionState.Connected) {
      return existingConnection;
    }

    const hubUrl = `${this.baseUrl}${hubPath}`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Cookies are sent automatically by the browser with the initial HTTP request.
        // Online-voter hubs also send the voter JWT via access_token / Authorization.
        withCredentials: true,
        accessTokenFactory: accessTokenFactory
          ? () => accessTokenFactory() ?? ""
          : undefined,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.onclose((error) => {
      console.error(`SignalR connection closed for ${hubPath}:`, error);
      this.connectionStates.set(hubPath, ConnectionState.Disconnected);
    });

    connection.onreconnecting((error) => {
      console.warn(`SignalR reconnecting for ${hubPath}:`, error);
      this.connectionStates.set(hubPath, ConnectionState.Reconnecting);
    });

    connection.onreconnected(async (connectionId) => {
      console.log(
        `SignalR reconnected for ${hubPath}. Connection ID: ${connectionId}`,
      );
      this.connectionStates.set(hubPath, ConnectionState.Connected);
      if (hubPath === "/hubs/main") {
        if (this.mainElectionGuid) {
          try {
            const assignedCode = (await connection.invoke(
              "JoinElection",
              this.mainElectionGuid,
              getOrCreateClientId(),
            )) as string;
            if (assignedCode) {
              setComputerCode(this.mainElectionGuid, assignedCode);
            }
            console.log(
              `Rejoined main election ${this.mainElectionGuid} after reconnect with code ${assignedCode}`,
            );
          } catch (error) {
            console.error(
              "Failed to rejoin main election after reconnect:",
              error,
            );
          }
        }

        if (this.dashboardElectionGuids.length > 0) {
          try {
            await connection.invoke(
              "JoinElections",
              this.dashboardElectionGuids,
            );
            console.log(
              `Rejoined ${this.dashboardElectionGuids.length} dashboard elections after reconnect`,
            );
          } catch (error) {
            console.error(
              "Failed to rejoin dashboard elections after reconnect:",
              error,
            );
          }
        }
      }

      if (hubPath === "/hubs/front-desk" && this.frontDeskElectionGuid) {
        try {
          await connection.invoke("JoinElection", this.frontDeskElectionGuid);
          console.log(
            `Rejoined front desk election ${this.frontDeskElectionGuid} after reconnect`,
          );
        } catch (error) {
          console.error(
            "Failed to rejoin front desk election after reconnect:",
            error,
          );
        }
      }

      if (hubPath === "/hubs/public" && this.publicGroupJoined) {
        try {
          await connection.invoke("JoinPublicGroup");
          console.log("Rejoined public group after reconnect");
        } catch (error) {
          console.error(
            "Failed to rejoin public group after reconnect:",
            error,
          );
        }
      }

      if (hubPath === "/hubs/all-voters" && this.allVotersJoined) {
        try {
          await connection.invoke("Join");
          console.log("Rejoined AllVoters group after reconnect");
        } catch (error) {
          console.error(
            "Failed to rejoin AllVoters group after reconnect:",
            error,
          );
        }
      }

      if (hubPath === "/hubs/voter-personal" && this.voterPersonalJoined) {
        try {
          await connection.invoke("Join");
          console.log("Rejoined VoterPersonal group after reconnect");
        } catch (error) {
          console.error(
            "Failed to rejoin VoterPersonal group after reconnect:",
            error,
          );
        }
      }
    });

    this.connectionStates.set(hubPath, ConnectionState.Connecting);

    try {
      await connection.start();
      this.connections.set(hubPath, connection);
      this.connectionStates.set(hubPath, ConnectionState.Connected);
      console.log(`SignalR connected to ${hubPath}`);
      return connection;
    } catch (error) {
      console.error(`Error connecting to SignalR hub ${hubPath}:`, error);
      this.connectionStates.set(hubPath, ConnectionState.Disconnected);
      throw error;
    }
  }

  async disconnect(hubPath: string): Promise<void> {
    const connection = this.connections.get(hubPath);
    if (connection) {
      try {
        await connection.stop();
        this.connections.delete(hubPath);
        this.connectionStates.set(hubPath, ConnectionState.Disconnected);
        console.log(`SignalR disconnected from ${hubPath}`);
      } catch (error) {
        console.error(
          `Error disconnecting from SignalR hub ${hubPath}:`,
          error,
        );
      }
    }
  }

  async disconnectAll(): Promise<void> {
    const disconnectPromises = Array.from(this.connections.keys()).map(
      (hubPath) => this.disconnect(hubPath),
    );
    await Promise.all(disconnectPromises);
  }

  getConnection(hubPath: string): signalR.HubConnection | undefined {
    return this.connections.get(hubPath);
  }

  getConnectionState(hubPath: string): ConnectionState {
    return this.connectionStates.get(hubPath) || ConnectionState.Disconnected;
  }

  async connectToMainHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/main");
  }

  async connectToAnalyzeHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/analyze");
  }

  async connectToBallotImportHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/ballot-import");
  }

  async connectToPublicHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/public");
  }

  async connectToFrontDeskHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/front-desk");
  }

  async connectToPeopleImportHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/people-import");
  }

  async connectToElectionPackageImportHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/election-package-import");
  }

  /**
   * Join user-scoped election package load progress (known teller only).
   * Group is derived server-side from the auth user id.
   */
  async joinElectionPackageImportSession(): Promise<void> {
    const connection = await this.connectToElectionPackageImportHub();
    await connection.invoke("JoinSession");
  }

  async leaveElectionPackageImportSession(): Promise<void> {
    const connection = this.getConnection("/hubs/election-package-import");
    if (connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke("LeaveSession");
      } catch (error) {
        console.warn("Failed to leave election package import session:", error);
      }
    }
  }

  async joinElection(electionGuid: string): Promise<string | null> {
    const previousGuid = this.mainElectionGuid;
    if (previousGuid && previousGuid !== electionGuid) {
      await this.leaveElection(previousGuid);
    }

    this.mainElectionGuid = electionGuid;
    const clientId = getOrCreateClientId();
    let assignedCode: string | null = null;

    const mainConnection = this.getConnection("/hubs/main");
    if (mainConnection) {
      assignedCode = (await mainConnection.invoke(
        "JoinElection",
        electionGuid,
        clientId,
      )) as string;
    }

    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      // Track for auto-rejoin after reconnect (same as joinFrontDeskElection).
      this.frontDeskElectionGuid = electionGuid;
      await frontDeskConnection.invoke("JoinElection", electionGuid);
    }

    if (assignedCode) {
      setComputerCode(electionGuid, assignedCode);
    }

    return assignedCode;
  }

  async leaveElection(electionGuid: string): Promise<void> {
    if (this.mainElectionGuid === electionGuid) {
      this.mainElectionGuid = null;
    }
    if (this.frontDeskElectionGuid === electionGuid) {
      this.frontDeskElectionGuid = null;
    }

    const mainConnection = this.getConnection("/hubs/main");
    if (mainConnection) {
      await mainConnection.invoke("LeaveElection", electionGuid);
    }

    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("LeaveElection", electionGuid);
    }
  }

  /**
   * Known-teller dashboard: join MainHub groups for many elections (listen-only).
   * Replaces any previous dashboard multi-join set. Does not assign computer codes.
   */
  async joinDashboardElections(electionGuids: string[]): Promise<void> {
    const unique = [
      ...new Set(
        electionGuids.map((g) => g.trim()).filter((g) => g.length > 0),
      ),
    ];

    const previous = this.dashboardElectionGuids;
    const toLeave = previous.filter((g) => !unique.includes(g));
    // Keep active workstation membership if it is in the leave set — SignalR
    // groups are not refcounted; leaving would drop the active JoinElection.
    const leaveGuids = this.mainElectionGuid
      ? toLeave.filter((g) => g !== this.mainElectionGuid)
      : toLeave;

    if (leaveGuids.length > 0) {
      const mainConnection = this.getConnection("/hubs/main");
      if (mainConnection) {
        await mainConnection.invoke("LeaveElections", leaveGuids);
      }
    }

    this.dashboardElectionGuids = unique;
    if (unique.length === 0) {
      return;
    }

    const mainConnection = this.getConnection("/hubs/main");
    if (!mainConnection) {
      throw new Error("Main hub is not connected");
    }
    if (mainConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `Main hub is not ready (state: ${mainConnection.state})`,
      );
    }

    await mainConnection.invoke("JoinElections", unique);
  }

  /**
   * Leave dashboard multi-election groups. Preserves active workstation
   * MainHub membership when that election is still the main session election.
   */
  async leaveDashboardElections(): Promise<void> {
    const guids = this.dashboardElectionGuids;
    this.dashboardElectionGuids = [];
    if (guids.length === 0) {
      return;
    }

    const leaveGuids = this.mainElectionGuid
      ? guids.filter((g) => g !== this.mainElectionGuid)
      : guids;
    if (leaveGuids.length === 0) {
      return;
    }

    const mainConnection = this.getConnection("/hubs/main");
    if (mainConnection) {
      await mainConnection.invoke("LeaveElections", leaveGuids);
    }
  }

  async joinTallySession(electionGuid: string): Promise<void> {
    const analyzeConnection = this.getConnection("/hubs/analyze");
    if (analyzeConnection) {
      await analyzeConnection.invoke("JoinTallySession", electionGuid);
    }
  }

  async leaveTallySession(electionGuid: string): Promise<void> {
    const analyzeConnection = this.getConnection("/hubs/analyze");
    if (analyzeConnection) {
      await analyzeConnection.invoke("LeaveTallySession", electionGuid);
    }
  }

  async joinImportSession(electionGuid: string): Promise<void> {
    const importConnection = this.getConnection("/hubs/ballot-import");
    if (importConnection) {
      await importConnection.invoke("JoinImportSession", electionGuid);
    }
  }

  async leaveImportSession(electionGuid: string): Promise<void> {
    const importConnection = this.getConnection("/hubs/ballot-import");
    if (importConnection) {
      await importConnection.invoke("LeaveImportSession", electionGuid);
    }
  }

  async joinPeopleImportSession(electionGuid: string): Promise<void> {
    const peopleImportConnection = this.getConnection("/hubs/people-import");
    if (peopleImportConnection) {
      await peopleImportConnection.invoke("JoinImportSession", electionGuid);
    }
  }

  async leavePeopleImportSession(electionGuid: string): Promise<void> {
    const peopleImportConnection = this.getConnection("/hubs/people-import");
    if (peopleImportConnection) {
      await peopleImportConnection.invoke("LeaveImportSession", electionGuid);
    }
  }

  async joinFrontDeskElection(electionGuid: string): Promise<void> {
    this.frontDeskElectionGuid = electionGuid;
    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (!frontDeskConnection) {
      throw new Error("Front desk hub is not connected");
    }
    if (frontDeskConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `Front desk hub is not ready (state: ${frontDeskConnection.state})`,
      );
    }
    await frontDeskConnection.invoke("JoinElection", electionGuid);
  }

  async leaveFrontDeskElection(electionGuid: string): Promise<void> {
    if (this.frontDeskElectionGuid === electionGuid) {
      this.frontDeskElectionGuid = null;
    }
    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("LeaveElection", electionGuid);
    }
  }

  async joinPublicGroup(): Promise<void> {
    this.publicGroupJoined = true;
    const publicConnection = this.getConnection("/hubs/public");
    if (publicConnection) {
      await publicConnection.invoke("JoinPublicGroup");
    }
  }

  async leavePublicGroup(): Promise<void> {
    this.publicGroupJoined = false;
    const publicConnection = this.getConnection("/hubs/public");
    if (publicConnection) {
      await publicConnection.invoke("LeavePublicGroup");
    }
  }

  /**
   * Connect + join online-voter hubs (AllVoters + VoterPersonal).
   * Requires a factory that returns the current voter JWT (localStorage).
   */
  async connectVoterHubs(
    accessTokenFactory: () => string | null,
  ): Promise<void> {
    this.voterAccessTokenFactory = accessTokenFactory;
    await this.connectToAllVotersHub();
    await this.joinAllVoters();
    await this.connectToVoterPersonalHub();
    await this.joinVoterPersonal();
  }

  async disconnectVoterHubs(): Promise<void> {
    await this.leaveAllVoters();
    await this.leaveVoterPersonal();
    await this.disconnect("/hubs/all-voters");
    await this.disconnect("/hubs/voter-personal");
    this.voterAccessTokenFactory = null;
  }

  async connectToAllVotersHub(): Promise<signalR.HubConnection> {
    const factory =
      this.voterAccessTokenFactory ??
      (() => localStorage.getItem("voter_token"));
    return this.connect("/hubs/all-voters", factory);
  }

  async connectToVoterPersonalHub(): Promise<signalR.HubConnection> {
    const factory =
      this.voterAccessTokenFactory ??
      (() => localStorage.getItem("voter_token"));
    return this.connect("/hubs/voter-personal", factory);
  }

  async joinAllVoters(): Promise<void> {
    this.allVotersJoined = true;
    const connection = await this.connectToAllVotersHub();
    if (connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `AllVoters hub is not ready (state: ${connection.state})`,
      );
    }
    await connection.invoke("Join");
  }

  async leaveAllVoters(): Promise<void> {
    this.allVotersJoined = false;
    const connection = this.getConnection("/hubs/all-voters");
    if (connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke("Leave");
      } catch (error) {
        console.warn("Failed to leave AllVoters group:", error);
      }
    }
  }

  async joinVoterPersonal(): Promise<void> {
    this.voterPersonalJoined = true;
    const connection = await this.connectToVoterPersonalHub();
    if (connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `VoterPersonal hub is not ready (state: ${connection.state})`,
      );
    }
    await connection.invoke("Join");
  }

  async leaveVoterPersonal(): Promise<void> {
    this.voterPersonalJoined = false;
    const connection = this.getConnection("/hubs/voter-personal");
    if (connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke("Leave");
      } catch (error) {
        console.warn("Failed to leave VoterPersonal group:", error);
      }
    }
  }
}

export const signalrService = new SignalRService();
