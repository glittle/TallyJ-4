/** Front Desk / person-edit: a recorded voting method or an accepted online ballot. */
export function hasAcceptedBallot(person: {
  votingMethod?: string | null;
  hasOnlineBallot?: boolean | null;
}): boolean {
  return Boolean(person.votingMethod?.trim()) || person.hasOnlineBallot === true;
}

/**
 * v3 Front Desk `.VM-`: a voting method means the ballot was received / recorded.
 * People with no voting method are “Ballot Not Received”.
 */
export function voterHasReceivedBallot(voter: {
  votingMethod?: string | null;
}): boolean {
  return Boolean(voter.votingMethod?.trim());
}

export function applyBallotNotReceivedFilter<
  T extends { votingMethod?: string | null },
>(voters: T[], ballotNotReceivedOnly: boolean): T[] {
  if (!ballotNotReceivedOnly) {
    return voters;
  }
  return voters.filter((voter) => !voterHasReceivedBallot(voter));
}

/** Disable X/R (and unknown) reasons that remove the right to vote. */
export function isCannotVoteReasonDisabled(
  reason: { canVote?: boolean } | null | undefined,
  personHasAcceptedBallot: boolean,
): boolean {
  return personHasAcceptedBallot && reason?.canVote === false;
}
