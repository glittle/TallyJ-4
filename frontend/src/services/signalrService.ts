import * as signalR from "@microsoft/signalr";
import { SignalRTellerHubs } from "./signalr/SignalRTellerHubs";

/**
 * SignalR facade for the SPA: connection core + teller hubs + online-voter hubs.
 * Call sites continue to import { signalrService } from "@/services/signalrService".
 */
export class SignalRService extends SignalRTellerHubs {
  /**
   * Connect + join online-voter hubs (AllVoters + VoterPersonal).
   * Auth is the httpOnly voter_token cookie (withCredentials); no JS-readable JWT.
   */
  async connectVoterHubs(): Promise<void> {
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
  }

  async connectToAllVotersHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/all-voters");
  }

  async connectToVoterPersonalHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/voter-personal");
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
