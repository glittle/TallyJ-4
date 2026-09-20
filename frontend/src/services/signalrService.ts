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
    await this.leaveOnlineVoterElection();
    await this.leaveAllVoters();
    await this.leaveVoterPersonal();
    await this.disconnect("/hubs/all-voters");
    await this.disconnect("/hubs/voter-personal");
  }

  /**
   * Count this AllVoters connection as a ballot-page session for the election.
   * Anonymous: the monitor receives a session count only.
   */
  async joinOnlineVoterElection(electionGuid: string): Promise<void> {
    this.allVotersElectionGuid = electionGuid;
    const connection = await this.connectToAllVotersHub();
    if (connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `AllVoters hub is not ready (state: ${connection.state})`,
      );
    }
    await connection.invoke("JoinElection", electionGuid);
  }

  async leaveOnlineVoterElection(): Promise<void> {
    this.allVotersElectionGuid = null;
    const connection = this.getConnection("/hubs/all-voters");
    if (connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke("LeaveElection");
      } catch (error) {
        console.warn("Failed to leave online voter election presence:", error);
      }
    }
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

  /**
   * Anonymous pre-auth hub for login-code delivery status.
   * Join uses the server-issued channel token from requestCode — not a voter JWT.
   */
  async connectToVoterCodeHub(): Promise<signalR.HubConnection> {
    return this.connect("/hubs/voter-code");
  }

  async joinVoterCodeChannel(channelToken: string): Promise<void> {
    this.voterCodeChannelToken = channelToken;
    const connection = await this.connectToVoterCodeHub();
    if (connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(
        `VoterCode hub is not ready (state: ${connection.state})`,
      );
    }
    await connection.invoke("Join", channelToken);
  }

  async leaveVoterCodeChannel(): Promise<void> {
    this.voterCodeChannelToken = null;
    const connection = this.getConnection("/hubs/voter-code");
    if (connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke("Leave");
      } catch (error) {
        console.warn("Failed to leave voter-code delivery channel:", error);
      }
    }
  }

  async disconnectVoterCodeHub(): Promise<void> {
    await this.leaveVoterCodeChannel();
    await this.disconnect("/hubs/voter-code");
  }
}

export const signalrService = new SignalRService();
