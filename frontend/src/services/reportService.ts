import {
  getApiReportsByElectionGuidAvailable,
  getApiReportsByElectionGuidMain,
  getApiReportsByElectionGuidVotesByNum,
  getApiReportsByElectionGuidVotesByName,
  getApiReportsByElectionGuidBallots,
  getApiReportsByElectionGuidBallotsOnline,
  getApiReportsByElectionGuidBallotsImported,
  getApiReportsByElectionGuidBallotsTied,
  getApiReportsByElectionGuidSpoiledVotes,
  getApiReportsByElectionGuidBallotAlignment,
  getApiReportsByElectionGuidBallotsSame,
  getApiReportsByElectionGuidBallotsSummary,
  getApiReportsByElectionGuidAllCanReceive,
  getApiReportsByElectionGuidVoters,
  getApiReportsByElectionGuidFlags,
  getApiReportsByElectionGuidVotersOnline,
  getApiReportsByElectionGuidVotersByArea,
  getApiReportsByElectionGuidVotersByLocation,
  getApiReportsByElectionGuidVotersByLocationArea,
  getApiReportsByElectionGuidChangedPeople,
  getApiReportsByElectionGuidAllNonEligible,
  getApiReportsByElectionGuidVoterEmails,
} from "@/api/gen/configService";
import type {
  ReportListItem,
  MainReport,
  VotesByNumReport,
  VotesByNameReport,
  BallotsReport,
  SpoiledVotesReport,
  BallotAlignmentReport,
  BallotsSameReport,
  BallotsSummaryReport,
  AllCanReceiveReport,
  VotersReport,
  FlagsReport,
  VotersOnlineReport,
  VotersByAreaReport,
  VotersByLocationReport,
  VotersByLocationAreaReport,
  ChangedPeopleReport,
  AllNonEligibleReport,
  VoterEmailsReport,
} from "../types";

