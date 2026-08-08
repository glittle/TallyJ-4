import type * as signalR from "@microsoft/signalr";
import { getOrCreateClientId } from "@/utils/clientIdStorage";
import { setComputerCode } from "@/utils/computerCodeStorage";
import { SignalRConnectionCore } from "./SignalRConnectionCore";

/** Teller / import / public hub connect + join APIs. */
export class SignalRTellerHubs extends SignalRConnectionCore {
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
      throw new Error(`Main hub is not ready (state: ${mainConnection.state})`);
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
}
