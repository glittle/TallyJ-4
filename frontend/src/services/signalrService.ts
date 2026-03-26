import * as signalR from "@microsoft/signalr";
import { getAppConfig } from "@/config/appConfig";
import { ConnectionState } from "@/types/SignalRConnection";

class SignalRService {
  private connections: Map<string, signalR.HubConnection> = new Map();
  private connectionStates: Map<string, ConnectionState> = new Map();

  private get baseUrl(): string {
    return getAppConfig().apiUrl;
  }

  async connect(
    hubPath: string,
    accessToken?: string,
  ): Promise<signalR.HubConnection> {
    const existingConnection = this.connections.get(hubPath);
    if (
      existingConnection &&
      existingConnection.state === signalR.HubConnectionState.Connected
    ) {
      return existingConnection;
    }

    const hubUrl = `${this.baseUrl}${hubPath}`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Cookies are sent automatically by the browser with the initial HTTP request
        // No need to provide accessTokenFactory when using httpOnly cookies
        withCredentials: true,
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

    connection.onreconnected((connectionId) => {
      console.log(
        `SignalR reconnected for ${hubPath}. Connection ID: ${connectionId}`,
      );
      this.connectionStates.set(hubPath, ConnectionState.Connected);
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

  async connectToMainHub(accessToken?: string): Promise<signalR.HubConnection> {
    return this.connect("/hubs/main", accessToken);
  }

  async connectToAnalyzeHub(
    accessToken?: string,
  ): Promise<signalR.HubConnection> {
    return this.connect("/hubs/analyze", accessToken);
  }

  async connectToBallotImportHub(
    accessToken?: string,
  ): Promise<signalR.HubConnection> {
    return this.connect("/hubs/ballot-import", accessToken);
  }

  async connectToPublicHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/public");
  }

  async connectToFrontDeskHub(
    accessToken?: string,
  ): Promise<signalR.HubConnection> {
    return this.connect("/hubs/front-desk", accessToken);
  }

  async connectToPeopleImportHub(
    accessToken?: string,
  ): Promise<signalR.HubConnection> {
    return this.connect("/hubs/people-import", accessToken);
  }

  async joinElection(electionGuid: string): Promise<void> {
    const mainConnection = this.getConnection("/hubs/main");
    if (mainConnection) {
      await mainConnection.invoke("JoinElection", electionGuid);
    }

    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("JoinElection", electionGuid);
    }
  }

  async leaveElection(electionGuid: string): Promise<void> {
    const mainConnection = this.getConnection("/hubs/main");
    if (mainConnection) {
      await mainConnection.invoke("LeaveElection", electionGuid);
    }

    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("LeaveElection", electionGuid);
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
    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("JoinElection", electionGuid);
    }
  }

  async leaveFrontDeskElection(electionGuid: string): Promise<void> {
    const frontDeskConnection = this.getConnection("/hubs/front-desk");
    if (frontDeskConnection) {
      await frontDeskConnection.invoke("LeaveElection", electionGuid);
    }
  }

  async joinPublicDisplay(electionGuid: string): Promise<void> {
    const publicConnection = this.getConnection("/hubs/public");
    if (publicConnection) {
      await publicConnection.invoke("JoinPublicDisplay", electionGuid);
    }
  }

  async leavePublicDisplay(electionGuid: string): Promise<void> {
    const publicConnection = this.getConnection("/hubs/public");
    if (publicConnection) {
      await publicConnection.invoke("LeavePublicDisplay", electionGuid);
    }
  }
}

export const signalrService = new SignalRService();
