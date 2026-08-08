import { getAppConfig } from "@/config/appConfig";
import { ConnectionState } from "@/types/SignalRConnection";
import { getOrCreateClientId } from "@/utils/clientIdStorage";
import { setComputerCode } from "@/utils/computerCodeStorage";
import * as signalR from "@microsoft/signalr";

export class SignalRConnectionCore {
  protected readonly connections: Map<string, signalR.HubConnection> =
    new Map();
  protected readonly connectionstates: Map<string, ConnectionState> = new Map();
  protected frontDeskElectionGuid: string | null = null;
  protected mainElectionGuid: string | null = null;
  /** Known-teller dashboard multi-election listen set (MainHub JoinElections). */
  protected dashboardElectionGuids: string[] = [];
  protected publicGroupJoined = false;
  /** Online voter hubs (Bearer voter JWT from localStorage). */
  protected allVotersJoined = false;
  protected voterPersonalJoined = false;
  protected voterAccessTokenFactory: (() => string | null) | null = null;

  protected get baseUrl(): string {
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
}
