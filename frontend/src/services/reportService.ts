import { client } from "../api/config";
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
    const response = await client.get<ReportListItem[]>({
      url: `/api/Reports/${electionGuid}/available`,
    });
    return response.data;
  },

  async getReport(electionGuid: string, reportCode: string): Promise<unknown> {
    const response = await client.get({
      url: `/api/Reports/${electionGuid}/${reportCode}`,
    });
    return response.data;
  },

  async getMainReport(electionGuid: string): Promise<MainReport> {
    const response = await client.get<MainReport>({
      url: `/api/Reports/${electionGuid}/Main`,
    });
    return response.data;
  },

  async getVotesByNum(electionGuid: string): Promise<VotesByNumReport> {
    const response = await client.get<VotesByNumReport>({
      url: `/api/Reports/${electionGuid}/VotesByNum`,
    });
    return response.data;
  },

  async getVotesByName(electionGuid: string): Promise<VotesByNameReport> {
    const response = await client.get<VotesByNameReport>({
      url: `/api/Reports/${electionGuid}/VotesByName`,
    });
    return response.data;
  },

  async getBallots(electionGuid: string): Promise<BallotsReport> {
    const response = await client.get<BallotsReport>({
      url: `/api/Reports/${electionGuid}/Ballots`,
    });
    return response.data;
  },

  async getBallotsOnline(electionGuid: string): Promise<BallotsReport> {
    const response = await client.get<BallotsReport>({
      url: `/api/Reports/${electionGuid}/BallotsOnline`,
    });
    return response.data;
  },

  async getBallotsImported(electionGuid: string): Promise<BallotsReport> {
    const response = await client.get<BallotsReport>({
      url: `/api/Reports/${electionGuid}/BallotsImported`,
    });
    return response.data;
  },

  async getBallotsTied(electionGuid: string): Promise<BallotsReport> {
    const response = await client.get<BallotsReport>({
      url: `/api/Reports/${electionGuid}/BallotsTied`,
    });
    return response.data;
  },

  async getSpoiledVotes(electionGuid: string): Promise<SpoiledVotesReport> {
    const response = await client.get<SpoiledVotesReport>({
      url: `/api/Reports/${electionGuid}/SpoiledVotes`,
    });
    return response.data;
  },

  async getBallotAlignment(
    electionGuid: string,
  ): Promise<BallotAlignmentReport> {
    const response = await client.get<BallotAlignmentReport>({
      url: `/api/Reports/${electionGuid}/BallotAlignment`,
    });
    return response.data;
  },

  async getBallotsSame(electionGuid: string): Promise<BallotsSameReport> {
    const response = await client.get<BallotsSameReport>({
      url: `/api/Reports/${electionGuid}/BallotsSame`,
    });
    return response.data;
  },

  async getBallotsSummary(electionGuid: string): Promise<BallotsSummaryReport> {
    const response = await client.get<BallotsSummaryReport>({
      url: `/api/Reports/${electionGuid}/BallotsSummary`,
    });
    return response.data;
  },

  async getAllCanReceive(electionGuid: string): Promise<AllCanReceiveReport> {
    const response = await client.get<AllCanReceiveReport>({
      url: `/api/Reports/${electionGuid}/AllCanReceive`,
    });
    return response.data;
  },

  async getVoters(electionGuid: string): Promise<VotersReport> {
    const response = await client.get<VotersReport>({
      url: `/api/Reports/${electionGuid}/Voters`,
    });
    return response.data;
  },

  async getFlags(electionGuid: string): Promise<FlagsReport> {
    const response = await client.get<FlagsReport>({
      url: `/api/Reports/${electionGuid}/Flags`,
    });
    return response.data;
  },

  async getVotersOnline(electionGuid: string): Promise<VotersOnlineReport> {
    const response = await client.get<VotersOnlineReport>({
      url: `/api/Reports/${electionGuid}/VotersOnline`,
    });
    return response.data;
  },

  async getVotersByArea(electionGuid: string): Promise<VotersByAreaReport> {
    const response = await client.get<VotersByAreaReport>({
      url: `/api/Reports/${electionGuid}/VotersByArea`,
    });
    return response.data;
  },

  async getVotersByLocation(
    electionGuid: string,
  ): Promise<VotersByLocationReport> {
    const response = await client.get<VotersByLocationReport>({
      url: `/api/Reports/${electionGuid}/VotersByLocation`,
    });
    return response.data;
  },

  async getVotersByLocationArea(
    electionGuid: string,
  ): Promise<VotersByLocationAreaReport> {
    const response = await client.get<VotersByLocationAreaReport>({
      url: `/api/Reports/${electionGuid}/VotersByLocationArea`,
    });
    return response.data;
  },

  async getChangedPeople(electionGuid: string): Promise<ChangedPeopleReport> {
    const response = await client.get<ChangedPeopleReport>({
      url: `/api/Reports/${electionGuid}/ChangedPeople`,
    });
    return response.data;
  },

  async getAllNonEligible(electionGuid: string): Promise<AllNonEligibleReport> {
    const response = await client.get<AllNonEligibleReport>({
      url: `/api/Reports/${electionGuid}/AllNonEligible`,
    });
    return response.data;
  },

  async getVoterEmails(electionGuid: string): Promise<VoterEmailsReport> {
    const response = await client.get<VoterEmailsReport>({
      url: `/api/Reports/${electionGuid}/VoterEmails`,
    });
    return response.data;
  },
};
