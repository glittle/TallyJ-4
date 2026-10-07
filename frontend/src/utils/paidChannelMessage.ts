/** i18n key for the owner-facing reason SMS, voice, and WhatsApp cannot send. */
export function paidChannelMessageKey(reason?: string | null): string | null {
  switch (reason) {
    case "not-approved":
      return "elections.paidChannels.notApproved";
    case "election-cap":
      return "elections.paidChannels.electionCap";
    case "owner-daily-cap":
      return "elections.paidChannels.ownerDailyCap";
    case "frozen":
      return "elections.paidChannels.frozen";
    case "flagged":
      return "elections.paidChannels.flagged";
    default:
      return null;
  }
}