export const reportService = {
  async getAvailableReports(electionGuid: string): Promise<ReportListItem[]> {
    const response = await getApiReportsByElectionGuidAvailable({
      path: { electionGuid },
    });
    return (response.data ?? []) as ReportListItem[];
  },

  async getReport(electionGuid: string, reportCode: string): Promise<unknown> {
    switch (reportCode) {
      case "Main":
        return this.getMainReport(electionGuid);
      case "VotesByNum":
        return this.getVotesByNum(electionGuid);
      case "VotesByName":
        return this.getVotesByName(electionGuid);
      case "Ballots":
        return this.getBallots(electionGuid);
      case "BallotsOnline":
        return this.getBallotsOnline(electionGuid);
      case "BallotsImported":
        return this.getBallotsImported(electionGuid);
      case "BallotsTied":
        return this.getBallotsTied(electionGuid);
      case "SpoiledVotes":
        return this.getSpoiledVotes(electionGuid);
      case "BallotAlignment":
        return this.getBallotAlignment(electionGuid);
      case "BallotsSame":
        return this.getBallotsSame(electionGuid);
      case "BallotsSummary":
        return this.getBallotsSummary(electionGuid);
      case "AllCanReceive":
        return this.getAllCanReceive(electionGuid);
      case "Voters":
        return this.getVoters(electionGuid);
      case "Flags":
        return this.getFlags(electionGuid);
      case "VotersOnline":
        return this.getVotersOnline(electionGuid);
      case "VotersByArea":
        return this.getVotersByArea(electionGuid);
      case "VotersByLocation":
        return this.getVotersByLocation(electionGuid);
      case "VotersByLocationArea":
        return this.getVotersByLocationArea(electionGuid);
      case "ChangedPeople":
        return this.getChangedPeople(electionGuid);
      case "AllNonEligible":
        return this.getAllNonEligible(electionGuid);
      case "VoterEmails":
        return this.getVoterEmails(electionGuid);
      default:
        throw new Error(`Unknown report code: ${reportCode}`);
    }
  },

  async getMainReport(electionGuid: string): Promise<MainReport> {
    const response = await getApiReportsByElectionGuidMain({
      path: { electionGuid },
    });
    return response.data as MainReport;
  },

  async getVotesByNum(electionGuid: string): Promise<VotesByNumReport> {
    const response = await getApiReportsByElectionGuidVotesByNum({
      path: { electionGuid },
    });
    return response.data as VotesByNumReport;
  },

  async getVotesByName(electionGuid: string): Promise<VotesByNameReport> {
    const response = await getApiReportsByElectionGuidVotesByName({
      path: { electionGuid },
    });
    return response.data as VotesByNameReport;
  },

  async getBallots(electionGuid: string): Promise<BallotsReport> {
    const response = await getApiReportsByElectionGuidBallots({
      path: { electionGuid },
    });
    return response.data as BallotsReport;
  },

  async getBallotsOnline(electionGuid: string): Promise<BallotsReport> {
    const response = await getApiReportsByElectionGuidBallotsOnline({
      path: { electionGuid },
    });
    return response.data as BallotsReport;
  },

  async getBallotsImported(electionGuid: string): Promise<BallotsReport> {
    const response = await getApiReportsByElectionGuidBallotsImported({
      path: { electionGuid },
    });
    return response.data as BallotsReport;
  },

  async getBallotsTied(electionGuid: string): Promise<BallotsReport> {
    const response = await getApiReportsByElectionGuidBallotsTied({
      path: { electionGuid },
    });
    return response.data as BallotsReport;
  },

  async getSpoiledVotes(electionGuid: string): Promise<SpoiledVotesReport> {
    const response = await getApiReportsByElectionGuidSpoiledVotes({
      path: { electionGuid },
    });
    return response.data as SpoiledVotesReport;
  },

  async getBallotAlignment(
    electionGuid: string,
  ): Promise<BallotAlignmentReport> {
    const response = await getApiReportsByElectionGuidBallotAlignment({
      path: { electionGuid },
    });
    return response.data as BallotAlignmentReport;
  },

  async getBallotsSame(electionGuid: string): Promise<BallotsSameReport> {
    const response = await getApiReportsByElectionGuidBallotsSame({
      path: { electionGuid },
    });
    return response.data as BallotsSameReport;
  },

  async getBallotsSummary(electionGuid: string): Promise<BallotsSummaryReport> {
    const response = await getApiReportsByElectionGuidBallotsSummary({
      path: { electionGuid },
    });
    return response.data as BallotsSummaryReport;
  },

  async getAllCanReceive(electionGuid: string): Promise<AllCanReceiveReport> {
    const response = await getApiReportsByElectionGuidAllCanReceive({
      path: { electionGuid },
    });
    return response.data as AllCanReceiveReport;
  },

  async getVoters(electionGuid: string): Promise<VotersReport> {
    const response = await getApiReportsByElectionGuidVoters({
      path: { electionGuid },
    });
    return response.data as VotersReport;
  },

  async getFlags(electionGuid: string): Promise<FlagsReport> {
    const response = await getApiReportsByElectionGuidFlags({
      path: { electionGuid },
    });
    return response.data as FlagsReport;
  },

  async getVotersOnline(electionGuid: string): Promise<VotersOnlineReport> {
    const response = await getApiReportsByElectionGuidVotersOnline({
      path: { electionGuid },
    });
    return response.data as VotersOnlineReport;
  },

  async getVotersByArea(electionGuid: string): Promise<VotersByAreaReport> {
    const response = await getApiReportsByElectionGuidVotersByArea({
      path: { electionGuid },
    });
    return response.data as VotersByAreaReport;
  },

  async getVotersByLocation(
    electionGuid: string,
  ): Promise<VotersByLocationReport> {
    const response = await getApiReportsByElectionGuidVotersByLocation({
      path: { electionGuid },
    });
    return response.data as VotersByLocationReport;
  },

  async getVotersByLocationArea(
    electionGuid: string,
  ): Promise<VotersByLocationAreaReport> {
    const response = await getApiReportsByElectionGuidVotersByLocationArea({
      path: { electionGuid },
    });
    return response.data as VotersByLocationAreaReport;
  },

  async getChangedPeople(electionGuid: string): Promise<ChangedPeopleReport> {
    const response = await getApiReportsByElectionGuidChangedPeople({
      path: { electionGuid },
    });
    return response.data as ChangedPeopleReport;
  },

  async getAllNonEligible(electionGuid: string): Promise<AllNonEligibleReport> {
    const response = await getApiReportsByElectionGuidAllNonEligible({
      path: { electionGuid },
    });
    return response.data as AllNonEligibleReport;
  },

  async getVoterEmails(electionGuid: string): Promise<VoterEmailsReport> {
    const response = await getApiReportsByElectionGuidVoterEmails({
      path: { electionGuid },
    });
    return response.data as VoterEmailsReport;
  },
};
