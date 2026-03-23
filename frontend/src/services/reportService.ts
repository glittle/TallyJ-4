import api from "./api";
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
    const response = await api.get<ReportListItem[]>(
      `/api/Reports/${electionGuid}/available`,
    );
    return response.data;
  },

  async getReport(electionGuid: string, reportCode: string): Promise<unknown> {
    const response = await api.get(
      `/api/Reports/${electionGuid}/${reportCode}`,
    );
    return response.data;
  },

  async getMainReport(electionGuid: string): Promise<MainReport> {
    const response = await api.get<MainReport>(
      `/api/Reports/${electionGuid}/Main`,
    );
    return response.data;
  },

  async getVotesByNum(electionGuid: string): Promise<VotesByNumReport> {
    const response = await api.get<VotesByNumReport>(
      `/api/Reports/${electionGuid}/VotesByNum`,
    );
    return response.data;
  },

  async getVotesByName(electionGuid: string): Promise<VotesByNameReport> {
    const response = await api.get<VotesByNameReport>(
      `/api/Reports/${electionGuid}/VotesByName`,
    );
    return response.data;
  },

  async getBallots(electionGuid: string): Promise<BallotsReport> {
    const response = await api.get<BallotsReport>(
      `/api/Reports/${electionGuid}/Ballots`,
    );
    return response.data;
  },

  async getBallotsOnline(electionGuid: string): Promise<BallotsReport> {
    const response = await api.get<BallotsReport>(
      `/api/Reports/${electionGuid}/BallotsOnline`,
    );
    return response.data;
  },

  async getBallotsImported(electionGuid: string): Promise<BallotsReport> {
    const response = await api.get<BallotsReport>(
      `/api/Reports/${electionGuid}/BallotsImported`,
    );
    return response.data;
  },

  async getBallotsTied(electionGuid: string): Promise<BallotsReport> {
    const response = await api.get<BallotsReport>(
      `/api/Reports/${electionGuid}/BallotsTied`,
    );
    return response.data;
  },

  async getSpoiledVotes(electionGuid: string): Promise<SpoiledVotesReport> {
    const response = await api.get<SpoiledVotesReport>(
      `/api/Reports/${electionGuid}/SpoiledVotes`,
    );
    return response.data;
  },

  async getBallotAlignment(
    electionGuid: string,
  ): Promise<BallotAlignmentReport> {
    const response = await api.get<BallotAlignmentReport>(
      `/api/Reports/${electionGuid}/BallotAlignment`,
    );
    return response.data;
  },

  async getBallotsSame(electionGuid: string): Promise<BallotsSameReport> {
    const response = await api.get<BallotsSameReport>(
      `/api/Reports/${electionGuid}/BallotsSame`,
    );
    return response.data;
  },

  async getBallotsSummary(electionGuid: string): Promise<BallotsSummaryReport> {
    const response = await api.get<BallotsSummaryReport>(
      `/api/Reports/${electionGuid}/BallotsSummary`,
    );
    return response.data;
  },

  async getAllCanReceive(electionGuid: string): Promise<AllCanReceiveReport> {
    const response = await api.get<AllCanReceiveReport>(
      `/api/Reports/${electionGuid}/AllCanReceive`,
    );
    return response.data;
  },

  async getVoters(electionGuid: string): Promise<VotersReport> {
    const response = await api.get<VotersReport>(
      `/api/Reports/${electionGuid}/Voters`,
    );
    return response.data;
  },

  async getFlags(electionGuid: string): Promise<FlagsReport> {
    const response = await api.get<FlagsReport>(
      `/api/Reports/${electionGuid}/Flags`,
    );
    return response.data;
  },

  async getVotersOnline(electionGuid: string): Promise<VotersOnlineReport> {
    const response = await api.get<VotersOnlineReport>(
      `/api/Reports/${electionGuid}/VotersOnline`,
    );
    return response.data;
  },

  async getVotersByArea(electionGuid: string): Promise<VotersByAreaReport> {
    const response = await api.get<VotersByAreaReport>(
      `/api/Reports/${electionGuid}/VotersByArea`,
    );
    return response.data;
  },

  async getVotersByLocation(
    electionGuid: string,
  ): Promise<VotersByLocationReport> {
    const response = await api.get<VotersByLocationReport>(
      `/api/Reports/${electionGuid}/VotersByLocation`,
    );
    return response.data;
  },

  async getVotersByLocationArea(
    electionGuid: string,
  ): Promise<VotersByLocationAreaReport> {
    const response = await api.get<VotersByLocationAreaReport>(
      `/api/Reports/${electionGuid}/VotersByLocationArea`,
    );
    return response.data;
  },

  async getChangedPeople(electionGuid: string): Promise<ChangedPeopleReport> {
    const response = await api.get<ChangedPeopleReport>(
      `/api/Reports/${electionGuid}/ChangedPeople`,
    );
    return response.data;
  },

  async getAllNonEligible(electionGuid: string): Promise<AllNonEligibleReport> {
    const response = await api.get<AllNonEligibleReport>(
      `/api/Reports/${electionGuid}/AllNonEligible`,
    );
    return response.data;
  },

  async getVoterEmails(electionGuid: string): Promise<VoterEmailsReport> {
    const response = await api.get<VoterEmailsReport>(
      `/api/Reports/${electionGuid}/VoterEmails`,
    );
    return response.data;
  },
};
